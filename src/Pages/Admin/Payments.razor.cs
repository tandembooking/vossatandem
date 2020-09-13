using BlazorInputFile;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using TandemBooking.Models;
using TandemBooking.Services;

namespace TandemBooking.Pages.Admin
{
    public partial class Payments
    {
        [Inject]
        private IZettleService IZettleService { get; set; }

        [Inject]
        private VippsService VippsService { get; set; }

        [Inject]
        private IServiceScopeFactory ScopeFactory { get; set; }

        protected List<Payment> ImportedPayments { get; set; }
        protected string ErrorMessage { get; set; }
        protected string Progress { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await Load();
        }

        protected async Task ImportPaymentsIZettle()
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                var purchases = await IZettleService.GetPayments(DateTime.Now.AddYears(-1), null);
                var payments = purchases.Purchases
                    .SelectMany(purchase => purchase.Payments
                        .Select(payment => new
                        {
                            Purchase = purchase,
                            Payment = payment,
                        })
                    )
                    .ToList();

                var paymentAccounts = db.PaymentAccounts
                    .Where(a => a.PaymentType == PaymentType.IZettle)
                    .ToList();

                foreach (var x in payments)
                {
                    if (!db.Payments.Any(p => p.PaymentType == PaymentType.IZettle && p.ExternalRef == x.Payment.Uuid))
                    {
                        var paymentAccount = paymentAccounts.FirstOrDefault(a =>a.PaymentType == PaymentType.IZettle && a.ExternalRef == x.Purchase.UserId.ToString());
                        if (paymentAccount == null)
                        {
                            paymentAccount = new PaymentAccount
                            {
                                Active = true,
                                PaymentType = PaymentType.IZettle,
                                ExternalRef = x.Purchase.UserId.ToString(),
                                Name = x.Purchase.UserDisplayName,
                            };
                            paymentAccounts.Add(paymentAccount);
                        }

                        var payment = new Payment
                        {
                            PaymentType = PaymentType.IZettle,
                            ExternalRef = x.Payment.Uuid,
                            Amount = x.Payment.Amount / 100m,
                            UnreconciledAmount = x.Payment.Amount / 100m,
                            Fee = 0m,
                            PaymentDate = x.Purchase.Timestamp,
                            InsertDate = DateTimeOffset.Now,
                            PaymentAccount = paymentAccount,
                        };
                        db.Add(payment);
                    }
                }

                await db.SaveChangesAsync();
            }

            await Load();
        }

        public async void ImportPaymentsVippsFiles(IFileListEntry[] files)
        {
            ErrorMessage = "";

            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                foreach (var file in files)
                {
                    try
                    {
                        await ImportPaymentsVippsFile(db, file);
                        await db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage += " " + ex.Message;
                    }
                }

            }

            await Load();
        }

        public async Task ImportPaymentsVippsDownload()
        {
            ErrorMessage = "";

            try
            {
                using (var scope = ScopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                    var importedSettlements = db.VippsSettlements.ToLookup(vs => vs.ExternalRef);

                    var subAccounts = await VippsService.ListSubAccounts();
                    var subAccountIndex = 0;
                    foreach (var subAccountId in subAccounts)
                    {
                        subAccountIndex++;

                        var settlements = await VippsService.ListSettlements(subAccountId);
                        var settlementIndex = 0;
                        foreach (var settlementId in settlements)
                        {
                            var settlementExternalRef = $"{subAccountId}-{settlementId}";
                            settlementIndex++;
                            Progress = $"Account {subAccountIndex}/{subAccounts.Count}, settlement {settlementIndex}/{settlements.Count}";
                            StateHasChanged();

                            if (!importedSettlements[settlementExternalRef].Any())
                            {
                                var settlementFile = await VippsService.GetSettlementFile(subAccountId, settlementId);
                                ImportPaymentsVippsStream(db, settlementFile);
                            }

                            await db.SaveChangesAsync();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ErrorMessage += "Error importing vipps settlement, " + ex.Message;
            }
            finally
            {
                Progress = null;
            }

            await Load();
        }

        private async Task ImportPaymentsVippsFile(TandemBookingContext db, IFileListEntry file)
        {
            if (file.Name.EndsWith(".xml.zip", StringComparison.OrdinalIgnoreCase))
            {
                using (var archive = new ZipArchive(await file.ReadAllAsync(), ZipArchiveMode.Read))
                {
                    using (var strm = archive.Entries.First().Open())
                    {
                        ImportPaymentsVippsStream(db, strm);
                    }
                }
            }
            else
            {
                ErrorMessage += $"File {file.Name} is of unexpected type. ";
            }
        }

        private void ImportPaymentsVippsStream(TandemBookingContext db, Stream settlementFile)
        {
            var doc = XDocument.Load(settlementFile);

            var ns = doc.Root.GetDefaultNamespace();
            var settlementDetails = doc.Root.Element(ns + "SettlementDetailsInfo");

            //make sure we have a payment account
            var accountRef = settlementDetails.Element(ns + "SerialNumber").Value;
            var paymentAccount = db.PaymentAccounts.FirstOrDefault(pa => pa.PaymentType == PaymentType.Vipps && pa.ExternalRef == accountRef);
            if (paymentAccount == null)
            {
                var accountName = settlementDetails.Element(ns + "SaleUnitName").Value;
                paymentAccount = new PaymentAccount
                {
                    Active = true,
                    PaymentType = PaymentType.Vipps,
                    ExternalRef = accountRef,
                    Name = accountName
                };
                db.PaymentAccounts.Add(paymentAccount);
            }

            var settlementId = settlementDetails.Descendants(ns + "SettlementID").First().Value;
            var settlementExternalRef = $"{accountRef}-{settlementId}";
            var settlement = db.VippsSettlements.FirstOrDefault(vs => vs.ExternalRef == settlementExternalRef);
            if (settlement == null)
            {
                settlement = new VippsSettlement
                {
                    ExternalRef = settlementExternalRef,
                    PaymentAccount = paymentAccount,
                    ImportDate = DateTime.UtcNow,
                };
                db.VippsSettlements.Add(settlement);
            }
            else
            {
                return;
            }

            var transactionElems = settlementDetails.Descendants(ns + "TransactionInfo");
            var count = 0;
            foreach (var txElem in transactionElems)
            {
                var externalRef = txElem.Element(ns + "TransactionID").Value;
                if (!db.Payments.Any(p => p.PaymentType == PaymentType.Vipps && p.ExternalRef == externalRef))
                {
                    var paymentDate = DateTimeOffset.Parse(txElem.Element(ns + "TransactionTime").Value, CultureInfo.InvariantCulture);
                    var paymentAmount = decimal.Parse(txElem.Element(ns + "TransactionGrossAmount").Value, CultureInfo.InvariantCulture);
                    var paymentFee = decimal.Parse(txElem.Element(ns + "TransactionFeeAmount").Value, CultureInfo.InvariantCulture);

                    var payment = new Payment
                    {
                        PaymentType = PaymentType.Vipps,
                        ExternalRef = externalRef,
                        Amount = paymentAmount,
                        UnreconciledAmount = paymentAmount,
                        Fee = paymentFee,
                        PaymentDate = paymentDate,
                        InsertDate = DateTimeOffset.Now,
                        PaymentAccount = paymentAccount,
                    };
                    db.Payments.Add(payment);
                    count++;
                }
            }
        }

        private async Task Load()
        {
            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<TandemBookingContext>();

                var payments = await db.Payments
                    .Include(p => p.PaymentAccount)
                    .OrderBy(p => p.PaymentDate)
                    .AsNoTracking()
                    .ToListAsync();
            
                ImportedPayments = payments;
            }
        }


    }
}
