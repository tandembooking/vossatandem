using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TandemBooking.Services;

namespace TandemBooking.Models
{
    public class TandemBookingContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Booking> Bookings { get; set; } 
        public DbSet<PilotAvailability> PilotAvailabilities { get; set; } 
        public DbSet<BookedPilot> BookedPilots { get; set; }
        public DbSet<SentSmsMessage> SentSmsMessages { get; set; }
        public DbSet<SentSmsMessagePart> SentSmsMessageParts { get; set; }



        public TandemBookingContext(DbContextOptions<TandemBookingContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var userBuilder = builder.Entity<ApplicationUser>();
            userBuilder.Property(p => p.EmailNotification).HasDefaultValue(true);
            userBuilder.Property(p => p.SmsNotification).HasDefaultValue(true);

            var bookingBuilder = builder.Entity<Booking>();
            bookingBuilder.HasOne(t => t.PrimaryBooking)
                .WithMany(u => u.AdditionalBookings);

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
