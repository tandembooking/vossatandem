using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TandemBooking.Models
{
    public class Payment
    {
        public Guid Id { get; set; }
        public PaymentType PaymentType { get; set; }
        public string ExternalRef { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal UnreconciledAmount { get; set; }
        public DateTimeOffset? InsertDate { get; set; }
        public DateTimeOffset PaymentDate { get; set; }

        public Guid PaymentAccountId { get; set; }
        public PaymentAccount PaymentAccount { get; set; }
    }
}
