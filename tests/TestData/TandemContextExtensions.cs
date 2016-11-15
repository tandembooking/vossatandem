using System;
using System.Collections.Generic;
using TandemBooking.Models;

namespace TandemBooking.Tests.TestData
{
    public static class TandemContextExtensions
    {
        public static void AddAvailabilityFixture(this TandemBookingContext context, DateTime date,
            params ApplicationUser[] pilots)
        {
            foreach (var pilot in pilots)
                pilot.Availabilities.Add(new PilotAvailability
                {
                    Date = date
                });
            context.SaveChanges();
        }

        public static Booking AddBookingFixture(this TandemBookingContext context, DateTime date, ApplicationUser pilot)
        {
            var booking = context.Bookings.Add(new Booking
            {
                AssignedPilot = pilot,
                DateRegistered = new DateTime(2016, 1, 1),
                BookingDate = date,
                Canceled = false,
                BookedPilots = pilot != null 
                    ? new List<BookedPilot> {new BookedPilot {Pilot = pilot}} 
                    : new List<BookedPilot>()
            }).Entity;

            context.SaveChanges();

            return booking;
        }
    }
}