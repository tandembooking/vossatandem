using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TandemBooking.Models
{
    public class VippsSettlement
    {
        public Guid Id { get; set; }
        public PaymentAccount PaymentAccount { get; set; }
        public string ExternalRef { get; set; }
        public DateTime ImportDate { get; set; }
    }
}
