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
        public ApplicationUser AssignedPilot {get;set;}
        
        public string PassengerName {get;set;}
        public string PassengerEmail {get;set;}
        public string PassengerPhone {get;set;}
        public string Comment {get;set;}
    }
}