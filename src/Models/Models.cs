using System;

namespace TandemBooking.Models {
    public class PilotAvailability {
        public Guid Id {get;set;}
        public DateTime Date {get; set;}
        public ApplicationUser Pilot {get;set;}
    }
    
    public class Booking {
        public Guid Id {get;set;}
        public DateTime DateRegistered {get;set;}
        public DateTime BookingDate {get;set;}
        
        public string PassengerName {get;set;}
        public string PassengerEmail {get;set;}
        public string PassengerPhone {get;set;} 
        public string Comment {get;set;}
    }

    public class BookedPilot
    {
        public Guid Id { get; set; }
        public Booking Booking { get; set; }
        public bool Confirmed { get; set; }
    }

    public class BookingEvent
    {
        public Guid Id { get; set; }
        public Booking Booking { get; set; }
        public DateTime EventDate { get; set; }
        public string EventMessage { get; set; }
    }

}