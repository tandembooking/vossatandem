using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TandemBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace TandemBooking.Services
{
    public class AvailablePilot
    {
        public ApplicationUser Pilot { get; set; }
        public int Priority { get; set; } 
        public bool Available { get; set; }
    }

    public class BookingService
    {
        private readonly TandemBookingContext _context;
        private readonly UserManager _userManager;
        private readonly BookingServiceDb _bookingServiceDb;

        public BookingService(TandemBookingContext context, UserManager userManager, BookingServiceDb bookingServiceDb)
        {
            _context = context;
            _userManager = userManager;
            _bookingServiceDb = bookingServiceDb;
        }

        public async Task<List<AvailablePilot>> FindAvailablePilotsAsync(DateTime date, bool includeUnavailable = false)
        {
            var result = await _bookingServiceDb.GetAvailablePilotsAsync(date);
            if (includeUnavailable)
            {
                return result;
            }
            else
            {
                return result
                    .Where(a => a.Available)
                    .ToList();
            }
        }

        public async Task<ApplicationUser> AssignNewPilotAsync(Booking booking)
        {
            var spentPilots = booking.BookedPilots?.Select(bp => bp.Pilot) ?? new List<ApplicationUser>();

            var availablePilots = (await FindAvailablePilotsAsync(booking.BookingDate))
                .Where(ap => !spentPilots.Contains(ap.Pilot));

            var prioritizedPilots = availablePilots
                .GroupBy(pa => pa.Priority)
                .OrderBy(grp => grp.Key)
                .FirstOrDefault()
                ?.ToList() ?? new List<AvailablePilot>()
                ;

            var selectedPilot = prioritizedPilots.Count > 0 
                ? prioritizedPilots[new Random().Next(prioritizedPilots.Count - 1)].Pilot
                : null;

            AssignNewPilot(booking, selectedPilot);

            return selectedPilot;
        }

        public void AssignNewPilot(Booking booking, ApplicationUser pilot)
        {
            //set all other booked pilots to canceled
            foreach (var bookedPilot in _context.BookedPilots.Where(b => b.Booking.Id == booking.Id))
            {
                bookedPilot.Canceled = true;
            }

            //set as currently assigned pilot
            booking.AssignedPilot = pilot;

            if (pilot != null)
            {
                //add to list of pilots assigned to this booking
                if (booking.BookedPilots == null)
                {
                    booking.BookedPilots = new List<BookedPilot>();
                }
                booking.BookedPilots.Add(new BookedPilot()
                {
                    AssignedDate = DateTime.UtcNow,
                    Pilot = pilot,
                });
            }
        }

        public void AddEvent(Booking booking, ClaimsPrincipal user, string message)
        {
            var userId = user == null ? null : _userManager.GetUserId(user);
            booking.BookingEvents.Add(new BookingEvent()
            {
                User = _context.Users.FirstOrDefault(u => u.Id == userId),
                EventDate = DateTime.UtcNow,
                EventMessage = message,
            });
        }
    }
}
