using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Packaging;

namespace TandemBooking.Services
{
    public static class PhoneNumberFormatter
    {
        public static string AsPhoneNumber(this string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return phoneNumber;
            }

            return $"+{phoneNumber}";
        }
    }
}
