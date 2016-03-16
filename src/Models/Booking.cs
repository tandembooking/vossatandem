using System;

namespace TandemBooking.Models
{
    public class Booking {
        public Guid Id {get;set;}
        public DateTime DateRegistered {get;set;}
        public DateTime BookingDate {get;set;}
        
        public string PassengerName {get;set;}
        public string PassengerEmail {get;set;}
        public string PassengerPhone {get;set;} 
        public string Comment {get;set;}
    }
}