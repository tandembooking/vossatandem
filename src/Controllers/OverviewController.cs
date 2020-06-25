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
    public class OverviewController : Controller
    {
        private readonly TandemBookingContext _context;
        private readonly UserManager _userManager;

        public OverviewController(TandemBookingContext context, UserManager userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var result = new OverviewViewModel();
            result.MissingPilotBookings = _context.Bookings
                .Include(b => b.AssignedPilot)
                .AsNoTracking()
                .Where(b => !b.Canceled && b.BookingDate >= DateTime.Today)
                .Where(b => b.AssignedPilot == null)
                .OrderBy(b => b.BookingDate).ThenBy(b => b.DateRegistered)
                .ToList();

            if (User.IsPilot())
            {
                result.UpcomingBookings = _context.Bookings
                    .Include(b => b.AssignedPilot)
                    .AsNoTracking()
                    .Where(b => !b.Canceled && b.BookingDate >= DateTime.Today)
                    .Where(b => b.AssignedPilotId == _userManager.GetUserId(User))
                    .OrderBy(b => b.BookingDate).ThenBy(b => b.DateRegistered)
                    .ToList();

                result.RecentBookings = _context.Bookings
                    .Include(b => b.AssignedPilot)
                    .AsNoTracking()
                    .Where(b => !b.Canceled && b.BookingDate < DateTime.Today.AddDays(1))
                    .Where(b => b.AssignedPilotId == _userManager.GetUserId(User))
                    .OrderByDescending(b => b.BookingDate).ThenBy(b => b.DateRegistered)
                    .Take(10)
                    .ToList();

            }

            return View(result);
        }
    }
}