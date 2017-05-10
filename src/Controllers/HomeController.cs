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
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
