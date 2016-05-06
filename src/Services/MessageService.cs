using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TandemBooking.Models;
using TandemBooking.ViewModels.BookingAdmin;

namespace TandemBooking.Services
{
    public class MessageService
    {
        private readonly SmsService _smsService;
        private readonly TandemBookingContext _context;
        private readonly BookingService _bookingService;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;

        public MessageService(SmsService smsService, TandemBookingContext context, BookingService bookingService, BookingCoordinatorSettings bookingCoordinatorSettings)
        {
            _smsService = smsService;
            _context = context;
            _bookingService = bookingService;
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
        }

        public async Task SendPilotMessage(ApplicationUser user, string subject, string message, Booking booking)
        {
            await _smsService.Send(user.PhoneNumber, message, booking);
        }

        public async Task SendNewBookingMessage(Booking booking)
        {
            var assignedPilot = booking.AssignedPilot;

            //send message to pilot or booking coordinator
            var bookingDateString = booking.BookingDate.ToString("dd.MM.yyyy");
            if (assignedPilot != null)
            {
                var message = $"You have a new flight on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}.";
                await SendPilotMessage(assignedPilot, "New Booking", message, booking);

                var passengerMessage = $"Awesome! Your tandem flight on  is confirmed. You will be contacted by {assignedPilot.Name} ({assignedPilot.PhoneNumber}) shortly.";
                await _smsService.Send(booking.PassengerPhone, passengerMessage, booking);

                var bookingCoordinatorMessage = $"New flight on {bookingDateString} assigned to {assignedPilot.Name}, {booking.Comment}";
                await _smsService.Send(_bookingCoordinatorSettings.PhoneNumber, bookingCoordinatorMessage, booking);

                _bookingService.AddEvent(booking, null, $"Assigned to {assignedPilot.Name} ({assignedPilot.PhoneNumber})");
                _bookingService.AddEvent(booking, null, $"Sent confirmation message to {booking.PassengerName} ({booking.PassengerPhone})");
            }
            else
            {
                var message = $"Please find a pilot on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}";
                await _smsService.Send(_bookingCoordinatorSettings.PhoneNumber, message, booking);

                var passengerMessage = $"Awesome! We will try to find a pilot who can take you flying on {bookingDateString}. You will be contacted shortly.";
                await _smsService.Send(booking.PassengerPhone, passengerMessage, booking);

                _bookingService.AddEvent(booking, null, $"No pilots available, sent message to tandem coordinator {_bookingCoordinatorSettings.Name} ({_bookingCoordinatorSettings.PhoneNumber})");
                _bookingService.AddEvent(booking, null, $"Sent confirmation message to {booking.PassengerName} ({booking.PassengerPhone})");
            }
            _context.SaveChanges();
        }

        public async Task SendMissingPilotMessage(string bookingDateString, Booking booking)
        {
            var message = $"Please find a pilot on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}";
            await _smsService.Send(_bookingCoordinatorSettings.PhoneNumber, message, booking);

            var passengerMessage = $"We're working on finding a pilot for your flight on {bookingDateString}. You will be contacted shortly. If you have any questions, you can contact the tandem booking coordinator, {_bookingCoordinatorSettings.Name} on ({_bookingCoordinatorSettings.PhoneNumber})";
            await _smsService.Send(booking.PassengerPhone, passengerMessage, booking);
        }

        public async Task SendNewPilotMessage(string bookingDateString, Booking booking)
        {
            var assignedPilot = booking.AssignedPilot;

            var message = $"You have a new flight on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}.";
            await SendPilotMessage(assignedPilot, "Assigned Booking", message, booking);

            var passengerMessage = $"Your flight on {bookingDateString} has been assigned a new pilot. You will be contacted by {assignedPilot.Name} ({assignedPilot.PhoneNumber}) shortly.";
            await _smsService.Send(booking.PassengerPhone, passengerMessage, booking);
        }

        public async Task SendCancelMessage(string cancelMessage, Booking booking)
        {
            var message = $"Unfortunately, your flight on {booking.BookingDate.ToString("dd.MM.yyyy")} has been canceled due to {cancelMessage}";
            await _smsService.Send(booking.PassengerPhone, message, booking);
        }

        public async Task SendPassengerMessage(SendMessageViewModel input, Booking booking)
        {
            await _smsService.Send(booking.PassengerPhone, input.EventMessage, booking);
        }

    }
}
