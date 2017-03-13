using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TandemBooking.Models;
using Microsoft.EntityFrameworkCore;
using TandemBooking.Services;

namespace TandemBooking.Controllers
{
    [Authorize(Policy="IsValidated")]
    public class ReportController : Controller
    {
        private readonly TandemBookingContext _context;
        private readonly UserManager _userManager;

        public ReportController(TandemBookingContext context, UserManager userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> CompletedBookings(string pilotId = null, int? year = null)
        {
            var fromDate = new DateTime(year ?? DateTime.Today.Year, 1, 1);
            var toDate = fromDate.AddYears(1);
            if (toDate > DateTime.Today)
            {
                toDate = DateTime.Today;
            }

            var bookings = _context.Bookings
                .Include(b => b.AssignedPilot)
                .AsNoTracking()
                .Where(b => !b.Canceled && b.BookingDate >= fromDate && b.BookingDate < toDate);

            //Pilots should be able to see their own flights
            if (!User.IsAdmin())
            {
                pilotId = _userManager.GetUserId(User);
            }
                
            if (pilotId != null)
            {
                bookings = bookings.Where(b => b.AssignedPilot.Id == pilotId);
            }

            var pilotName = pilotId == null
                ? "all pilots"
                : (await _userManager.FindByIdAsync(pilotId)).Name;

            var orderedBookings = bookings
                .OrderBy(b => b.BookingDate)
                .ToList();

            return View(new CompletedBookingsViewModel()
            {
                Bookings = orderedBookings,
                Year = fromDate.Year,
                PilotId = pilotId,
                PilotName = pilotName
            });
        }

        [Authorize(Policy = "IsAdmin")]
        public IActionResult BookingsByPilot(int? year = null)
        {
            var fromDate = new DateTime(year ?? DateTime.Today.Year, 1, 1);
            var toDate = fromDate.AddYears(1);
            if (toDate > DateTime.Today)
            {
                toDate = DateTime.Today;
            }


            var pilotStats = _context.Bookings
                .Include(b => b.AssignedPilot)
                .AsNoTracking()
                .Where(b => !b.Canceled && b.AssignedPilot != null && b.BookingDate >= fromDate && b.BookingDate < toDate)
                .GroupBy(b => b.AssignedPilot)
                .Select(grp => new BookingsByPilotViewModelItem
                {
                    PilotId = grp.Key.Id,
                    PilotName = grp.Key.Name,
                    Flights = grp.Count()
                })
                .ToList()
                .OrderByDescending(b => b.Flights)
                .ToList();

            return View(new BookingsByPilotViewModel()
            {
                Year = fromDate.Year,
                PilotStats = pilotStats,
            });
        }
    }
}