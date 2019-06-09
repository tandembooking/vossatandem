using System;

namespace TandemBooking.Models
{
    public class PilotAvailability {
        public Guid Id {get;set;}
        public DateTime Date {get; set;}
        public Location Location { get; set; }

        public string PilotId { get; set; }
        public ApplicationUser Pilot {get;set;}
    }
}