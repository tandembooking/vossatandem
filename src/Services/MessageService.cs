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
        private readonly MailService _mailService;
        private readonly TandemBookingContext _context;
        private readonly BookingService _bookingService;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;

        public MessageService(SmsService smsService, TandemBookingContext context, BookingService bookingService, BookingCoordinatorSettings bookingCoordinatorSettings, MailService mailService)
        {
            _smsService = smsService;
            _context = context;
            _bookingService = bookingService;
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
            _mailService = mailService;
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
                //send message to pilot
                if (assignedPilot.SmsNotification) { 
                    var message = $"You have a new flight on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}.";
                    await SendPilotMessage(assignedPilot, "New Booking", message, booking);
                }
                if (assignedPilot.EmailNotification)
                {
                    var subject = $"New flight on {bookingDateString}";
                    var message = $@"Hi {assignedPilot.Name},

You have been assigned a new flight:
Date:            {bookingDateString}. 
Passenger Name:  {booking.PassengerName},
Passenger Phone: {booking.PassengerPhone},
Passenger Email: {booking.PassengerEmail ?? "not specified"}
Comments:
{booking.Comment}

Booking Url: http://vossatandem.no/BookingAdmin/Edit/{booking.Id}

fly safe!
Booking Coordinator
";
                    await _mailService.Send(assignedPilot.Email, subject, message);
                }

                //send message to passenger
                var passengerMessage = $"Awesome! Your tandem flight on  is confirmed. You will be contacted by {assignedPilot.Name} ({assignedPilot.PhoneNumber}) shortly.";
                await _smsService.Send(booking.PassengerPhone, passengerMessage, booking);

                //send message to booking coordinator
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

        public async Task SendNewPilotMessage(string bookingDateString, Booking booking, ApplicationUser previousPilot)
        {
            //send message to new pilot
            var assignedPilot = booking.AssignedPilot;
            if (assignedPilot.SmsNotification)
            {
                var message = $"You have a new flight on {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone}, {booking.Comment}.";
                await SendPilotMessage(assignedPilot, "Assigned Booking", message, booking);
            }
            if (assignedPilot.EmailNotification)
            {
                var subject = $"New flight on {bookingDateString}";
                var message = $@"Hi {assignedPilot.Name},

You have been assigned a flight:
Date:            {bookingDateString}. 
Passenger Name:  {booking.PassengerName},
Passenger Phone: +{booking.PassengerPhone},
Passenger Email: {booking.PassengerEmail ?? "not specified"}
Comments:
{booking.Comment}

Booking Url: http://vossatandem.no/BookingAdmin/Edit/{booking.Id}

fly safe!
Booking Coordinator
";
                await _mailService.Send(assignedPilot.Email, subject, message);
            }

            //send message to passenger
            var passengerMessage = $"Your flight on {bookingDateString} has been assigned a new pilot. You will be contacted by {assignedPilot.Name} ({assignedPilot.PhoneNumber}) shortly.";
            await _smsService.Send(booking.PassengerPhone, passengerMessage, booking);
        }

        public async Task SendPilotUnassignedMessage(Booking booking, ApplicationUser previousPilot)
        {
            var bookingDateString = booking.BookingDate.ToString("dd.MM.yyyy");

            if (previousPilot.SmsNotification)
            {
                var message = $"Your booking on {bookingDateString} has been reassigned to another pilot";
                await SendPilotMessage(previousPilot, "Booking reassigned", message, booking);
            }
            if (previousPilot.EmailNotification)
            {
                var message =
                    $@"Hi {previousPilot.Name},

Your flight on {bookingDateString} has been assigned another pilot.

Booking Url: http://vossatandem.no/BookingAdmin/Edit/{booking
                        .Id}

fly safe!
Booking Coordinator
";
                await _mailService.Send(previousPilot.Email, $"Booking on {bookingDateString} reassigned", message);
            }
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
