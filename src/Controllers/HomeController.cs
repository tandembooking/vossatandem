using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TandemBooking.Services;

namespace TandemBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly BookingCoordinatorSettings _bookingCoordinatorSettings;

        public HomeController(BookingCoordinatorSettings bookingCoordinatorSettings)
        {
            _bookingCoordinatorSettings = bookingCoordinatorSettings;
        }

        public IActionResult Index()
        {
            if (User.IsAdmin() || User.IsPilot())
            {
                return RedirectToAction("Index", "Overview");
            }
            return View();
        }

        public IActionResult Faq()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View(new ContactViewModel()
            {
                BookingCoordinatorPhoneNumber = _bookingCoordinatorSettings.PublicPhoneNumber ?? _bookingCoordinatorSettings.PhoneNumber.AsPhoneNumber(),
                BookingCoordinatorEmail = _bookingCoordinatorSettings.Email,
            });
        }

        public IActionResult Error()
        {
            return View();
        }
    }

    public class ContactViewModel
    {
        public string BookingCoordinatorPhoneNumber { get; set; }
        public string BookingCoordinatorEmail { get; set; }
    }
}
