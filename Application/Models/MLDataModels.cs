using Microsoft.ML.Data;

namespace Application.Models
{
    //  Regression (R1: Daily Sales Count
    public class DailySalesInput
    {
        public float DayOfWeek { get; set; }
        public float Month { get; set; }
        public float IsWeekend { get; set; }
        public float Label { get; set; }
    }

    public class DailySalesOutput
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }

    //  Multi-class Classification Product
    public class ProductClassificationInput
    {
        public float HourOfDay { get; set; }
        public float DayOfWeek { get; set; }
        public float Month { get; set; }
        public string PeriodDay { get; set; } = string.Empty;
        public string TypeOfDay { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
    }

    public class ProductClassificationOutput
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;
        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }

    //  Binary Classification  Weekday/Weekend
    public class DayTypeInput
    {
        public float HourOfDay { get; set; }
        public string PeriodDay { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public bool Label { get; set; }
    }

    public class DayTypeOutput
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }
        [ColumnName("Probability")]
        public float Probability { get; set; }
        [ColumnName("Score")]
        public float Score { get; set; }
    }

    //  Forecasting SSA
    public class TimeSeriesInput
    {
        public float DailySalesCount { get; set; }
    }

    public class TimeSeriesForecastOutput
    {
        public float[] ForecastedSales { get; set; } = Array.Empty<float>();
        public float[] LowerBoundSales { get; set; } = Array.Empty<float>();
        public float[] UpperBoundSales { get; set; } = Array.Empty<float>();
    }
}
