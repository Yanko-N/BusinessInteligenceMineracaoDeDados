namespace Application.Models
{
    public class TrainResult
    {
        // Statistical regression
        public int TotalRecords { get; set; }
        public int UniqueDays { get; set; }
        public double AverageDailySales { get; set; }
        public int UniqueProducts { get; set; }
        public double RSquared { get; set; }
        public double MAE { get; set; }
        public double RMSE { get; set; }

        // ML.NET Regression
        public double MLRSquared { get; set; }
        public double MLMAE { get; set; }
        public double MLRMSE { get; set; }

        // Multi-class Classification (product)
        public double ClassificationAccuracy { get; set; }
        public double ClassificationLogLoss { get; set; }

        // Binary Classification (day type)
        public double BinaryAccuracy { get; set; }
        public double BinaryAUC { get; set; }
        public double BinaryF1 { get; set; }

        // Forecasting
        public bool ForecastingTrained { get; set; }
    }
}