using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TandemBooking.Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private readonly NexmoService _nexmo;
        private readonly MailService _mailService;

        public AuthMessageSender(NexmoService nexmo, MailService mailService)
        {
            _nexmo = nexmo;
            _mailService = mailService;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            await _mailService.Send(email, subject, message);
        }

        public async Task SendSmsAsync(string number, string message)
        {
            number = await _nexmo.FormatPhoneNumber(number);
            await _nexmo.SendSms("VossHPK", number, message);
        }
    }
}
