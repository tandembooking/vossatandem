using System;
using System.Collections.Generic;

namespace TandemBooking.Models
{
    public enum FlightType
    {
        Other = 0,
        Hangur = 1,
        Winch = 2,
        MyrkdalenRokneLiaset = 3,
        Aurland = 4,
    }

    public enum PaymentType
    {
        IZettle = 1,
        Vipps = 2
    }


    public class Booking {
        public Guid Id {get;set;}
        public DateTime DateRegistered {get;set;}
        public DateTime BookingDate {get;set;}
        public bool Canceled { get; set; }
        public bool Completed { get; set; }
        public decimal PassengerFee { get; set; }
        public int? PassengerWeight { get; set; }

        public string PassengerName {get;set;}
        public string PassengerEmail {get;set;}
        public string PassengerPhone {get;set;} 
        public string Comment {get;set;}
        
        public FlightType? FlightType { get; set; }
        public PaymentType? PaymentType { get; set; }
        public string BoatDriver { get; set; }
        public decimal? PilotFee { get; set; }
        public decimal? BoatDriverFee { get; set; }
        public DateTime? CompletedDate { get; set; }

        public string AssignedPilotId { get; set; }
        public ApplicationUser AssignedPilot { get; set; }

        public Booking PrimaryBooking { get; set; }
        public ICollection<Booking> AdditionalBookings { get; set; }

        public Location BookingLocation { get; set; }
        public string IZettleAccount { get; set; }
        public string VippsAccount { get; set; }

        public ICollection<BookedPilot> BookedPilots { get; set; }
        public ICollection<BookingEvent> BookingEvents { get; set; }
    }
}