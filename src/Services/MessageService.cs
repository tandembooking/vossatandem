using System.Linq;
using System.Threading.Tasks;
using TandemBooking.Models;
using TandemBooking.ViewModels.BookingAdmin;

namespace TandemBooking.Services
{
    public class MessageService
    {
        private readonly SmsService _smsService;
        private readonly IMailService _mailService;
        private readonly TandemBookingContext _context;
        private readonly BookingService _bookingService;
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;

        public MessageService(SmsService smsService, TandemBookingContext context, BookingService bookingService, BookingCoordinatorSettings bookingCoordinatorSettings, IMailService mailService)
        {
            _smsService = smsService;
            _context = context;
            _bookingService = bookingService;
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
            _mailService = mailService;
        }

        private async Task SendPilotMessage(ApplicationUser user, string message, Booking booking)
        {
            await _smsService.Send(user.PhoneNumber, message, booking);
        }

        public async Task SendNewBookingMessage(Booking primaryBooking, Booking[] additionalBookings, bool notifyPassenger, bool notifyPilot)
        {
            var allBookings = new[] {primaryBooking}.Union(additionalBookings).ToArray();
            var bookingDateString = primaryBooking.BookingDate.ToString("dd.MM.yyyy");

            //send message to pilot or booking coordinator
            foreach (var booking in allBookings)
            {
                var assignedPilot = booking.AssignedPilot;

                if (assignedPilot != null)
                {
                    _bookingService.AddEvent(booking, null,
                        $"Tildelt pilot {assignedPilot.Name} ({assignedPilot.PhoneNumber.AsPhoneNumber()})");

                    //send message to pilot
                    if (notifyPilot)
                    {
                        //sms to pilot
                        if (assignedPilot.SmsNotification)
                        {
                            var message =
                                $"Du har en ny tandemtur {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone.AsPhoneNumber()}, {booking.Comment}.";
                            await SendPilotMessage(assignedPilot, message, booking);
                        }
                        //mail to pilot
                        if (assignedPilot.EmailNotification)
                        {
                            var subject = $"Ny tandemtur  {bookingDateString}";
                            var message = $@"Hei {assignedPilot.Name},

Du har blitt tildelt en ny tandemtur:
Dato:                 {bookingDateString}. 
Passasjerens navn:    {booking.PassengerName},
Passasjerens telefon: {booking.PassengerPhone.AsPhoneNumber()},
Passasjerens epost:   {booking.PassengerEmail ?? "ikke oppgitt"}
Kommentar:
{booking.Comment}

Booking-lenke: http://tandembooking.bhpgk.club/BookingAdmin/Details/{booking.Id}

fly pent!
Bookingkoordinator
";
                            await _mailService.Send(assignedPilot.Email, subject, message);
                        }
                    }

                    //send booking assigned message to booking coordinator
                    var bookingCoordinatorMessage =
                        $"Ny tandemtur {bookingDateString} tildelt {assignedPilot.Name}, {booking.Comment}";
                    await _smsService.Send(_bookingCoordinatorSettings.PhoneNumber, bookingCoordinatorMessage, booking);
                }
                else
                {
                    _bookingService.AddEvent(booking, null,
                        $"Ingen tilgjengelige piloter, melding er sendt til tandemkoordinator {_bookingCoordinatorSettings.Name} ({_bookingCoordinatorSettings.PhoneNumber.AsPhoneNumber()})");

                    //send no pilots available to booking coordinator
                    var message =
                        $"Vennligst finn en tandempilot til {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone.AsPhoneNumber()}, {booking.Comment}";
                    await _smsService.Send(_bookingCoordinatorSettings.PhoneNumber, message, booking);
                }
            }

            //send message to passenger
            if (notifyPassenger)
            {
                string passengerMessage;
                if (allBookings.All(b => b.AssignedPilot != null))
                {
                    var assignedPilot = primaryBooking.AssignedPilot;
                    passengerMessage = additionalBookings.Any() 
                        ? $"Fantastisk! Dine {allBookings.Length} tandemturer {bookingDateString} er bekreftet. Du vil bli kontaktet av {assignedPilot.Name} ({assignedPilot.PhoneNumber.AsPhoneNumber()}) for å avtale detaljene rundt flyturene." 
                        : $"Fantastisk! Din tandemtur {bookingDateString} er bekreftet. Du vil bli kontaktet av {assignedPilot.Name} ({assignedPilot.PhoneNumber.AsPhoneNumber()}) for å avtale detaljene rundt flyturen.";
                }
                else
                {
                    passengerMessage = additionalBookings.Any()
                        ? $"Fantastisk! Vi prøver å finne {allBookings.Length} piloter som kan ta dere med i lufta {bookingDateString}. Dere vil snart bli kontaktet."
                        : $"Fantastisk Vi prøver å finne en pilot som kan ta deg med i lufta {bookingDateString}. Du vil snart bli kontaktet.";
                }

                if (!string.IsNullOrEmpty(primaryBooking.PassengerPhone))
                {
                    await _smsService.Send(primaryBooking.PassengerPhone, passengerMessage, primaryBooking);
                    _bookingService.AddEvent(primaryBooking, null,
                        $"Bekreftelse er sendt pr. sms til {primaryBooking.PassengerName} ({primaryBooking.PassengerPhone.AsPhoneNumber()})");
                }

                if (!string.IsNullOrEmpty(primaryBooking.PassengerEmail))
                {
                    var assignedPilotMessage = "";
                    if (primaryBooking.AssignedPilot != null)
                    {
                        var pilotName = primaryBooking.AssignedPilot.Name;
                        var pilotPhone = primaryBooking.AssignedPilot.PhoneNumber.AsPhoneNumber();
                        var pilotEmail = primaryBooking.AssignedPilot.Email;
                        assignedPilotMessage =
                            $@"
Din tildelte instruktør er {pilotName} ({pilotPhone}, {pilotEmail}), føl deg fri 
til å kontakte ham angående spørsmål du har angående flyturen.
";
                    }

                    var message = $@"Hei {primaryBooking.PassengerName},

Takk for at du booket en tandemtur med Bodø Hang & Paragliderklubb {bookingDateString}. Din booking
er bekreftet. En av våre instruktører vil kontakte deg for å organisere detaljene rundt 
når og hvor dere møtes, et par dager før flyturen.
{assignedPilotMessage}

Med vennlig hilsen,
{_bookingCoordinatorSettings.Name}
Bookingkoordinator
Bodø Hang & Paragliderklubb";
                    await _mailService.Send(primaryBooking.PassengerEmail, $"Tandemtur {bookingDateString}", message);

                    _bookingService.AddEvent(primaryBooking, null,
                        $"Bekreftelse er sendt på epost til {primaryBooking.PassengerName} ({primaryBooking.PassengerEmail})");
                }
            }

            _context.SaveChanges();
        }

        public async Task SendMissingPilotMessage(string bookingDateString, Booking booking)
        {
            //send to booking coordinator
            var message = $"Vennligst finn en pilot til {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone.AsPhoneNumber()}, {booking.Comment}";
            await _smsService.Send(_bookingCoordinatorSettings.PhoneNumber, message, booking);

            //passenger sms 
            if (!string.IsNullOrEmpty(booking.PassengerPhone))
            {
                var passengerMessage = $"Vi jobber med å finne en instruktør for din flytur {bookingDateString}. Du vil bli kontaktet med navnet og nummeret til den nye instruktøren. Om du har noen spørsmål, kan du kontakte tandembookingkoordinator {_bookingCoordinatorSettings.Name} på ({_bookingCoordinatorSettings.PhoneNumber.AsPhoneNumber()})";
                await _smsService.Send(booking.PassengerPhone, passengerMessage, booking);
            }

            //passenger email
            if (!string.IsNullOrEmpty(booking.PassengerEmail))
            {
                var mailMessage = $@"Hei {booking.PassengerName},

Din tandembooking {bookingDateString} har blitt oppdatert.

Vi jobber med å finne en ny instruktør for din flytur {bookingDateString}. 
Du vil bli kontaktet med navnet og nummeret til den nye instruktøren.
Om du har noen spørsmål, kan du kontakte tandembookingkoordinator,
{_bookingCoordinatorSettings.Name} på ({_bookingCoordinatorSettings.PhoneNumber.AsPhoneNumber()} eller {_bookingCoordinatorSettings.Email})

Med vennlig hilsen,
{_bookingCoordinatorSettings.Name}
Bookingkoordinator
Bodø Hang & Paragliderklubb
";
                await _mailService.Send(booking.PassengerEmail, $"Tandemtur {bookingDateString}", mailMessage);
            }


        }

        public async Task SendNewPilotMessage(string bookingDateString, Booking booking, ApplicationUser previousPilot, bool notifyPassenger)
        {
            //send message to new pilot
            var assignedPilot = booking.AssignedPilot;
            if (assignedPilot.SmsNotification)
            {
                var message = $"Du har en ny tandemtur {bookingDateString}: {booking.PassengerName}, {booking.PassengerEmail}, {booking.PassengerPhone.AsPhoneNumber()}, {booking.Comment}.";
                await SendPilotMessage(assignedPilot, message, booking);
            }
            if (assignedPilot.EmailNotification)
            {
                var subject = $"Ny tandemtur {bookingDateString}";
                var message = $@"Hei {assignedPilot.Name},

Du har blitt tildelt en ny tandemtur:
Dato:                 {bookingDateString}. 
Passasjerens navn:    {booking.PassengerName},
Passasjerens telefon: {booking.PassengerPhone.AsPhoneNumber()},
Passasjerens epost: {booking.PassengerEmail ?? "ikke oppgitt"}
Kommentar:
{booking.Comment}

Booking-lenke: http://tandembooking.bhpgk.club/BookingAdmin/Details/{booking.Id}

fly pent!
Bookingkoordinator
";
                await _mailService.Send(assignedPilot.Email, subject, message);
            }

            //send message to passenger
            if (notifyPassenger)
            {
                //sms
                if (!string.IsNullOrEmpty(booking.PassengerPhone))
                {
                    var passengerMessage = $"Tandemturen din {bookingDateString} er tildelt en ny pilot. Du vil bli kontaktet av {assignedPilot.Name} ({assignedPilot.PhoneNumber.AsPhoneNumber()}) snart.";
                    await _smsService.Send(booking.PassengerPhone, passengerMessage, booking);
                }

                //email
                if (!string.IsNullOrEmpty(booking.PassengerEmail))
                {
                    var mailMessage = $@"Hei {booking.PassengerName},

Din booking {bookingDateString} er oppdatert. Du har blitt tildelt en ny pilot.

Din nye instruktør for {bookingDateString} er {assignedPilot.Name} ({assignedPilot.PhoneNumber.AsPhoneNumber()}, {assignedPilot.Email}). 

Med vennlig hilsen,
{_bookingCoordinatorSettings.Name}
Bookingkoordinator
Bodø Hang & Paragliderklubb
";
                    await _mailService.Send(booking.PassengerEmail, $"Tandemtur {bookingDateString}", mailMessage);
                }
            }
        }

        public async Task SendPilotUnassignedMessage(Booking booking, ApplicationUser previousPilot)
        {
            var bookingDateString = booking.BookingDate.ToString("dd.MM.yyyy");

            if (previousPilot.SmsNotification)
            {
                var message = $"Din booking {bookingDateString} har blitt tildelt en annen pilot";
                await SendPilotMessage(previousPilot, message, booking);
            }
            if (previousPilot.EmailNotification)
            {
                var message =
                    $@"Hei {previousPilot.Name},

Tandemturen din {bookingDateString} har blitt tildelt en annen pilot.

Booking-lenke: http://tandembooking.bhpgk.club/BookingAdmin/Details/{booking
                        .Id}

fly safe!
Booking Coordinator
";
                await _mailService.Send(previousPilot.Email, $"Tandemtur {bookingDateString} omtildelt", message);
            }
        }

        public async Task SendCancelMessage(string cancelMessage, Booking booking, ApplicationUser sender)
        {
            var bookingDate = $"{booking.BookingDate:dd.MM.yyyy}";
            if (!string.IsNullOrEmpty(booking.PassengerPhone))
            {
                var message = $"Desverre er tandemturen dinn {bookingDate} kansellert grunnet {cancelMessage}";
                await _smsService.Send(booking.PassengerPhone, message, booking);
            }

            if (!string.IsNullOrEmpty(booking.PassengerEmail))
            {
                var senderText = $"{sender.Name} ({sender.PhoneNumber.AsPhoneNumber()}, {sender.Email})";
                var mailMessage = $@"Hei {booking.PassengerName},

Din booking {bookingDate} er oppdatert. {senderText} har sendt deg en ny melding:

Desverre er tandemturen din {bookingDate} kansellert grunnet {cancelMessage}

Med vennlig hilsen,
{_bookingCoordinatorSettings.Name}
Bookingkoordinator
Bodø Hang & Paragliderklubb
";
                await _mailService.Send(booking.PassengerEmail, $"Tandemtur {bookingDate}", mailMessage);
            }
        }

        public async Task SendBookingInformationMessage(SendMessageViewModel input, Booking booking, ApplicationUser sender)
        {
            if (input.SendToPassenger)
            {
                if (!string.IsNullOrEmpty(booking.PassengerPhone))
                {
                    await _smsService.Send(booking.PassengerPhone, input.EventMessage, booking);
                }

                if (!string.IsNullOrEmpty(booking.PassengerEmail))
                {
                    var senderText = $"{sender.Name} ({sender.PhoneNumber.AsPhoneNumber()}, {sender.Email})";
                    var bookingDate = $"{booking.BookingDate:dd.MM.yyyy}";
                    var mailMessage = $@"Hei {booking.PassengerName},

Bookingen din {bookingDate} er oppdatert. {senderText} har sendt deg en ny melding:

{input.EventMessage}

Med vennlig hilsen,
{_bookingCoordinatorSettings.Name}
Bookingkoordinator
Bodø Hang & Paragliderklubb
";
                    await _mailService.Send(booking.PassengerEmail, $"Tandemtur {bookingDate}", mailMessage);
                }
            }
        }

    }
}
