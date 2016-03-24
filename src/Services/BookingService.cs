using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TandemBooking.Models;

namespace TandemBooking.Services
{
    public class AvailablePilot
    {
        public ApplicationUser Pilot { get; set; }
        public int Priority { get; set; } 
    }

    public class BookingService
    {
        private readonly TandemBookingContext _context;

        public BookingService(TandemBookingContext context)
        {
            _context = context;
        }

        public List<AvailablePilot> FindAvailablePilots(DateTime date)
        {
            //find list of available pilots pilots having the least amount of flights
            //during the last 30 days
            return _context.PilotAvailabilities
                .Where(pa => pa.Date.Date == date && pa.Pilot.IsPilot)
                .Select(pa => new
                {
                    Pilot = pa.Pilot,
                    Bookings = pa.Pilot.Bookings.Where(b => b.Booking.BookingDate > DateTime.UtcNow.AddDays(-30) && !b.Booking.Canceled)
                })
                .ToList()
                .Select(pa => new AvailablePilot()
                {
                    Pilot = pa.Pilot,
                    Priority = pa.Bookings?.Count() ?? 0
                })
                .ToList();

        }

        public ApplicationUser AssignNewPilot(Booking booking)
        {
            var spentPilots = booking.BookedPilots?.Select(bp => bp.Pilot) ?? new List<ApplicationUser>();

            var availablePilots = FindAvailablePilots(booking.BookingDate)
                .Where(ap => !spentPilots.Contains(ap.Pilot));

            var prioritizedPilots = availablePilots
                .GroupBy(pa => pa.Priority)
                .OrderBy(grp => grp.Key)
                .FirstOrDefault()
                ?.ToList() ?? new List<AvailablePilot>()
                ;

            if (prioritizedPilots.Count > 0)
            {
                var selectedPilot = prioritizedPilots[new Random().Next(prioritizedPilots.Count - 1)].Pilot;

                //set as currently assigned pilot
                booking.AssignedPilot = selectedPilot;

                //add to list of pilots assigned to this booking
                if (booking.BookedPilots == null)
                {
                    booking.BookedPilots = new List<BookedPilot>();
                }
                booking.BookedPilots.Add(new BookedPilot()
                {
                    AssignedDate = DateTime.UtcNow,
                    Pilot = selectedPilot,
                });

                return selectedPilot;
            }
            else
            {
                booking.AssignedPilot = null;
            }

            return null;
        }

        public void AddEvent(Booking booking, ClaimsPrincipal user, string message)
        {
            var userId = user?.GetUserId();
            booking.BookingEvents.Add(new BookingEvent()
            {
                User = _context.Users.FirstOrDefault(u => u.Id == userId),
                EventDate = DateTime.UtcNow,
                EventMessage = message,
            });
        }
    }
}
