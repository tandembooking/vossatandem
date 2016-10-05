using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TandemBooking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1;
using TandemBooking.Services;
using TandemBooking.ViewModels.BookingAdmin;

namespace TandemBooking.Controllers
{
    [Authorize]
    public class BookingAdminController : Controller
    {
        private readonly ILogger<BookingAdminController> _logger;
        private readonly TandemBookingContext _context;
        private readonly BookingService _bookingService;
        private readonly MessageService _messageService;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;
        private readonly UserManager _userManager;
        private readonly NexmoService _nexmo;

        public BookingAdminController(TandemBookingContext context, NexmoService nexmo, BookingService bookingService,
            MessageService messageService, BookingCoordinatorSettings bookingCoordinatorSettings,
            UserManager userManager, ILogger<BookingAdminController> logger)
        {
            _context = context;
            _bookingService = bookingService;
            _messageService = messageService;
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
            _userManager = userManager;
            _logger = logger;
            _nexmo = nexmo;
        }


        public ActionResult Index(string userId = null)
        {
            if (!User.IsAdmin() && !User.IsPilot())
            {
                return new UnauthorizedResult();
            }

            var bookingQuery = _context.Bookings
                .Include(b => b.AssignedPilot)
                .Where(u => u.BookingDate >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(u => u.BookingDate)
                .AsQueryable();

            //non-admin users can only see their own bookings
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

            if (!User.IsAdmin() && !User.IsPilot())
            {
                return new UnauthorizedResult();
            }

            var vm = new BookingDetailsViewModel()
            {
                ErrorMessage = errorMessage,
                Booking = booking,
                Editable = User.IsAdmin() || booking.AssignedPilot?.Id == _userManager.GetUserId(User),
            };

            vm.AvailablePilots = _bookingService.FindAvailablePilots(booking.BookingDate, true);

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
                .Include(b => b.AssignedPilot)
                .Include(b => b.BookingEvents)
                .FirstOrDefault(b => b.Id == id);

            var userId = _userManager.GetUserId(User);
            if (!User.IsAdmin() && booking.AssignedPilot?.Id != userId)
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(cancelMessage))
            {
                return RedirectToAction("Edit", new { Id = booking.Id });
            }

            booking.Canceled = true;
            _bookingService.AddEvent(booking, User, $"Canceled due to {cancelMessage}");

            await _messageService.SendCancelMessage(cancelMessage, booking);

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

            var userId = _userManager.GetUserId(User);
            if (!User.IsAdmin() && booking.AssignedPilot.Id != userId)
            {
                return RedirectToAction("Index");
            }

            //get optional pilot id from input
            var newPilotId = newPilot.NewPilotUserId;

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
                await _messageService.SendNewPilotMessage(bookingDateString, booking, originalPilot, newPilot.NewPilotNotifyPassenger);
                if (originalPilot != null)
                {
                    await _messageService.SendPilotUnassignedMessage(booking, originalPilot);
                }

                if (string.IsNullOrEmpty(newPilotId))
                {
                    _bookingService.AddEvent(booking, User, $"Assigned booking to {assignedPilot.Name} ({assignedPilot.PhoneNumber})");
                }
                else
                {
                    _bookingService.AddEvent(booking, User, $"Forced booking to {assignedPilot.Name} ({assignedPilot.PhoneNumber})");
                }

                if (newPilot.NewPilotNotifyPassenger)
                {
                    _bookingService.AddEvent(booking, User, $"Sent status update to passenger, {booking.PassengerName} ({booking.PassengerPhone})");
                }
            }
            else if (originalPilot != null)
            {
                await _messageService.SendMissingPilotMessage(bookingDateString, booking);
                await _messageService.SendPilotUnassignedMessage(booking, originalPilot);
                _bookingService.AddEvent(booking, User, $"No pilots available, sent message to tandem coordinator {_bookingCoordinatorSettings.Name} ({_bookingCoordinatorSettings.PhoneNumber})");
                _bookingService.AddEvent(booking, User, $"Sent status update to passenger, {booking.PassengerName} ({booking.PassengerPhone})");
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
                .Include(b => b.AssignedPilot)
                .Include(b => b.BookingEvents)
                .FirstOrDefault(b => b.Id == id);

            if (string.IsNullOrWhiteSpace(input.EventMessage))
            {
                return RedirectToAction("Edit", new { Id = booking.Id });
            }

            var userId = _userManager.GetUserId(User);
            if (!User.IsAdmin() && booking.AssignedPilot.Id != userId)
            {
                return RedirectToAction("Index");
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
                await _messageService.SendPassengerMessage(input, booking);
            }

            return RedirectToAction("Edit", new {Id = booking.Id});
        }
    }
}