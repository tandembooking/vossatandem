using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TandemBooking.Models;

namespace TandemBooking.Services
{
    public class BookingServiceDb
    {
        private readonly TandemBookingContext _context;
        private readonly UserManager _userManager;

        public BookingServiceDb(TandemBookingContext context, UserManager userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<AvailablePilot>> GetAvailablePilotsAsync(DateTime date)
        {
            var availablePilots = new List<AvailablePilot>();
            var conn = (SqlConnection) _context.Database.GetDbConnection();
            {
                var sql = @"
                    DECLARE @Date date = CONVERT(date, @DateParam)

                    SELECT Pilot.Id PilotId, COALESCE(AvailabilityCount, 0) AvailabilityCount, COALESCE(Bookings.BookingCount, 0) BookingCount, COALESCE(BookingsToday.BookingCount, 0) BookingCountToday
	                    FROM AspNetUsers Pilot
	                    LEFT OUTER JOIN (
		                    SELECT PilotId, COUNT(Id) AvailabilityCount FROM PilotAvailability
		                    WHERE
			                    CONVERT(date, PilotAvailability.[Date]) = @Date
		                    GROUP BY PilotId 
	                    ) Availabilities ON Pilot.Id = Availabilities.PilotId
	                    LEFT OUTER JOIN (
		                    SELECT PilotId, COUNT(BookedPilot.Id) BookingCount
			                    FROM BookedPilot 
			                    INNER JOIN Booking ON BookedPilot.BookingId = Booking.Id
			                    WHERE
				                    BookedPilot.Canceled = 0
				                    AND Booking.Canceled = 0
				                    AND Booking.BookingDate BETWEEN DATEADD(day, -30, @Date) AND DATEADD(day, 14, @Date)
			                    GROUP BY PilotId
	                    ) Bookings ON Pilot.Id = Bookings.PilotId
	                    LEFT OUTER JOIN (
		                    SELECT PilotId, COUNT(BookedPilot.Id) BookingCount
			                    FROM BookedPilot 
			                    INNER JOIN Booking ON BookedPilot.BookingId = Booking.Id
			                    WHERE
				                    BookedPilot.Canceled = 0
				                    AND Booking.Canceled = 0
				                    AND CONVERT(date, Booking.BookingDate) = @Date
			                    GROUP BY PilotId
	                    ) BookingsToday ON Pilot.Id = BookingsToday.PilotId

	                    WHERE
		                    Pilot.IsPilot = 1
                ";

                var users = _context.Users.ToList();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("DateParam", date);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            var pilotId = rd.GetString(rd.GetOrdinal("PilotId"));
                            var availabilities = rd.GetInt32(rd.GetOrdinal("AvailabilityCount"));
                            var bookings = rd.GetInt32(rd.GetOrdinal("BookingCount"));
                            var bookingsToday = rd.GetInt32(rd.GetOrdinal("BookingCountToday"));

                            var availablePilot = new AvailablePilot()
                            {
                                Pilot = users.Single(u => u.Id == pilotId),
                                Available = availabilities > 0,
                                Priority = bookings
                                           + 1000*bookingsToday,
                                // a booking the same day is weighted more heavily to avoid pilots getting too many flights a day
                            };
                            availablePilots.Add(availablePilot);
                        }
                    }
                }
            }

            return availablePilots;
        }
    }
}
