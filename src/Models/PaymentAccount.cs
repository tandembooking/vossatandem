using System;

namespace TandemBooking.Models
{
    public class PaymentAccount
    {
        public Guid Id { get; set; }
        public string ExternalRef { get; set; }
        public string Name { get; set; }
    }

}
