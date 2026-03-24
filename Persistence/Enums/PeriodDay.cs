using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Enums
{
    public record PeriodDay
    {
        public static readonly PeriodDay Morning = new("morning");
        public static readonly PeriodDay Afternoon = new("afternoon");
        public static readonly PeriodDay Evening = new("evening");

        public string Value { get; }

        private PeriodDay(string value) => Value = value;

        public override string ToString() => Value;
    }
}
