using Persistence.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Models
{
    public class Sale
    {
        public int Transaction { get; set; }
        public string Item { get; set; }
        public DateTime DateTime { get; set; }
        public PeriodDay PeriodDay { get; set; }

    }
}
