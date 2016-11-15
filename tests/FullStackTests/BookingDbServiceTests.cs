using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TandemBooking.Controllers;
using TandemBooking.Models;
using TandemBooking.Services;
using TandemBooking.Tests.TestSetup;
using TandemBooking.ViewModels.Booking;
using Xunit;

namespace TandemBooking.Tests.FullStackTests
{
    [Collection("Integration Tests")]
    public class BookingDbServiceTests : IntegrationTestBase
    {
        public BookingDbServiceTests(IntegrationTestFixture fixture) : base(fixture)
        {
            _db = GetService<BookingServiceDb>();

            AddPilotsFixture();
        }

        private BookingServiceDb _db;

        private ApplicationUser _frode;
        private ApplicationUser _erik;
        private ApplicationUser _aasmund;
        private ApplicationUser _tore;

        private void AddPilotsFixture()
        {
            _frode = Context.Users.Add(new ApplicationUser()
            {
                Name = "Frode Fester",
                Email = "frode@mail.com",
                PhoneNumber = "4794279974",
                IsAdmin = false,
                IsPilot = true,
                Availabilities = new List<PilotAvailability>()
            }).Entity;

            _erik = Context.Users.Add(new ApplicationUser()
            {
                Name = "Erik Røthe Klette",
                Email = "heimabrygg.og.smalahove@gmail.com",
                PhoneNumber = "47999694616",
                IsAdmin = false,
                IsPilot = true,
                Availabilities = new List<PilotAvailability>()
            }).Entity;

            _aasmund = Context.Users.Add(new ApplicationUser()
            {
                Name = "Åsmund Birkeland",
                Email = "aasmund.birkeland@gmail.com",
                PhoneNumber = "4792819387",
                IsAdmin = false,
                IsPilot = true,
                Availabilities = new List<PilotAvailability>()
            }).Entity;

            _tore = Context.Users.Add(new ApplicationUser()
            {
                Name = "Tore Birkeland",
                Email = "tore.birkeland@gmail.com",
                PhoneNumber = "4798463072",
                IsAdmin = true,
                IsPilot = false,
                Availabilities = new List<PilotAvailability>()
            }).Entity;

            Context.SaveChanges();
        }

        private void AddAvailabilityFixture(DateTime date, params ApplicationUser[] pilots)
        {
            foreach (var pilot in pilots)
            {
                pilot.Availabilities.Add(new PilotAvailability()
                {
                    Date = date
                });
            }
            Context.SaveChanges();
        }

        private Booking AddBookingFixture(DateTime Date, ApplicationUser pilot)
        {
            var booking = Context.Bookings.Add(new Booking()
            {
                AssignedPilot = pilot,
                DateRegistered = new DateTime(2016, 1, 1),
                BookingDate = Date,
                Canceled = false,
                BookedPilots = new List<BookedPilot>() { new BookedPilot() { Pilot = pilot } }
            }).Entity;

            Context.SaveChanges();

            return booking;
        }

        [Fact]
        public async Task Availability()
        {
            AddAvailabilityFixture(new DateTime(2016, 11, 1), _frode, _erik);

            var avail = await _db.GetAvailablePilotsAsync(new DateTime(2016, 11, 1));

            Assert.Equal(3, avail.Count); // only pilots
            Assert.Equal(2, avail.Count(x => x.Available)); // frode and erik are available
            Assert.Equal("Åsmund Birkeland", avail.Single(x => !x.Available).Pilot.Name); // åsmund is not available
            Assert.All(avail, x => Assert.Equal(0, x.Priority));
        }

        [Fact]
        public async Task PriorityWithBookings()
        {
            //future bookings
            AddBookingFixture(new DateTime(2016, 11, 2), _frode);
            AddBookingFixture(new DateTime(2016, 11, 3), _frode);
            AddBookingFixture(new DateTime(2016, 11, 4), _frode);
            AddBookingFixture(new DateTime(2016, 11, 1).AddDays(20), _frode); //should not count

            //past bookings
            AddBookingFixture(new DateTime(2016, 10, 29), _erik);
            AddBookingFixture(new DateTime(2016, 11, 1).AddDays(-20), _erik);
            AddBookingFixture(new DateTime(2016, 11, 1).AddDays(-40), _erik); //should not count

            //today
            AddBookingFixture(new DateTime(2016, 11, 1), _aasmund);
            AddBookingFixture(new DateTime(2016, 11, 2), _aasmund);
            AddBookingFixture(new DateTime(2016, 10, 29), _aasmund);

            //Canceled booking
            var b1 = AddBookingFixture(new DateTime(2016, 10, 28), _aasmund);
            b1.Canceled = true;

            //Reassigned booking (should count on frode)
            var b2 = AddBookingFixture(new DateTime(2016, 11, 5), _aasmund);
            b2.AssignedPilot = _frode;
            b2.BookedPilots.Single().Canceled = true;
            b2.BookedPilots.Add(new BookedPilot() { Pilot = _frode});

            Context.SaveChanges();

            //act
            var avail = await _db.GetAvailablePilotsAsync(new DateTime(2016, 11, 1));

            //assert
            Assert.All(avail, x => Assert.Equal(false, x.Available));

            Assert.Equal(4, avail.Single(x => x.Pilot == _frode).Priority);
            Assert.Equal(2, avail.Single(x => x.Pilot == _erik).Priority);
            Assert.Equal(1002, avail.Single(x => x.Pilot == _aasmund).Priority);
        }

    }
}