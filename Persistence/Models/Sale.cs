using Persistence.Enums;

namespace Persistence.Models
{
    public class Sale
    {
        public int Transaction { get; set; }
        public string Item { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public PeriodDay PeriodDay { get; set; } = PeriodDay.Morning;

        public TypeOfDay TypeOfDay { get; set; } = TypeOfDay.Weekday;

    }
}
