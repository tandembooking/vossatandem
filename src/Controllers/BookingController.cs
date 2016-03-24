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
        private readonly BookingService _bookingService;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;

        public BookingController(NexmoService nexmo, TandemBookingContext context, BookingCoordinatorSettings bookingCoordinatorSettings, ILoggerFactory loggerFactory, BookingService bookingService)
        {
            _nexmo = nexmo;
            _context = context;
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
            _bookingService = bookingService;
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
                        BookingEvents = new List<BookingEvent>(),
                    };
                    _context.Add(booking);

                    var selectedPilot = _bookingService.AssignNewPilot(booking);
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

                        _bookingService.AddEvent(booking, null, $"Assigned to {selectedPilot.Name} ({selectedPilot.PhoneNumber})");
                        _bookingService.AddEvent(booking, null, $"Sent confirmation message to {booking.PassengerName} ({booking.PassengerPhone})");
                    }
                    else
                    {
                        var message =
                            $"Please find a pilot on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}";
                        await _nexmo.SendSms("VossHPK", _bookingCoordinatorSettings.PhoneNumber, message);

                        var passengerMessage =
                            $"Awesome! We will try to find a pilot who can take you flying on {bookingDateString}. You will be contacted shortly.";
                        await _nexmo.SendSms("VossHPK", booking.PassengerPhone, passengerMessage);

                        _bookingService.AddEvent(booking, null,
                            $"No pilots available, sent message to tandem coordinator {_bookingCoordinatorSettings.Name} ({_bookingCoordinatorSettings.PhoneNumber})");

                        _bookingService.AddEvent(booking, null,
                            $"Sent confirmation message to {booking.PassengerName} ({booking.PassengerPhone})");
                    }
                    _context.SaveChanges();

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
