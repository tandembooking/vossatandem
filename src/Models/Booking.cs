using System;
using System.Collections.Generic;

namespace TandemBooking.Models
{
    public class Booking {
        public Guid Id {get;set;}
        public DateTime DateRegistered {get;set;}
        public DateTime BookingDate {get;set;}
        public bool Canceled { get; set; }

        public string PassengerName {get;set;}
        public string PassengerEmail {get;set;}
        public string PassengerPhone {get;set;} 
        public string Comment {get;set;}

        public ApplicationUser AssignedPilot { get; set; }

        public ICollection<BookedPilot> BookedPilots { get; set; }
        public ICollection<BookingEvent> BookingEvents { get; set; }
    }
}