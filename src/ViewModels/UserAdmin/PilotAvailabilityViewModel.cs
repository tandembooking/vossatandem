using System;
using System.Collections.Generic;
using TandemBooking.Models;

namespace TandemBooking.ViewModels
{
    public class PilotAvailabilityViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<PilotAvailability> Availabilities { get; set; }
        public DateTime Next { get; set; }
        public DateTime Prev { get; set; }
        public string MonthName { get; set; }
    }
}