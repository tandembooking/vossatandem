using System;

namespace TandemBooking.Models
{
    public class BookingPayment
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime InsertDate { get; set; }

        public Guid PaymentId { get; set; }
        public Payment Payment { get; set; }

        public Guid BookingId { get; set; }
        public Booking Booking { get; set; }
    }
}
