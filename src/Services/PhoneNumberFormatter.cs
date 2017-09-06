namespace TandemBooking.Services
{
    public static class PhoneNumberFormatter
    {
        public static string AsPhoneNumber(this string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            return $"+{phoneNumber}";
        }
    }
}