using System.Collections.Generic;
using Microsoft.AspNet.Identity.EntityFramework;

namespace TandemBooking.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public bool IsPilot { get; set; }
        public bool IsAdmin { get; set; }
        public bool EmailNotification { get; set; }
        public bool SmsNotification { get; set; }

        public ICollection<BookedPilot> Bookings { get; set; }
        public ICollection<PilotAvailability> Availabilities { get; set; }
    }
}