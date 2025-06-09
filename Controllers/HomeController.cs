using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {

    
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext dp;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dp)
        {
            _logger = logger;
            this.dp = dp;
        }



        public IActionResult Index()
        {
            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
            {
                // المستخدم غير مسجّل الدخول
                return View("IndexGuest"); // صفحة الزائر
            }

            // المستخدم مسجّل الدخول
            return RedirectToAction("IndexUser");
            // صفحة مخصصة للمستخدم المسجّل
        }

        public IActionResult IndexUser()
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            var topDoctors = dp.DoctorProfile
                               .Include(d => d.Reviews)
                               .ToList()
                               .OrderByDescending(d => d.Reviews.Any() ? d.Reviews.Average(r => r.Rating) : 0)
                               .ToList();
            var reviews = dp.Review
                .Include(r => r.Patient)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToList();


            var specialties = dp.Specialty.ToList();

            var viewModel = new HomePageViewModel
            {
                Doctors = topDoctors,
                Specialties = specialties,
                Reviews = reviews
            };

            return View(viewModel);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
