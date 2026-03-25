namespace Application.Models
{
    public class TrainResult
    {
        public int TotalRecords { get; set; }
        public int UniqueDays { get; set; }
        public double AverageDailySales { get; set; }
        public int UniqueProducts { get; set; }
        public double RSquared { get; set; }
        public double MAE { get; set; }
        public double RMSE { get; set; }
    }
}