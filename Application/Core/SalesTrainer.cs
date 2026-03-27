using Application.Custom_Exceptions;
using Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using Persistence.Models;

namespace Application.Core
{
    public class SalesTrainer
    {
        private readonly MLContext _mlContext;
        private readonly object _predictionLock = new();
        private readonly ILogger<SalesTrainer> _logger;

        public SalesTrainer(ILogger<SalesTrainer> logger)
        {
            _mlContext = new MLContext(seed:32);
            _logger = logger;
        }

        public bool IsTrained { get; private set; }

        public List<string> UniqueProducts
        {
            get 
            {
                lock (_uniqueProductsLock)
                {
                    return _uniqueProducts;
                }
            }
            set 
            {
                lock (_uniqueProductsLock)
                {
                    _uniqueProducts = value;
                }
            }
        }

        private readonly object _uniqueProductsLock = new();

        private List<string> _uniqueProducts = new List<string>();


        // Statistic fields for regression and predictions 
        private Dictionary<(int DayOfWeek, string TypeOfDay), double> _dailyAverages = new();
        private Dictionary<(int DayOfWeek, string PeriodDay, string TypeOfDay), double> _periodAverages = new();
        private Dictionary<(int DayOfWeek, string PeriodDay, string TypeOfDay), List<(string Item, int Count)>> _topProducts = new();
        private Dictionary<(int DayOfWeek, string PeriodDay, string TypeOfDay), (int SampleCount, double StdDev)> _periodStats = new();
        private double _overallDailyAvg;
        private double _rSquared;
        private double _mae;
        private double _rmse;

        // ml .net field 
        private PredictionEngine<DailySalesInput, DailySalesOutput>? _regressionEngine; // regresion model engine 
        private PredictionEngine<ProductClassificationInput, ProductClassificationOutput>? _productEngine; // multi-class classification model engine
        private PredictionEngine<DayTypeInput, DayTypeOutput>? _dayTypeEngine; // binary classification model engine
        private ITransformer? _forecastModel;

        private double _mlRSquared, _mlMAE, _mlRMSE;
        private double _classAccuracy, _classLogLoss;
        private double _binaryAccuracy, _binaryAUC, _binaryF1;
        private bool _forecastTrained;
        private string[] _productLabels = Array.Empty<string>();

        public TrainResult Train(List<Sale> sales,bool trainRegression = true,bool trainClassification = true, bool trainForecasting = true)
        {
            _logger.LogInformation($"Initiating training with {sales.Count} sales.", sales.Count);
            var dailyCounts = sales
                .GroupBy(s => s.DateTime.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    DayOfWeek = (int)g.Key.DayOfWeek,
                    TypeOfDay = g.First().TypeOfDay.ToString(),
                    Count = g.Count()
                })
                .ToList();

            _overallDailyAvg = dailyCounts.Average(d => d.Count);

            _logger.LogInformation($"Calculated overall daily average: {_overallDailyAvg:F2} from {dailyCounts.Count} unique days.", _overallDailyAvg, dailyCounts.Count);

            _dailyAverages = dailyCounts
                .GroupBy(d => (d.DayOfWeek, d.TypeOfDay))
                .ToDictionary(g => g.Key, g => g.Average(d => d.Count));

            // Averages of (dayOfWeek, period, typeOfDay, date)
            var periodDailyCounts = sales
                .GroupBy(s => new
                {
                    DayOfWeek = (int)s.DateTime.DayOfWeek,
                    PeriodDay = s.PeriodDay.ToString(),
                    TypeOfDay = s.TypeOfDay.ToString(),
                    Date = s.DateTime.Date
                })
                .Select(g => new
                {
                    g.Key.DayOfWeek,
                    g.Key.PeriodDay,
                    g.Key.TypeOfDay,
                    Count = g.Count()
                })
                .ToList();

            _periodAverages = periodDailyCounts
                .GroupBy(x => (x.DayOfWeek, x.PeriodDay, x.TypeOfDay))
                .ToDictionary(g => g.Key, g => g.Average(x => x.Count));

            _logger.LogInformation($"Calculated period averages for {periodDailyCounts.Count} unique (dayOfWeek, period, typeOfDay) combinations.", periodDailyCounts.Count);

            // Calculate statistics 
            _periodStats = periodDailyCounts
                .GroupBy(x => (x.DayOfWeek, x.PeriodDay, x.TypeOfDay))
                .ToDictionary(g => g.Key, g =>
                {
                    var values = g.Select(x => (double)x.Count).ToList();
                    var avg = values.Average();
                    var stdDev = values.Count > 1
                        ? Math.Sqrt(values.Sum(v => (v - avg) * (v - avg)) / (values.Count - 1))
                        : 0;
                    return (SampleCount: values.Count, StdDev: Math.Round(stdDev, 2));
                });

            _topProducts = sales
                .GroupBy(s => (
                    DayOfWeek: (int)s.DateTime.DayOfWeek,
                    PeriodDay: s.PeriodDay.ToString(),
                    TypeOfDay: s.TypeOfDay.ToString()))
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(s => s.Item)
                          .Select(ig => (Item: ig.Key, Count: ig.Count()))
                          .OrderByDescending(x => x.Count)
                          .Take(5)
                          .ToList());

            // R², MAE, RMSE
            var ssTot = dailyCounts.Sum(d => Math.Pow(d.Count - _overallDailyAvg, 2));
            var ssRes = 0.0;
            var absErrors = 0.0;
            var sqErrors = 0.0;

            foreach (var day in dailyCounts)
            {
                var key = (day.DayOfWeek, day.TypeOfDay);
                var predicted = _dailyAverages.GetValueOrDefault(key, _overallDailyAvg);
                var error = day.Count - predicted;
                ssRes += error * error;
                absErrors += Math.Abs(error);
                sqErrors += error * error;
            }

            _rSquared = ssTot > 0 ? Math.Round(1 - ssRes / ssTot, 4) : 1;
            _mae = Math.Round(absErrors / dailyCounts.Count, 2);
            _rmse = Math.Round(Math.Sqrt(sqErrors / dailyCounts.Count), 2);

            UniqueProducts = sales.Select(s => s.Item).Distinct().OrderBy(x => x).ToList();

            IsTrained = true;

            _logger.LogInformation($"Training completed. R²: {_rSquared}, MAE: {_mae}, RMSE: {_rmse}. Unique products: {UniqueProducts.Count}.", _rSquared, _mae, _rmse, UniqueProducts.Count);

            // In here We will train all ml.net models 
            try 
            {
                if (trainRegression)
                {
                    TrainRegressionModel(sales);
                }

                if(trainClassification)
                {
                    TrainProductClassifier(sales);
                    TrainDayTypeClassifier(sales);
                }

                if (trainForecasting) 
                {
                    TrainForecastingModel(sales);
                }
            }
            catch(TrainingException ex) {
                _logger.LogError(ex,"Error training regression model. ML.NET regression metrics will be unavailable.");    
            }

            return new TrainResult
            {
                TotalRecords = sales.Count,
                UniqueDays = dailyCounts.Count,
                AverageDailySales = Math.Round(_overallDailyAvg, 1),
                UniqueProducts = UniqueProducts.Count,
                RSquared = _rSquared,
                MAE = _mae,
                RMSE = _rmse,
                MLRSquared = _mlRSquared,
                MLMAE = _mlMAE,
                MLRMSE = _mlRMSE,
                ClassificationAccuracy = _classAccuracy,
                ClassificationLogLoss = _classLogLoss,
                BinaryAccuracy = _binaryAccuracy,
                BinaryAUC = _binaryAUC,
                BinaryF1 = _binaryF1,
                ForecastingTrained = _forecastTrained
            };
        }

        #region Precdiction methods

        public PredictionResult Predict(int dayOfWeek, int month, string periodDay, string typeOfDay)
        {
            var dayName = ((DayOfWeek)dayOfWeek).ToString();
            var isWeekend = typeOfDay.Equals("weekend", StringComparison.OrdinalIgnoreCase) ? 1f : 0f;

            // Use ML.NET regression engine for daily prediction
            double predictedDaily;
            if (_regressionEngine != null)
            {
                DailySalesOutput mlOutput;
                lock (_predictionLock)
                {
                    mlOutput = _regressionEngine.Predict(new DailySalesInput
                    {
                        DayOfWeek = (float)dayOfWeek,
                        Month = (float)month,
                        IsWeekend = isWeekend,
                        Label = 0f
                    });
                }
                predictedDaily = Math.Max(0, mlOutput.Score);
            }
            else
            {
                // Fallback to statistical average if regression engine not trained
                var dailyKey = (dayOfWeek, typeOfDay);
                predictedDaily = _dailyAverages.GetValueOrDefault(dailyKey, _overallDailyAvg);
            }

            var periodKey = (dayOfWeek, periodDay, typeOfDay);
            var predictedPeriod = _periodAverages.GetValueOrDefault(periodKey, 0);

            var sampleCount = 0;
            var stdDev = 0.0;
            if (_periodStats.TryGetValue(periodKey, out var stats))
            {
                sampleCount = stats.SampleCount;
                stdDev = stats.StdDev;
            }

            var topProducts = new List<ProductPrediction>();
            if (_topProducts.TryGetValue(periodKey, out var products))
            {
                var total = products.Sum(p => p.Count);
                topProducts = products.Select(p => new ProductPrediction
                {
                    ProductName = p.Item,
                    Count = p.Count,
                    Percentage = total > 0 ? Math.Round(100.0 * p.Count / total, 1) : 0
                }).ToList();
            }

            // Confidence higher sample + lower relative
            var confidence = sampleCount > 0 && predictedPeriod > 0
                ? Math.Round(Math.Max(0, Math.Min(100, 100 * (1 - stdDev / predictedPeriod) * Math.Min(1, sampleCount / 10.0))), 1)
                : 0;

            var usesMlEngine = _regressionEngine != null;

            return new PredictionResult
            {
                PredictedDailySalesCount = Math.Round(predictedDaily, 1),
                PredictedPeriodSalesCount = Math.Round(predictedPeriod, 1),
                TopProducts = topProducts,
                DayName = dayName,
                Period = periodDay,
                DayType = typeOfDay,
                RSquared = usesMlEngine ? _mlRSquared : _rSquared,
                MAE = usesMlEngine ? _mlMAE : _mae,
                RMSE = usesMlEngine ? _mlRMSE : _rmse,
                Confidence = confidence,
                SampleCount = sampleCount,
                StandardDeviation = stdDev,
                UsesMLEngine = usesMlEngine
            };
        }

        public ClassificationResult PredictProduct(int hourOfDay, int dayOfWeek, int month, string periodDay, string typeOfDay)
        {
            if (_productEngine == null)
                throw new InvalidOperationException("Modelo de classificação de produto não treinado.");

            ProductClassificationOutput prediction;
            lock (_predictionLock)
            {
                prediction = _productEngine.Predict(new ProductClassificationInput
                {
                    HourOfDay = hourOfDay,
                    DayOfWeek = dayOfWeek,
                    Month = month,
                    PeriodDay = periodDay,
                    TypeOfDay = typeOfDay,
                    Item = string.Empty
                });
            }

            var topPredictions = new List<ProductScore>();
            if (prediction.Score != null && _productLabels.Length > 0)
            {
                topPredictions = prediction.Score
                    .Select((score, idx) => new { Score = score, Index = idx })
                    .OrderByDescending(x => x.Score)
                    .Take(5)
                    .Where(x => x.Index < _productLabels.Length)
                    .Select(x => new ProductScore
                    {
                        ProductName = _productLabels[x.Index],
                        Score = Math.Round(x.Score, 4),
                        Percentage = Math.Round(x.Score * 100, 1)
                    })
                    .ToList();
            }

            return new ClassificationResult
            {
                PredictedProduct = prediction.PredictedLabel ?? string.Empty,
                TopPredictions = topPredictions,
                ModelAccuracy = _classAccuracy,
                ModelLogLoss = _classLogLoss,
                HourOfDay = hourOfDay,
                DayName = ((DayOfWeek)dayOfWeek).ToString(),
                Period = periodDay,
                DayType = typeOfDay
            };
        }

        public DayTypePredictionResult PredictDayType(int hourOfDay, string periodDay, string item)
        {
            if (_dayTypeEngine == null)
                throw new InvalidOperationException("Modelo de classificação de tipo de dia não treinado.");

            DayTypeOutput prediction;
            lock (_predictionLock)
            {
                prediction = _dayTypeEngine.Predict(new DayTypeInput
                {
                    HourOfDay = hourOfDay,
                    PeriodDay = periodDay,
                    Item = item,
                    Label = false
                });
            }

            return new DayTypePredictionResult
            {
                PredictedIsWeekend = prediction.PredictedLabel,
                PredictedDayType = prediction.PredictedLabel ? "weekend" : "weekday",
                Probability = Math.Round(prediction.Probability * 100, 1),
                ModelAccuracy = _binaryAccuracy,
                ModelAUC = _binaryAUC,
                ModelF1 = _binaryF1,
                HourOfDay = hourOfDay,
                Period = periodDay,
                Item = item
            };
        }

        public ForecastResult Forecast(int horizon = 30)
        {
            if (_forecastModel == null){
                throw new InvalidOperationException("Modelo de previsão temporal não treinado.");
            }

            TimeSeriesForecastOutput forecast;
            lock (_predictionLock)
            {
                var engine = _forecastModel.CreateTimeSeriesEngine<TimeSeriesInput, TimeSeriesForecastOutput>(_mlContext);
                forecast = engine.Predict();
            }

            var today = DateTime.Today;
            var points = new List<ForecastPoint>();
            var count = Math.Min(horizon, forecast.ForecastedSales.Length);
            for (int i = 0; i < count; i++)
            {
                points.Add(new ForecastPoint
                {
                    Date = today.AddDays(i + 1),
                    PredictedSales = Math.Round(Math.Max(0, forecast.ForecastedSales[i]), 1),
                    LowerBound = Math.Round(Math.Max(0, forecast.LowerBoundSales[i]), 1),
                    UpperBound = Math.Round(Math.Max(0, forecast.UpperBoundSales[i]), 1)
                });
            }

            return new ForecastResult
            {
                Forecast = points,
                Horizon = horizon,
                StartDate = today.AddDays(1)
            };
        }

        #endregion

        #region Training methods
        private void TrainRegressionModel(List<Sale> sales)
        {
            try
            {
                var dailyData = sales
               .GroupBy(s => s.DateTime.Date)
               .Select(g => new DailySalesInput
               {
                   DayOfWeek = (float)g.Key.DayOfWeek,
                   Month = (float)g.Key.Month,
                   IsWeekend = g.First().TypeOfDay.ToString() == "weekend" ? 1f : 0f,
                   Label = (float)g.Count()
               })
               .ToList();

                var dataView = _mlContext.Data.LoadFromEnumerable(dailyData);
                var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

                var pipeline = _mlContext.Transforms.Concatenate("Features",
                        nameof(DailySalesInput.DayOfWeek),
                        nameof(DailySalesInput.Month),
                        nameof(DailySalesInput.IsWeekend))
                    .Append(_mlContext.Regression.Trainers.FastTree());

                var model = pipeline.Fit(split.TrainSet);

                var predictions = model.Transform(split.TestSet);
                var metrics = _mlContext.Regression.Evaluate(predictions);

                _mlRSquared = Math.Round(metrics.RSquared, 4);
                _mlMAE = Math.Round(metrics.MeanAbsoluteError, 2);
                _mlRMSE = Math.Round(metrics.RootMeanSquaredError, 2);

                _regressionEngine = _mlContext.Model.CreatePredictionEngine<DailySalesInput, DailySalesOutput>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training regression model.");
                throw new RegressionTrainingException("Error training thre regression Model",ex);
            }
        }

        private void TrainProductClassifier(List<Sale> sales)
        {
            try
            {
                var data = sales.Select(s => new ProductClassificationInput
                {
                    HourOfDay = (float)s.DateTime.Hour,
                    DayOfWeek = (float)s.DateTime.DayOfWeek,
                    Month = (float)s.DateTime.Month,
                    PeriodDay = s.PeriodDay.ToString(),
                    TypeOfDay = s.TypeOfDay.ToString(),
                    Item = s.Item
                }).ToList();

                _productLabels = data.Select(d => d.Item).Distinct().ToArray();

                var dataView = _mlContext.Data.LoadFromEnumerable(data);
                var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

                var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ProductClassificationInput.Item))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PeriodDayEncoded", nameof(ProductClassificationInput.PeriodDay)))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("TypeOfDayEncoded", nameof(ProductClassificationInput.TypeOfDay)))
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        nameof(ProductClassificationInput.HourOfDay),
                        nameof(ProductClassificationInput.DayOfWeek),
                        nameof(ProductClassificationInput.Month),
                        "PeriodDayEncoded",
                        "TypeOfDayEncoded"))
                    .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                var model = pipeline.Fit(split.TrainSet);

                var testPredictions = model.Transform(split.TestSet);
                var metrics = _mlContext.MulticlassClassification.Evaluate(testPredictions);

                _classAccuracy = Math.Round(metrics.MacroAccuracy, 4);
                _classLogLoss = Math.Round(metrics.LogLoss, 4);

                _productEngine = _mlContext.Model.CreatePredictionEngine<ProductClassificationInput, ProductClassificationOutput>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training product classification model.");
                throw new ClassificationTrainingException("Error training product classification model.",ex);
            }
        }

        private void TrainDayTypeClassifier(List<Sale> sales)
        {
            try
            {
                var data = sales.Select(s => new DayTypeInput
                {
                    HourOfDay = (float)s.DateTime.Hour,
                    PeriodDay = s.PeriodDay.ToString(),
                    Item = s.Item,
                    Label = s.TypeOfDay.ToString() == "weekend"
                }).ToList();

                var dataView = _mlContext.Data.LoadFromEnumerable(data);
                var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

                var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("PeriodDayEncoded", nameof(DayTypeInput.PeriodDay))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("ItemEncoded", nameof(DayTypeInput.Item)))
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        nameof(DayTypeInput.HourOfDay),
                        "PeriodDayEncoded",
                        "ItemEncoded"))
                    .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                        labelColumnName: "Label",
                        featureColumnName: "Features"));

                var model = pipeline.Fit(split.TrainSet);

                var testPredictions = model.Transform(split.TestSet);
                var metrics = _mlContext.BinaryClassification.Evaluate(testPredictions, "Label");

                _binaryAccuracy = Math.Round(metrics.Accuracy, 4);
                _binaryAUC = Math.Round(metrics.AreaUnderRocCurve, 4);
                _binaryF1 = Math.Round(metrics.F1Score, 4);

                _dayTypeEngine = _mlContext.Model.CreatePredictionEngine<DayTypeInput, DayTypeOutput>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training day type classification model.");
                throw new ClassificationTrainingException("Error training day type classification model.",ex);
            }
        }

        private void TrainForecastingModel(List<Sale> sales)
        {
            try
            {
                var dailySales = sales
               .GroupBy(s => s.DateTime.Date)
               .Select(g => new { Date = g.Key, Count = (float)g.Count() })
               .OrderBy(d => d.Date)
               .ToList();

                if (dailySales.Count < 14)
                {
                    return;
                }

                var startDate = dailySales.First().Date;
                var endDate = dailySales.Last().Date;

                var salesByDate = dailySales.ToDictionary(d => d.Date, d => d.Count);
                var timeSeriesData = Enumerable.Range(0, (endDate - startDate).Days + 1)
                    .Select(i => new TimeSeriesInput
                    {
                        DailySalesCount = salesByDate.GetValueOrDefault(startDate.AddDays(i), 0f)
                    })
                    .ToList();

                var dataView = _mlContext.Data.LoadFromEnumerable(timeSeriesData);
                var windowSize = Math.Min(7, timeSeriesData.Count / 4);
                var seriesLength = Math.Min(30, timeSeriesData.Count / 2);

                var pipeline = _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(TimeSeriesForecastOutput.ForecastedSales),
                    inputColumnName: nameof(TimeSeriesInput.DailySalesCount),
                    windowSize: windowSize,
                    seriesLength: seriesLength,
                    trainSize: timeSeriesData.Count,
                    horizon: 30,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: nameof(TimeSeriesForecastOutput.LowerBoundSales),
                    confidenceUpperBoundColumn: nameof(TimeSeriesForecastOutput.UpperBoundSales));

                _forecastModel = pipeline.Fit(dataView);
                _forecastTrained = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training forecasting model.");
                throw new ForecastingTrainingException("Error training forecasting model",ex);
            }
        }

        #endregion
    }
}
