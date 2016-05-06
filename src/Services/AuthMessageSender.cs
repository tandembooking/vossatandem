using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TandemBooking.Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private readonly NexmoService _nexmo;

        public AuthMessageSender(NexmoService nexmo)
        {
            _nexmo = nexmo;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Task.FromResult(0);
        }

        public async Task SendSmsAsync(string number, string message)
        {
            number = await _nexmo.FormatPhoneNumber(number);
            await _nexmo.SendSms("VossHPK", number, message);
        }
    }
}
