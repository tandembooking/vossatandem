using System;
using System.Collections.Generic;
using TandemBooking.Models;

namespace TandemBooking.ViewModels.Booking
{
    public class BookingCalendarViewModel
    {
        public DateTime? SelectedDate { get; set; }
        public DateTime Next { get; set; }
        public DateTime Prev { get; set; }
        public string MonthName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<BookingCalendarDayViewModel> Days { get; set; }
    }

    public class BookingCalendarDayViewModel
    {
        public DateTime Date { get; set; }
        public List<BookingCalendarTimeSlotViewModel> TimeSlots { get; set; }
        public bool InPast { get; set; }
    }
    public class BookingCalendarTimeSlotViewModel
    {
        public int TimeSlot { get; set; }
        public int AvailablePilots { get; set; }
        public int FreePilots { get; set; }
        public bool InPast { get; set; }
    }
}