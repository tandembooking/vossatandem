using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TandemBooking.Models;
using Microsoft.Data.Entity;

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

        public BookingService(TandemBookingContext context)
        {
            _context = context;
        }

        public List<AvailablePilot> FindAvailablePilots(DateTime date, bool includeUnavailable = false)
        {
            //find list of available pilots pilots having the least amount of flights
            //during the 30 days prior to and 14 days after the booking date
            var pilots = _context.Users
                .Where(u => u.IsPilot)
                .Select(u => new
                {
                    Availabilities = u.Availabilities.Where(a => a.Date.Date == date.Date).ToList(),
                    Pilot = u,
                    Bookings =
                        u.Bookings
                            .Where(b =>
                                b.Booking.BookingDate > DateTime.UtcNow.AddDays(-30)
                                && b.Booking.BookingDate < DateTime.UtcNow.AddDays(14)
                                && !b.Canceled 
                                && !b.Booking.Canceled
                            ).ToList(),
                    BookingsToday =
                        u.Bookings
                            .Where(b => 
                                b.Booking.BookingDate.Date == date.Date 
                                && !b.Canceled 
                                && !b.Booking.Canceled
                            ).ToList(),
                })
                .ToList();

            var availablePilots = pilots
                .Select(pa => new AvailablePilot()
                {
                    Pilot = pa.Pilot,
                    Priority = (pa.Bookings?.Count() ?? 0) 
                        + (1000 * pa.BookingsToday?.Count() ?? 0), // a booking the same day are weighted more heavily to avoid pilots getting too many flights a day
                    Available = (pa.Availabilities?.Count() ?? 0) > 0
                })
                .Where(ap => ap.Available || includeUnavailable)
                .ToList();

            return availablePilots;
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

            var selectedPilot = prioritizedPilots.Count > 0 
                ? prioritizedPilots[new Random().Next(prioritizedPilots.Count - 1)].Pilot
                : null;

            AssignNewPilot(booking, selectedPilot);

            return selectedPilot;
        }

        public void AssignNewPilot(Booking booking, ApplicationUser pilot)
        {
            if (pilot != null)
            {
                //set all other booked pilots to canceled
                foreach (var bookedPilot in _context.BookedPilots.Where(b => b.Booking.Id == booking.Id))
                {
                    bookedPilot.Canceled = true;
                }

                //set as currently assigned pilot
                booking.AssignedPilot = pilot;

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
            else
            {
                booking.AssignedPilot = null;
            }
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
