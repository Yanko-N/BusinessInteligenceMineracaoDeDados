
namespace Persistence.Enums
{
    public record PeriodDay
    {
        public static readonly PeriodDay Morning = new("morning");
        public static readonly PeriodDay Afternoon = new("afternoon");
        public static readonly PeriodDay Evening = new("evening");
        public static readonly PeriodDay Night = new("night");

        public string Value { get; }

        private PeriodDay(string value) => Value = value;

        public override string ToString() => Value;
    }
}
