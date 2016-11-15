using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TandemBooking.Controllers;
using TandemBooking.Services;
using TandemBooking.Tests.TestSetup;
using TandemBooking.ViewModels.Booking;
using Xunit;

namespace TandemBooking.Tests.ControllerTests
{
    [Collection("Integration Tests")]
    public class BookingTests : IntegrationTestBase
    {
        public BookingTests(IntegrationTestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task CreateSuccessfulSimpleBookingWithNoAvailablePilot()
        {
            var input = new BookingViewModel
            {
                Date = new DateTime(2016, 11, 13),
                Name = "My Name",
                PhoneNumber = "11111111",
                Email = "passenger@example.com",
                Comment = "Blah"
            };

            var ctrl = GetService<BookingController>();
            var result = await ctrl.Index(input);

            //Assert booking is created
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, Context.Bookings.Count());

            var booking = Context.Bookings
                .Include(b => b.AssignedPilot)
                .Include(b => b.AdditionalBookings)
                .Include(b => b.BookedPilots)
                .Include(b => b.BookingEvents)
                .First();

            Assert.Equal(null, booking.AssignedPilot); // no pilots available
            Assert.Equal("passenger@example.com", booking.PassengerEmail);
            Assert.Equal("4711111111", booking.PassengerPhone);
            Assert.Equal("Blah", booking.Comment);

            //Assert sms is sent
            var nexmoService = (MockNexmoService) GetService<INexmoService>();

            //we should have sent two text messages: one to passenger, one to booking coordinator
            Assert.Equal(2, nexmoService.Messages.Count);

            var passengerMessage = nexmoService.Messages.Single(m => m.Recipient == "4711111111");
            Assert.Contains("We will try to find a pilot", passengerMessage.Body);

            var coordinatorMessage = nexmoService.Messages.Single(m => m.Recipient == "4798463072");
            Assert.Contains("Please find a pilot", coordinatorMessage.Body);
        }
    }
}