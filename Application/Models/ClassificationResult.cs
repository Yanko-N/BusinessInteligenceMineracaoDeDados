namespace Application.Models
{
    public class ClassificationResult
    {
        public string PredictedProduct { get; set; } = string.Empty;
        public List<ProductScore> TopPredictions { get; set; } = new();
        public double ModelAccuracy { get; set; }
        public double ModelLogLoss { get; set; }
        public int HourOfDay { get; set; }
        public string DayName { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string DayType { get; set; } = string.Empty;
    }

    public class ProductScore
    {
        public string ProductName { get; set; } = string.Empty;
        public double Score { get; set; }
        public double Percentage { get; set; }
    }
}
