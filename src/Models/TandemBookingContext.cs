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
        public DbSet<SentSmsMessage> SentSmsMessages { get; set; }
        public DbSet<SentSmsMessagePart> SentSmsMessageParts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var userBuilder = builder.Entity<ApplicationUser>();
            userBuilder.Property(p => p.EmailNotification).HasDefaultValue(true);
            userBuilder.Property(p => p.SmsNotification).HasDefaultValue(true);

            builder.Entity<Booking>();
            builder.Entity<PilotAvailability>();
            builder.Entity<BookingEvent>();
            builder.Entity<BookedPilot>();
            builder.Entity<SentSmsMessage>();
            var sentSmsMessagePartBuilder = builder.Entity<SentSmsMessagePart>();
            sentSmsMessagePartBuilder.Property(t => t.GatewayMessageId)
                .HasMaxLength(255);
            sentSmsMessagePartBuilder.HasIndex(t => t.GatewayMessageId);
        }
    }
}
