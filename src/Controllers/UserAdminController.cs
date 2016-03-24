using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using TandemBooking.Models;
using TandemBooking.Services;

namespace TandemBooking.Controllers
{
    [Authorize]
    public class UserAdminController : Controller
    {
        private readonly TandemBookingContext _context;
        private readonly NexmoService _nexmo;

        public UserAdminController(TandemBookingContext context, NexmoService nexmo)
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
        public ActionResult EditUser(string id)
        {
            var user = _context.Users.First(u => u.Id == id);
            return View(user);
        }

        [HttpPost]
        public async Task<ActionResult> EditUser(string id, ApplicationUser input)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.First(u => u.Id == id);
                user.Name = input.Name;
                user.Email = input.Email;
                input.PhoneNumber = await _nexmo.FormatPhoneNumber(input.PhoneNumber);
                user.PhoneNumber = input.PhoneNumber;
                user.IsPilot = input.IsPilot;
                user.IsAdmin = input.IsAdmin;
                _context.SaveChanges();
            }

            return View(input);
        }
    }
}