using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace TandemBooking.ViewModels.Booking
{
    public class AdditionalPassengerViewModel
    {
        public string Name { get; set; }
        public int? Weight { get; set; }
    }

    public class BookingViewModel
    {
        [Required(ErrorMessage = "Vennligst velg en dato")]
        [DataType(DataType.Date, ErrorMessage="Vennligst legg inn en gyldig dato")]
        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Vennligst legg inn navnet ditt")]
        public string Name { get; set; }

        public int? Weight { get; set; }

        public AdditionalPassengerViewModel[] AdditionalPassengers { get; set; }

        [Phone(ErrorMessage = "Vi trenger et telefonnummer for å kontakte deg angående flyturen. Vennligst legg inn et gyldig telefonnummer.")]
        [Required(ErrorMessage = "Vi trenger et telefonnummer for å kontakte deg angående flyturen. Vennligst legg inn et gyldig telefonnummer.")]
        public string PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Vennligst legg inn en gyldig epost-adresse")]
        public string Email { get; set;  }

        //[Required(ErrorMessage = "Please enter a valid number of passengers or 1 if you're the only one flying")]   
        //public int Passengers { get; set; }

        [DataType(DataType.MultilineText)]
        public string Comment { get; set; }
    }
}
