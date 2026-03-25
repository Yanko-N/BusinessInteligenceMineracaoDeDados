using Application.Models;
using Persistence.Models;

namespace Application.Core
{
    public class SalesTrainer
    {
        public bool IsTrained { get; private set; }

        private Dictionary<(int DayOfWeek, string TypeOfDay), double> _dailyAverages = new();
        private Dictionary<(int DayOfWeek, string PeriodDay, string TypeOfDay), double> _periodAverages = new();
        private Dictionary<(int DayOfWeek, string PeriodDay, string TypeOfDay), List<(string Item, int Count)>> _topProducts = new();
        private Dictionary<(int DayOfWeek, string PeriodDay, string TypeOfDay), (int SampleCount, double StdDev)> _periodStats = new();
        private double _overallDailyAvg;
        private double _rSquared;
        private double _mae;
        private double _rmse;

        public TrainResult Train(List<Sale> sales)
        {
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

            _dailyAverages = dailyCounts
                .GroupBy(d => (d.DayOfWeek, d.TypeOfDay))
                .ToDictionary(g => g.Key, g => g.Average(d => d.Count));

            // Period averages per (dayOfWeek, period, typeOfDay, date)
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

            // Compute sample count and standard deviation per period group
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

            // R², MAE, RMSE — measure how well group averages predict each day's actual count
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

            IsTrained = true;

            return new TrainResult
            {
                TotalRecords = sales.Count,
                UniqueDays = dailyCounts.Count,
                AverageDailySales = Math.Round(_overallDailyAvg, 1),
                UniqueProducts = sales.Select(s => s.Item).Distinct().Count(),
                RSquared = _rSquared,
                MAE = _mae,
                RMSE = _rmse
            };
        }

        public PredictionResult Predict(int dayOfWeek, string periodDay, string typeOfDay)
        {
            var dayName = ((DayOfWeek)dayOfWeek).ToString();

            var dailyKey = (dayOfWeek, typeOfDay);
            var predictedDaily = _dailyAverages.GetValueOrDefault(dailyKey, _overallDailyAvg);

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

            // Confidence: higher sample + lower relative stddev = higher confidence (0-100 scale)
            var confidence = sampleCount > 0 && predictedPeriod > 0
                ? Math.Round(Math.Max(0, Math.Min(100, 100 * (1 - stdDev / predictedPeriod) * Math.Min(1, sampleCount / 10.0))), 1)
                : 0;

            return new PredictionResult
            {
                PredictedDailySalesCount = Math.Round(predictedDaily, 1),
                PredictedPeriodSalesCount = Math.Round(predictedPeriod, 1),
                TopProducts = topProducts,
                DayName = dayName,
                Period = periodDay,
                DayType = typeOfDay,
                RSquared = _rSquared,
                MAE = _mae,
                RMSE = _rmse,
                Confidence = confidence,
                SampleCount = sampleCount,
                StandardDeviation = stdDev
            };
        }
    }
}
