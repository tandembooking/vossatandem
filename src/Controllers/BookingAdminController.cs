using System;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using TandemBooking.Models;
using Microsoft.Data.Entity;
using TandemBooking.Services;
using TandemBooking.ViewModels.BookingAdmin;

namespace TandemBooking.Controllers
{
    [Authorize]
    public class BookingAdminController : Controller
    {
        private readonly TandemBookingContext _context;
        private readonly NexmoService _nexmo;
        private readonly BookingService _bookingService;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;

        public BookingAdminController(TandemBookingContext context, NexmoService nexmo, BookingService bookingService, BookingCoordinatorSettings bookingCoordinatorSettings)
        {
            _context = context;
            _nexmo = nexmo;
            _bookingService = bookingService;
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
        }

        public ActionResult Index(string userId = null)
        {
            var bookingQuery = _context.Bookings
                .Include(b => b.AssignedPilot)
                .Where(u => u.BookingDate >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(u => u.BookingDate)
                .AsQueryable();

            //non-admin users can only see their own bookings
            if (!User.IsAdmin())
            {
                userId = User.GetUserId();
            }

            if (userId != null)
            {
                bookingQuery = bookingQuery.Where(b => b.AssignedPilot.Id == userId);
            }

            //get at least 30 bookings and all bookings from the last 30days
            var bookings = bookingQuery
                .AsEnumerable()
                .TakeWhile((booking, index) => booking.BookingDate > DateTime.Now.AddDays(-30) || index < 30)
                .ToList();

            return View(bookings);
        }

        [HttpGet]
        public ActionResult Edit(Guid id, string errorMessage = null)
        {
            var booking = _context.Bookings
                .Include(b => b.BookedPilots).ThenInclude(bp => bp.Pilot)
                .Include(b => b.BookingEvents).ThenInclude(be => be.User)
                .Include(b => b.AssignedPilot)
                .FirstOrDefault(b => b.Id == id);

            var userId = User.GetUserId();
            if (!User.IsAdmin() && booking.AssignedPilot.Id == userId)
            {
                return new HttpUnauthorizedResult();
            }

            var vm = new BookingDetailsViewModel()
            {
                ErrorMessage = errorMessage,
                Booking = booking
            };

            if (User.IsAdmin())
            {
                vm.AvailablePilots = _bookingService.FindAvailablePilots(booking.BookingDate, true);
            }

            ViewBag.ErrorMessage = errorMessage;
            return View(vm);
        }

        [HttpPost]
        public ActionResult EditUser(Guid id, ApplicationUser input)
        {
            return RedirectToAction("Edit", new { Id = id });
        }

        [HttpPost]
        public async Task<ActionResult> Cancel(Guid id, string cancelMessage)
        {
            var booking = _context.Bookings
                .Include(b => b.BookingEvents)
                .FirstOrDefault(b => b.Id == id);

            var userId = User.GetUserId();
            if (!User.IsAdmin() && booking.AssignedPilot.Id == userId)
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(cancelMessage))
            {
                return RedirectToAction("Edit", new { Id = booking.Id });
            }

            booking.Canceled = true;
            _bookingService.AddEvent(booking, User, $"Canceled due to {cancelMessage}");

            await _nexmo.SendSms("VossHPK", booking.PassengerPhone, $"Unfortunately, your flight on {booking.BookingDate.ToString("dd.MM.yyyy")} has been canceled due to {cancelMessage}");

            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        public async Task<ActionResult> NewPilot(Guid id, NewPilotViewModel newPilot)
        {
            var booking = _context.Bookings
                .Include(b => b.AssignedPilot)
                .Include(b => b.BookingEvents)
                .Include(b => b.BookedPilots).ThenInclude(bp => bp.Pilot)
                .FirstOrDefault(b => b.Id == id);

            var userId = User.GetUserId();
            if (!User.IsAdmin() && booking.AssignedPilot.Id == userId)
            {
                return RedirectToAction("Index");
            }

            //get optional pilot id from 
            var newPilotId = User.IsAdmin() ? newPilot.NewPilotUserId : null;

            var originalPilot = booking.AssignedPilot;
            string errorMessage = null;

            //unassign the previous pilot
            if (booking.AssignedPilot != null)
            {
                var removedMessage = $"Removed assigned pilot {booking.AssignedPilot} ({booking.AssignedPilot.PhoneNumber})";
                if (!string.IsNullOrEmpty(newPilot.NewPilotMessage))
                {
                    removedMessage = removedMessage + $" due to {newPilot.NewPilotMessage}";
                }
                _bookingService.AddEvent(booking, User, removedMessage);
            }

            ApplicationUser assignedPilot;
            if (string.IsNullOrEmpty(newPilotId))
            {
                //try to find a new pilot (if none are available, set no pilot assigned)
                assignedPilot = _bookingService.AssignNewPilot(booking);
            }
            else
            {
                assignedPilot = _context.Users.Single(u => u.Id == newPilotId);
                _bookingService.AssignNewPilot(booking, assignedPilot);
            }
            _context.SaveChanges();

            //notify pilot and passenger of the newly assigned pilot
            var bookingDateString = booking.BookingDate.ToString("dd.MM.yyyy");
            if (assignedPilot != null)
            {
                var message =
                    $"You have a new flight on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}.";
                await _nexmo.SendSms("VossHPK", assignedPilot.PhoneNumber, message);

                var passengerMessage =
                    $"Your flight on {bookingDateString} has been assigned a new pilot. You will be contacted by {assignedPilot.Name} ({assignedPilot.PhoneNumber}) shortly.";
                await _nexmo.SendSms("VossHPK", booking.PassengerPhone, passengerMessage);

                if (string.IsNullOrEmpty(newPilotId))
                {
                    _bookingService.AddEvent(booking, User,
                        $"Assigned booking to {assignedPilot.Name} ({assignedPilot.PhoneNumber})");
                }
                else
                {
                    _bookingService.AddEvent(booking, User,
                        $"Admin forced booking to {assignedPilot.Name} ({assignedPilot.PhoneNumber})");
                }
                _bookingService.AddEvent(booking, User, $"Sent status update to passenger, {booking.PassengerName} ({booking.PassengerPhone})");
            }
            else if (originalPilot != null)
            {
                var message =
                    $"Please find a pilot on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}";
                await _nexmo.SendSms("VossHPK", _bookingCoordinatorSettings.PhoneNumber, message);

                var passengerMessage =
                    $"We're working on finding a pilot for your flight on {bookingDateString}. You will be contacted shortly. If you have any questions, you can contact the tandem booking coordinator, {_bookingCoordinatorSettings.Name} on ({_bookingCoordinatorSettings})";
                await _nexmo.SendSms("VossHPK", booking.PassengerPhone, passengerMessage);

                _bookingService.AddEvent(booking, User,
                    $"No pilots available, sent message to tandem coordinator {_bookingCoordinatorSettings.Name} ({_bookingCoordinatorSettings.PhoneNumber})");

                _bookingService.AddEvent(booking, User,
                    $"Sent status update to passenger, {booking.PassengerName} ({booking.PassengerPhone})");
            }
            else
            {
                errorMessage = "No pilots available. Please select pilot manually";
            }
            _context.SaveChanges();



            return RedirectToAction("Edit", new { Id = booking.Id, errorMessage });
        }

        [HttpPost]
        public async Task<ActionResult> AddMessage(Guid id, SendMessageViewModel input)
        {
            var booking = _context.Bookings
                .Include(b => b.BookingEvents)
                .FirstOrDefault(b => b.Id == id);

            if (string.IsNullOrWhiteSpace(input.EventMessage))
            {
                return RedirectToAction("Edit", new { Id = booking.Id });
            }

            var message = input.EventMessage;
            if (input.SendToPassenger)
            {
                message += " (sent to passenger)";
            }

            _bookingService.AddEvent(booking, User, message);
            _context.SaveChanges();

            if (input.SendToPassenger)
            {
                await _nexmo.SendSms("VossHPK", booking.PassengerPhone, input.EventMessage);
            }

            return RedirectToAction("Edit", new {Id = booking.Id});
        }
    }
}