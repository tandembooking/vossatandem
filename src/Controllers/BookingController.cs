using System;
using System.Collections.Generic;
using System.Data;
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
        private readonly INexmoService _nexmo;
        private readonly TandemBookingContext _context;
        private readonly BookingService _bookingService;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;
        private readonly MessageService _messageService;

        public BookingController(INexmoService nexmo, TandemBookingContext context, BookingCoordinatorSettings bookingCoordinatorSettings, BookingService bookingService, MessageService messageService, ILogger<BookingController> logger)
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
                    if (string.IsNullOrWhiteSpace(input.Name))
                    {
                        ModelState.AddModelError("Name", "Please enter your name");
                        return View(input);
                    }
                    if (input.AdditionalPassengers?.Any(a => string.IsNullOrWhiteSpace(a.Name)) ?? false)
                    {
                        ModelState.AddModelError("AdditionalPassengers",
                            "Please enter the name of the additional passengers");
                        return View(input);
                    }

                    var booking = new Booking()
                    {
                        BookingDate = input.Date.Value.Date,
                        DateRegistered = DateTime.UtcNow,
                        PassengerEmail = input.Email,
                        PassengerName = input.Name,
                        PassengerPhone = phoneNumber,
                        PassengerWeight = input.Weight,
                        PassengerFee = _bookingCoordinatorSettings.DefaultPassengerFee,
                        Comment = input.Comment,
                        BookingEvents = new List<BookingEvent>(),
                    };
                    _context.Add(booking);

                    var additionalPassengers = input.AdditionalPassengers
                        ?.Select((a, i) => new
                        {
                            Name = a.Name,
                            Weight = a.Weight,
                            Index = i + 1,
                        })
                        .ToList();

                    if (additionalPassengers?.Any() ?? false)
                    {
                        booking.Comment += $", booking 1/{additionalPassengers.Count() + 1}";
                    }

                    //Create separate bookings for the additional passengers
                    var additionalBookings = additionalPassengers?.Select(additionalPassenger =>
                    {
                        var commentExtra = $"booking {additionalPassenger.Index + 1}/{additionalPassengers.Count() + 1}";
                        var additionalBooking = new Booking()
                        {
                            BookingDate = input.Date.Value.Date,
                            PrimaryBooking = booking,
                            DateRegistered = DateTime.UtcNow,
                            PassengerEmail = input.Email,
                            PassengerName = additionalPassenger.Name,
                            PassengerWeight = additionalPassenger.Weight,
                            PassengerPhone = phoneNumber,
                            PassengerFee = booking.PassengerFee,
                            Comment = $"{input.Comment}, {commentExtra}",
                            BookingEvents = new List<BookingEvent>(),
                        };
                        _context.Add(additionalBooking);

                        return additionalBooking;
                    }).ToArray() ?? new Booking[] { };

                    //Assign pilots
                    var bookings = new [] {booking}.Union(additionalBookings).ToList();
                    await _bookingService.AssignNewPilotAsync(bookings);
                    _context.SaveChanges();

                    await _messageService.SendNewBookingMessage(booking, additionalBookings, true, true);

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
