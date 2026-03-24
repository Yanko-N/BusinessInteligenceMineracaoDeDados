using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Enums
{
    public record TypeOfDay
    {
        public static readonly TypeOfDay Weekday = new("weekday");
        public static readonly TypeOfDay Weekend = new("weekend");
        public string Value { get; }
        private TypeOfDay(string value) => Value = value;
        public override string ToString() => Value;
    }
}
