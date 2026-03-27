namespace Application.Models
{
    public class ForecastResult
    {
        public List<ForecastPoint> Forecast { get; set; } = new();
        public int Horizon { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class ForecastPoint
    {
        public DateTime Date { get; set; }
        public double PredictedSales { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
    }
}
