using System.Linq;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using TandemBooking.Models;

namespace TandemBooking.Controllers
{
    [Authorize]
    public class UserAdminController : Controller
    {
        private readonly TandemBookingContext _context;

        public UserAdminController(TandemBookingContext context)
        {
            _context = context;
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
        public ActionResult EditUser(string id, ApplicationUser input)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.First(u => u.Id == id);
                user.Name = input.Name;
                user.Email = input.Email;
                user.PhoneNumber = input.PhoneNumber;
                user.IsPilot = input.IsPilot;
                user.IsAdmin = input.IsAdmin;
                _context.SaveChanges();
            }

            return View(input);
        }
    }
}