namespace Application.Models
{
    public class DayTypePredictionResult
    {
        public bool PredictedIsWeekend { get; set; }
        public string PredictedDayType { get; set; } = string.Empty;
        public double Probability { get; set; }
        public double ModelAccuracy { get; set; }
        public double ModelAUC { get; set; }
        public double ModelF1 { get; set; }
        public int HourOfDay { get; set; }
        public string Period { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
    }
}
