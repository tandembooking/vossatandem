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
        public decimal UnreconciledAmount { get; set; }
        public DateTime? InsertDate { get; set; }
        public DateTime? ConfirmedDate { get; set; }

        public Guid PaymentAccountId { get; set; }
        public PaymentAccount PaymentAccount { get; set; }
    }
}
