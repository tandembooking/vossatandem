using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TandemBooking.Controllers;
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

        public async Task<List<AvailablePilot>> GetAvailablePilotsAsync(DateTime date, Guid? locationId)
        {
            var availablePilots = new List<AvailablePilot>();
            var conn = (SqlConnection) _context.Database.GetDbConnection();
            await _context.Database.OpenConnectionAsync();

            var sql = @"
                DECLARE @Date date = CONVERT(date, @DateParam)

                SELECT Pilots.Id PilotId, COALESCE(AvailabilityCount, 0) AvailabilityCount, COALESCE(Bookings.BookingCount, 0) BookingCount, COALESCE(BookingsToday.BookingCount, 0) BookingCountToday
	                FROM AspNetUsers Pilots
	                LEFT OUTER JOIN (
		                SELECT PilotId, COUNT(Id) AvailabilityCount FROM PilotAvailabilities
		                WHERE
			                CONVERT(date, PilotAvailabilities.[Date]) = @Date
                            AND (
                                LocationId = @LocationId
                                OR LocationId IS NULL
                                OR @LocationId IS NULL
                            )
		                GROUP BY PilotId 
	                ) Availabilities ON Pilots.Id = Availabilities.PilotId
	                LEFT OUTER JOIN (
		                SELECT PilotId, COUNT(BookedPilots.Id) BookingCount
			                FROM BookedPilots 
			                INNER JOIN Bookings ON BookedPilots.BookingId = Bookings.Id
			                WHERE
                                COALESCE(PassengerFee, 0) > 0
				                AND BookedPilots.Canceled = 0
				                AND Bookings.Canceled = 0
				                AND Bookings.BookingDate BETWEEN DATEADD(day, -30, @Date) AND DATEADD(day, 14, @Date)
                                AND Bookings.BookingDate != @Date
			                GROUP BY PilotId
	                ) Bookings ON Pilots.Id = Bookings.PilotId
	                LEFT OUTER JOIN (
		                SELECT PilotId, COUNT(BookedPilots.Id) BookingCount
			                FROM BookedPilots 
			                INNER JOIN Bookings ON BookedPilots.BookingId = Bookings.Id
			                WHERE
                                COALESCE(PassengerFee, 0) > 0
				                AND BookedPilots.Canceled = 0
				                AND Bookings.Canceled = 0
				                AND CONVERT(date, Bookings.BookingDate) = @Date
			                GROUP BY PilotId
	                ) BookingsToday ON Pilots.Id = BookingsToday.PilotId

	                WHERE
		                Pilots.IsPilot = 1
            ";

            var users = _context.Users.ToList();

            var isDuringVeko = (date > new DateTime(2019, 6, 23) && date < new DateTime(2019, 6, 30));

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = _context.Database.GetExistingTransaction()?.GetDbTransaction() as SqlTransaction;
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("DateParam", date);
                cmd.Parameters.AddWithValue("LocationId", (object)locationId ?? DBNull.Value);

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
                            Priority = isDuringVeko
                                ? bookingsToday
                                : bookings + 1000 * bookingsToday,
                            // a booking the same day is weighted more heavily to avoid pilots getting too many flights a day
                        };
                        availablePilots.Add(availablePilot);
                    }
                }
            }

            return availablePilots;
        }
    }
}
