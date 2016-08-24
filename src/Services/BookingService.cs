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

        public List<AvailablePilot> FindAvailablePilots(DateTime date, bool includeUnavailable = false)
        {
            var task = _bookingServiceDb.GetAvailablePilotsAsync(date);
            task.Wait();
            if (includeUnavailable)
            {
                return task.Result;
            }
            else
            {
                return task.Result
                    .Where(a => a.Available)
                    .ToList();
            }
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
