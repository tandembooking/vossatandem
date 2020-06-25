using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TandemBooking.Models;
using Microsoft.EntityFrameworkCore;
using TandemBooking.Services;
using OfficeOpenXml;

namespace TandemBooking.Controllers
{
    [Authorize(Policy="IsValidated")]
    public class ReportController : Controller
    {
        private readonly TandemBookingContext _context;
        private readonly UserManager _userManager;

        public ReportController(TandemBookingContext context, UserManager userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> CompletedBookings(string pilotId = null, int? year = null)
        {
            var fromDate = new DateTime(year ?? DateTime.Today.Year, 1, 1);
            var toDate = fromDate.AddYears(1);

            var bookings = _context.Bookings
                .Include(b => b.AssignedPilot)
                .AsNoTracking()
                .Where(b =>
                    !b.Canceled
                    && b.Completed
                    && (
                        (b.CompletedDate == null && b.BookingDate >= fromDate && b.BookingDate < toDate)
                        || (b.CompletedDate != null && b.CompletedDate >= fromDate && b.CompletedDate < toDate)
                    )
                );

            //Pilots should be able to see their own flights
            if (!User.IsAdmin())
            {
                pilotId = _userManager.GetUserId(User);
            }
                
            if (pilotId != null)
            {
                bookings = bookings.Where(b => b.AssignedPilot.Id == pilotId);
            }

            var pilotName = pilotId == null
                ? "all pilots"
                : (await _userManager.FindByIdAsync(pilotId)).Name;

            var orderedBookings = bookings
                .OrderBy(b => b.BookingDate)
                .ToList();

            return View(new CompletedBookingsViewModel()
            {
                Bookings = orderedBookings,
                Year = fromDate.Year,
                PilotId = pilotId,
                PilotName = pilotName
            });
        }

        public async Task<IActionResult> ExportCompletedBookings(string pilotId = null, int? year = null)
        {
            var fromDate = new DateTime(year ?? DateTime.Today.Year, 1, 1);
            var toDate = fromDate.AddYears(1);

            var bookings = _context.Bookings
                .Include(b => b.AssignedPilot)
                .AsNoTracking()
                .Where(b =>
                    !b.Canceled
                    && b.Completed
                    && (
                        (b.CompletedDate == null && b.BookingDate >= fromDate && b.BookingDate <= toDate)
                        || (b.CompletedDate != null && b.CompletedDate >= fromDate && b.CompletedDate <= toDate)
                    )
                );

            //Pilots should be able to see their own flights
            if (!User.IsAdmin())
            {
                pilotId = _userManager.GetUserId(User);
            }

            if (pilotId != null)
            {
                bookings = bookings.Where(b => b.AssignedPilot.Id == pilotId);
            }

            var pilotName = pilotId == null
                ? "all pilots"
                : (await _userManager.FindByIdAsync(pilotId)).Name;

            var orderedBookings = bookings
                .OrderBy(b => b.CompletedDate)
                .ThenBy(b => b.BookingDate)
                .ToList();

            var projectedBookings = orderedBookings.Select(b => new
            {
                b.Id,
                RegisteredDate = b.DateRegistered,
                BookingDate = b.BookingDate,
                CompletedDate = b.CompletedDate,
                InstructorName = b.AssignedPilot.Name,
                InstructorEmail = b.AssignedPilot.Email,
                IZettleAccount = b.PaymentType == PaymentType.IZettle 
                    ? b.IZettleAccount ?? b.AssignedPilot?.IZettleAccount ?? "ukjent iZettle-konto"
                    : "",
                VippsAccount = b.PaymentType == PaymentType.Vipps
                    ? b.VippsAccount ?? b.AssignedPilot?.VippsAccount?? "ukjent Vipps-konto"
                    : "",
                PassengerName = b.PassengerName,
                PassengerEmail = b.PassengerEmail,

                FlightType = b.FlightType switch
                {
                    FlightType.Hangur => "Hangur",
                    FlightType.Winch => "Winch",
                    FlightType.MyrkdalenRokneLiaset => "MyrkdalenRokneLiaset",
                    FlightType.Aurland => "Aurland",
                    _ => "Other",
                },
                BoatDriver = b.BoatDriver,
                PassengerFee = b.PassengerFee,
                PilotFee = b.PilotFee,
                BoatDriverFee = b.BoatDriverFee,
            });

            using (var wb = new ExcelPackage())
            {
                var ws = wb.Workbook.Worksheets.Add("Completed Bookings");
                var range = ws.Cells["A1"].LoadFromCollection(projectedBookings, true, OfficeOpenXml.Table.TableStyles.Medium1);
                foreach (var cell in range)
                {
                    if (cell.Value is DateTime dateValue)
                    {
                        cell.Style.Numberformat.Format = "yyyy-mm-dd";
                    }
                }
                range.AutoFitColumns();

                return File(wb.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"completed-flights-{year}-{DateTime.Now.Date:o}.xlsx");
            }
        }

        [Authorize(Policy = "IsAdmin")]
        public IActionResult BookingsByPilot(int? year = null)
        {
            var fromDate = new DateTime(year ?? DateTime.Today.Year, 1, 1);
            var toDate = fromDate.AddYears(1);
            if (toDate > DateTime.Today)
            {
                toDate = DateTime.Today;
            }

            var pilots = _context.Users.ToList();

            var basicQuery = _context.Bookings
                .AsNoTracking()
                .Where(b => !b.Canceled && b.AssignedPilotId != null && b.BookingDate >= fromDate && b.BookingDate < toDate);

            var pilotStats = _context.Bookings
                .AsNoTracking()
                .Where(b => !b.Canceled && b.AssignedPilotId != null && b.BookingDate >= fromDate && b.BookingDate < toDate)
                .Where(b => b.PassengerFee > 0)
                .ToList()
                .GroupBy(b => b.AssignedPilotId)
                .Select(grp => new BookingsByPilotViewModelItem
                {
                    PilotId = grp.Key,
                    PilotName = pilots.First(p => p.Id == grp.Key).Name,
                    CompletedFlights = grp.Count(b => b.Completed),
                    FlightsMissingStatus = grp.Count(b => !b.Completed),
                })
                .ToList()
                .OrderByDescending(b => b.CompletedFlights + b.FlightsMissingStatus)
                .ToList();

            return View(new BookingsByPilotViewModel()
            {
                Year = fromDate.Year,
                PilotStats = pilotStats,
            });
        }
    }
}