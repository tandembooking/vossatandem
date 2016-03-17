using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TandemBooking.Models;
using TandemBooking.Services;
using TandemBooking.ViewModels.Booking;

namespace TandemBooking.Controllers
{
    public class BookingController: Controller
    {
        private readonly ILogger _logger;
        private readonly NexmoService _nexmo;
        private readonly TandemBookingContext _context;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;

        public BookingController(NexmoService nexmo, TandemBookingContext context, BookingCoordinatorSettings bookingCoordinatorSettings, ILoggerFactory loggerFactory)
        {
            _nexmo = nexmo;
            _context = context;
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
            _logger = loggerFactory.CreateLogger<BookingController>();
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View(new BookingViewModel()
            {
                //Passengers = 1,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(BookingViewModel input)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //validate phone number
                    var phoneNumber = await _nexmo.FormatPhoneNumber(input.PhoneNumber);
                    if (phoneNumber == null)
                    {
                        ModelState.AddModelError("PhoneNumber", "Please enter a valid phone number");
                        return View(input);
                    }

                    var date = input.Date.Value.Date;

                    var booking = new Booking()
                    {
                        BookingDate = input.Date.Value.Date,
                        DateRegistered = DateTime.UtcNow,
                        PassengerEmail = input.Email,
                        PassengerName = input.Name,
                        PassengerPhone = phoneNumber,
                        Comment = input.Comment,
                    };
                    _context.Add(booking);


                    //find list of available pilots pilots having the least amount of flights
                    //during the last 30 days
                    var availablePilots = _context.PilotAvailabilities
                        .Where(pa => pa.Date.Date == date && pa.Pilot.IsPilot)
                        .Select(pa => new
                        {
                            Pilot = pa.Pilot,
                            Bookings = pa.Pilot.Bookings.Where(b => b.Booking.BookingDate > DateTime.UtcNow.AddDays(-30))
                        })
                        .ToList()
                        .GroupBy(pa => pa.Bookings.Count())
                        .OrderBy(grp => grp.Key)
                        .FirstOrDefault()
                        .ToList()
                        ;


                    ApplicationUser selectedPilot = null;
                    if (availablePilots.Count > 0)
                    {
                        //todo: improve pilot selection algorithm to take into account the amount of flights 
                        selectedPilot = availablePilots[new Random().Next(availablePilots.Count - 1)].Pilot;

                        var bookedPilot = new BookedPilot()
                        {
                            Booking = booking,
                            Pilot = selectedPilot,
                        };
                        _context.Add(bookedPilot);
                    }

                    _context.SaveChanges();

                    //send message to pilot or booking coordinator
                    var bookingDateString = booking.BookingDate.ToString("dd.MM.yyyy");
                    if (selectedPilot != null)
                    {
                        var message =
                            $"You have a new flight on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}.";
                        await _nexmo.SendSms("VossHPK", selectedPilot.PhoneNumber, message);

                        var passengerMessage =
                            $"Awesome! Your tandem flight on  is confirmed. You will be contacted by {selectedPilot.Name} ({selectedPilot.PhoneNumber}) shortly.";
                        await _nexmo.SendSms("VossHPK", booking.PassengerPhone, passengerMessage);
                    }
                    else
                    {
                        var message =
                            $"Please find a pilot on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}";
                        await _nexmo.SendSms("VossHPK", _bookingCoordinatorSettings.PhoneNumber, message);

                        var passengerMessage =
                            $"Awesome! We will try to find a pilot who can take you flying on {bookingDateString}. You will be contacted shortly.";
                        await _nexmo.SendSms("VossHPK", booking.PassengerPhone, passengerMessage);
                    }

                    return RedirectToAction("Confirm", new {bookingId = booking.Id});
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(0, $"Error creating booking, {ex.Message}, {ex}");
                ModelState.AddModelError("", $"An unexpected error occured during booking, please contact the booking coordinator, {_bookingCoordinatorSettings.Name}, on {_bookingCoordinatorSettings.PhoneNumber}");
            }

            return View(input);
        }

        public ActionResult Confirm(string bookingId)
        {

            return View();
        }
    }
}
