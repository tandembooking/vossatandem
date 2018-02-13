using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
        private static string[] _monthNames = new[]
        {
            "January",
            "February",
            "March",
            "April",
            "May",
            "June",
            "July",
            "August",
            "September",
            "October",
            "November",
            "December"
        };
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
        public async Task<ActionResult> Index(BookingViewModel input, string Action=null)
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
                    
                    
                   
                    if (Action == "show_calender" )
                    {
                        
                        input.Calendar = UpdateBookingCalendar(input.Calendar, DateTime.UtcNow);
                        return View(input);
                    }
                    if (Action == "next_month")
                    {
                        
                        input.Calendar = UpdateBookingCalendar(input.Calendar, input.NextDate??DateTime.UtcNow);
                        return View(input);
                    }
                    if (Action == "prev_month")
                    {
                        
                        input.Calendar = UpdateBookingCalendar(input.Calendar, input.PrevDate ?? DateTime.UtcNow);
                        return View(input);
                    }
                    if (input.TimeSlot > 5 || input.TimeSlot < 0)
                    {
                        ModelState.AddModelError("TimeSlot", "Please select a time slot");

                        return View(input);
                    }
                    var booking = new Booking()
                    {
                        BookingDate = input.Date.Value.Date,
                        TimeSlot = input.TimeSlot,
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
                            TimeSlot = input.TimeSlot,
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
        
      
        public BookingCalendarViewModel UpdateBookingCalendar(BookingCalendarViewModel input, DateTime date)
        {
       
            var startDate = new DateTime(date.Year, date.Month, 1);
            var endDate = startDate.AddMonths(1);

            var nextMonth = startDate.AddMonths(1);
            var prevMonth = startDate.AddMonths(-1);

            int startWeekDay = ((int)startDate.DayOfWeek - 1 + 7) % 7;
            startDate = startDate.AddDays(-startWeekDay);

            int endWeekDay = ((int)endDate.DayOfWeek - 1 + 7) % 7;
            endDate = endDate.AddDays(6 - endWeekDay);

            var pilots = _context.Users.ToList();

            var availabilities = _context.PilotAvailabilities
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToList();

            var pilotBookings = _context.BookedPilots
                .Include(b => b.Booking)
                .Where(b => !b.Canceled && b.Booking.BookingDate >= startDate && b.Booking.BookingDate <= endDate)
                .ToList();


    

            var days = new List<BookingCalendarDayViewModel>();

            for (var dayIndex = 0; startDate.AddDays(dayIndex) <= endDate; dayIndex++)
            {
                var curDate = startDate.AddDays(dayIndex);
                var availableToday = availabilities.Where(a => a.Date.Date == curDate).ToList();
                
                var bookingsToday = pilotBookings.Where(a => a.Booking.BookingDate.Date == curDate).ToList();

                var timeSlots = new List<BookingCalendarTimeSlotViewModel>();

                for (var timeSlotIndex = 0; timeSlotIndex < 5; timeSlotIndex++)
                {
                    var availableNow = availableToday.Where(a => a.TimeSlot == timeSlotIndex);
                    var bookingsNow = bookingsToday.Where(a => a.Booking.TimeSlot == timeSlotIndex);
                    timeSlots.Add(new BookingCalendarTimeSlotViewModel()
                    {
                        TimeSlot = timeSlotIndex,
                        AvailablePilots = availableNow.ToList().Count,
                        FreePilots = availableNow.Where(a => bookingsNow.All(b => b.Pilot != a.Pilot)).ToList().Count,
                        InPast = curDate < DateTime.Now.Date,
                    });
                }
                days.Add(new BookingCalendarDayViewModel()
                {
                    Date = curDate,
                    TimeSlots = timeSlots,
                    InPast = curDate < DateTime.Now.Date,
                });
            }

            return new BookingCalendarViewModel()
            {
                Next = nextMonth,
                Prev = prevMonth,
                MonthName = $"{_monthNames[date.Month - 1]} {date.Year}",
                StartDate = startDate,
                EndDate = endDate,
                Days = days,
            };
        }

   
    }
}
