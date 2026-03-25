namespace Application.Models
{
    public class PredictionResult
    {
        public double PredictedDailySalesCount { get; set; }
        public double PredictedPeriodSalesCount { get; set; }
        public List<ProductPrediction> TopProducts { get; set; } = new();
        public string DayName { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string DayType { get; set; } = string.Empty;
        public double RSquared { get; set; }
        public double MAE { get; set; }
        public double RMSE { get; set; }
        public double Confidence { get; set; }
        public int SampleCount { get; set; }
        public double StandardDeviation { get; set; }
    }
}