using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using TandemBooking.Models;
using TandemBooking.ViewModels;

namespace TandemBooking.Controllers
{
    [Authorize]
    public class PilotAvailabilityController : Controller
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

        private readonly TandemBookingContext _context;

        public PilotAvailabilityController(TandemBookingContext context)
        {
            _context = context;
        }

        public ActionResult Index(DateTime? date = null)
        {
            if (date == null)
            {
                date = DateTime.UtcNow;
            }
            var startDate = new DateTime(date.Value.Year, date.Value.Month, 1);
            var endDate = startDate.AddMonths(1);

            var nextMonth = startDate.AddMonths(1);
            var prevMonth = startDate.AddMonths(-1);

            int startWeekDay = ((int) startDate.DayOfWeek - 1 + 7)%7;
            startDate = startDate.AddDays(-startWeekDay);

            int endWeekDay = ((int) endDate.DayOfWeek - 1 + 7)%7;
            endDate = endDate.AddDays(6 - endWeekDay);

            var availabilities = _context.PilotAvailabilities
                .Where(a => a.Pilot.Id == User.GetUserId())
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToList();

            return View(new PilotAvailabilityViewModel()
            {
                Next = nextMonth,
                Prev = prevMonth,   
                MonthName = $"{_monthNames[date.Value.Month-1]} {date.Value.Year}",
                StartDate = startDate,
                EndDate = endDate,
                Availabilities = availabilities
            });
        }

        public ActionResult SetAvailability(DateTime date, bool available)
        {
            if (available)
            {
                var pilotAvailability = new PilotAvailability()
                {
                    Date = date.Date,
                    Pilot = _context.Users.Single(u => u.Id == User.GetUserId())
                };
                _context.PilotAvailabilities.Add(pilotAvailability);
            }
            else
            {
                var availabilities = _context.PilotAvailabilities
                    .Where(a => a.Pilot.Id == User.GetUserId())
                    .Where(a => a.Date >= date && a.Date < date.AddDays(1))
                    .ToList();

                foreach (var pilotAvailability in availabilities)
                {
                    _context.PilotAvailabilities.Remove(pilotAvailability);
                }
            }
            _context.SaveChanges();

            return Json(new {result = "ok"});
        }

    }

}