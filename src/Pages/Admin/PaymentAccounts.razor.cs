using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using TandemBooking.Models;
using TandemBooking.Services;

namespace TandemBooking.Pages.Admin
{
    public partial class PaymentAccounts
    {
        [Inject]
        private IZettleService IZettleService { get; set; }

        [Inject]
        private IServiceScopeFactory ScopeFactory { get; set; }

        protected List<PaymentAccount> Accounts { get; set; }
        protected Guid? EditId { get; set; }
        protected bool IsInsertMode { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await Load();
        }

        protected async Task ImportAccountsIZettle()
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                var izettleSubAccounts = await IZettleService.GetSubAccounts();
                var existingAccounts = db.PaymentAccounts
                    .Where(pa => pa.PaymentType == PaymentType.IZettle)
                    .ToList();

                foreach (var izettleAccount in izettleSubAccounts)
                {
                    var acct = existingAccounts.FirstOrDefault(a => a.ExternalRef == izettleAccount.Id.ToString());
                    if (acct == null)
                    {
                        acct = new PaymentAccount
                        {
                            PaymentType = PaymentType.IZettle,
                            Active = true,
                            ExternalRef = izettleAccount.Id.ToString(),
                            Name = izettleAccount.Email,
                        };
                        db.Add(acct);
                    }
                }

                foreach (var acct in existingAccounts)
                {
                    int.TryParse(acct.ExternalRef, out var acctId);
                    var izettleAcct = izettleSubAccounts.FirstOrDefault(a => a.Id == acctId);
                    if (izettleAcct == null)
                    {
                        acct.Active = false;
                    }
                }

                await db.SaveChangesAsync();
            }

            await Load();
        }

        private async Task Load()
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                var paymentAccounts = await db.PaymentAccounts
                    .OrderBy(pa => pa.PaymentType)
                    .ThenBy(pa => pa.Name)
                    .AsNoTracking()
                    .ToListAsync();
            
                Accounts = paymentAccounts;
            }
        }

        protected async Task StartEdit(PaymentAccount account)
        {
            EditId = account.Id;
        }

        protected async Task EndEdit()
        {
            EditId = null;
            await Load();
        }

        protected async Task StartInsert()
        {
            IsInsertMode = true;
        }

        protected async Task EndInsert()
        {
            IsInsertMode = false;
            await Load();
        }

        protected async Task Disable(PaymentAccount account)
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();
                var edit = db.PaymentAccounts.Single(a => a.Id == account.Id);
                edit.Active = false;
                if (EnsureUnique(db, account))
                {
                    await db.SaveChangesAsync();
                }
            }

            await Load();
        }

        protected async Task Enable(PaymentAccount account)
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();
                var edit = db.PaymentAccounts.Single(a => a.Id == account.Id);
                edit.Active = true;
                if (EnsureUnique(db, account))
                {
                    await db.SaveChangesAsync();
                }
            }

            await Load();
        }

        private bool EnsureUnique(TandemBookingContext db, PaymentAccount account)
        {
            var duplicate = db.PaymentAccounts.FirstOrDefault(a => a.Id != account.Id && a.PaymentType == account.PaymentType && a.ExternalRef == account.ExternalRef);
            if (duplicate != null)
            {
                return false;
            }

            return true;
        }
    }
}
