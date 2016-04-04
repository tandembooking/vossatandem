using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;

namespace TandemBooking.Models
{
    public class TandemBookingContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Booking> Bookings { get; set; } 
        public DbSet<PilotAvailability> PilotAvailabilities { get; set; } 
        public DbSet<BookedPilot> BookedPilots { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Booking>();
            builder.Entity<PilotAvailability>();
            builder.Entity<BookingEvent>();
            builder.Entity<BookedPilot>();
        }
    }
}
