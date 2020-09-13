using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TandemBooking.Models
{
    public class PaymentAccount
    {
        public Guid Id { get; set; }
        public PaymentType PaymentType { get; set; }
        public string ExternalRef { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
    }

}
