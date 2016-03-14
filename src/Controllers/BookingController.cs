using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using TandemBooking.Models;
using TandemBooking.ViewModels.Booking;

namespace TandemBooking.Controllers
{
    public class BookingController: Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(new BookingViewModel()
            {
                Passengers = 1,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(BookingViewModel input)
        {
            if (ModelState.IsValid)
            {
                
            }

            return View(input);
        }
    }
}
