using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using TandemBooking.Models;
using TandemBooking.Services;

namespace TandemBooking.Controllers
{
    [Authorize]
    public class UserAdminController : Controller
    {
        private readonly TandemBookingContext _context;
        private readonly INexmoService _nexmo;

        public UserAdminController(TandemBookingContext context, INexmoService nexmo)
        {
            _context = context;
            _nexmo = nexmo;
        }

        public ActionResult Index()
        {
            var users = _context.Users
                .OrderBy(u => u.Name)
                .ToList();

            return View(users);
        }

        [HttpGet]
        [Authorize(Policy = "IsAdmin")]
        public ActionResult EditUser(string id)
        {
            var user = _context.Users.First(u => u.Id == id);
            var accounts = _context.PaymentAccounts
                .Where(a => a.Active)
                .OrderBy(a => a.Name)
                .ToList();
            return View(new EditUserViewModel
            {
                User = user,
                IZettleAccounts = accounts.Where(a => a.PaymentType == PaymentType.IZettle).ToList(),
                VippsAccounts = accounts.Where(a => a.PaymentType == PaymentType.Vipps).ToList(),
            });
        }

        [HttpPost]
        [Authorize(Policy = "IsAdmin")]
        public async Task<ActionResult> EditUser(string id, ApplicationUser user)
        {
            if (ModelState.IsValid)
            {
                var updateUser = _context.Users.First(u => u.Id == id);
                updateUser.Name = user.Name;
                updateUser.Email = user.Email;
                user.PhoneNumber = await _nexmo.FormatPhoneNumber(user.PhoneNumber);
                updateUser.PhoneNumber = user.PhoneNumber;
                updateUser.IsPilot = user.IsPilot;
                updateUser.IsAdmin = user.IsAdmin;
                updateUser.IZettlePaymentAccountId = user.IZettlePaymentAccountId;
                updateUser.VippsPaymentAccountId = user.VippsPaymentAccountId;
                _context.SaveChanges();
            }

            return EditUser(id);
        }
    }

    public class EditUserViewModel
    {
        public ApplicationUser User { get; set; }
        public List<PaymentAccount> VippsAccounts { get; set; }
        public List<PaymentAccount> IZettleAccounts { get; set; }
    }
}