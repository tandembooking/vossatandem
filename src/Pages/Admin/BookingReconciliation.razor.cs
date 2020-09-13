using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TandemBooking.Models;

namespace TandemBooking.Pages.Admin
{
    public partial class BookingReconciliation
    {
        [Inject] private IServiceScopeFactory ScopeFactory { get; set; }

        protected List<Booking> UnreconciledBookings { get; set; }
        protected List<Payment> UnreconciledPayments { get; set; }
        protected List<PaymentAccount> PaymentAccounts { get; set; }

        protected Booking SelectedBooking { get; set; }
        protected List<Payment> FilteredPayments { get; set; }
        protected List<PaymentAccount> FilteredPaymentAccounts { get; set; }

        protected string ErrorMessage { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await Load();
        }

        public async Task SelectBooking(Booking booking)
        {
            SelectedBooking = booking;

            if (SelectedBooking.PaymentAccount == null)
            {
                if (booking.AssignedPilot != null)
                {
                    if (booking.PaymentType == PaymentType.Vipps)
                    {
                        booking.PaymentAccountId = booking.AssignedPilot.VippsPaymentAccountId;
                    }
                    else if (booking.PaymentType == PaymentType.IZettle)
                    {
                        booking.PaymentAccountId = booking.AssignedPilot.IZettlePaymentAccountId;
                    }
                    booking.PaymentAccount = PaymentAccounts.FirstOrDefault(a => a.Id == booking.PaymentAccountId);
                }
            }

            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                FilteredPaymentAccounts = PaymentAccounts
                    .Where(p => p.PaymentType == booking.PaymentType)
                    .ToList();

                FilteredPayments = db.Payments
                    .Where(p => p.PaymentAccountId == SelectedBooking.PaymentAccountId)
                    .Where(p => p.UnreconciledAmount > 0)
                    .AsEnumerable()
                    .OrderBy(p => Math.Abs((p.PaymentDate - (booking.CompletedDate ?? booking.BookingDate)).TotalSeconds))
                    .ToList();
            }
                                            
        }

        protected async Task OnPaymentTypeSelected(ChangeEventArgs args)
        {
            var paymentType = Enum.Parse<PaymentType>((string)args.Value);

            if (SelectedBooking != null)
            {
                SelectedBooking.PaymentType = paymentType;
                SelectedBooking.PaymentAccount = null;
                SelectedBooking.PaymentAccountId = null;
                await SelectBooking(SelectedBooking);
            }
        }


        protected async Task OnAccountSelected(ChangeEventArgs args)
        {
            var accountId = Guid.Parse((string)args.Value);

            if (SelectedBooking != null)
            {
                var account = PaymentAccounts.FirstOrDefault(x => x.Id == accountId);
                SelectedBooking.PaymentAccount = account;
                SelectedBooking.PaymentAccountId = account?.Id;
                await SelectBooking(SelectedBooking);
            }
        }

        protected async Task AddBookingPayment(Booking booking, Payment payment)
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                var bookingLinks = db.Set<BookingPayment>()
                    .Where(bp => bp.BookingId == booking.Id)
                    .ToList();

                if (bookingLinks.Any())
                {
                    ErrorMessage = "Booking already has a payment";
                }

                var paymentLinks = db.Set<BookingPayment>()
                    .Where(bp => bp.PaymentId == payment.Id)
                    .ToList();

                if (bookingLinks.Any(bp => bp.PaymentId == payment.Id))
                {
                    ErrorMessage = "Booking and payment is already connected";
                    return;
                }

                var pmt = db.Payments.Single(p => p.Id == payment.Id);
                if (pmt.UnreconciledAmount < booking.PassengerFee)
                {
                    ErrorMessage = $"{booking.PassengerFee} required, only has {payment.UnreconciledAmount} available";
                    return;
                }

                var bkng = db.Bookings
                    .Include(b => b.BookingPayments)
                    .Single(b => b.Id == booking.Id);
                bkng.ReconciledDate = DateTime.Now;

                pmt.UnreconciledAmount -= booking.PassengerFee;

                bkng.BookingPayments.Add(new BookingPayment
                {
                    Amount = bkng.PassengerFee,
                    InsertDate = DateTime.Now,
                    Booking = bkng,
                    Payment = pmt,
                });

                await db.SaveChangesAsync();
            }

            SelectedBooking = null;
            await Load();
        }


        private async Task Load()
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                UnreconciledBookings = await db.Bookings
                    .Where(b => b.Completed && !b.Canceled && b.PassengerFee > 0 && b.ReconciledDate == null)
                    .Include(b => b.AssignedPilot)
                    .Include(b => b.PaymentAccount)
                    .Include(b => b.BookingPayments)
                    .OrderBy(b => b.BookingDate)
                    .AsNoTracking()
                    .ToListAsync();

                UnreconciledPayments = await db.Payments
                    .Where(p => p.UnreconciledAmount > 0)
                    .Include(p => p.PaymentAccount)
                    .OrderBy(p => p.PaymentDate)
                    .AsNoTracking()
                    .ToListAsync();

                PaymentAccounts = await db.PaymentAccounts
                    .Where(p => p.Active)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
        }


    }
}
