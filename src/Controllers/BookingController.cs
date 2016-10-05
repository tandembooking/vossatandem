using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TandemBooking.Models;
using TandemBooking.Services;
using TandemBooking.ViewModels.Booking;

namespace TandemBooking.Controllers
{
    public class BookingController: Controller
    {
        private readonly ILogger<BookingController> _logger;
        private readonly NexmoService _nexmo;
        private readonly TandemBookingContext _context;
        private readonly BookingService _bookingService;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;
        private readonly MessageService _messageService;

        public BookingController(NexmoService nexmo, TandemBookingContext context, BookingCoordinatorSettings bookingCoordinatorSettings, BookingService bookingService, MessageService messageService, ILogger<BookingController> logger)
        {
            _nexmo = nexmo;
            _context = context;
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
            _bookingService = bookingService;
            _messageService = messageService;
            _logger = logger;
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
                    if (input.Date == null)
                    {
                        ModelState.AddModelError("Date", "Please select a date");
                        return View(input);
                    }

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

                    _bookingService.AssignNewPilot(booking);
                    _context.SaveChanges();

                    await _messageService.SendNewBookingMessage(booking, true, true);

                    return RedirectToAction("Confirm", new {bookingId = booking.Id});
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(0, $"Error creating booking, {ex.Message}, {ex}");
                ModelState.AddModelError("", $"An unexpected error occured during booking, please contact the booking coordinator, {_bookingCoordinatorSettings.Name}, on {_bookingCoordinatorSettings.PhoneNumber.AsPhoneNumber()}");
            }

            return View(input);
        }

        public ActionResult Confirm(string bookingId)
        {

            return View();
        }
    }
}
