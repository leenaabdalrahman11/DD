using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication1.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ReviewController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; // تم التمرير هنا
        }

        [HttpPost]
        [HttpPost]
        public IActionResult AddReview(Review review)
        {
            if (HttpContext.Session.GetString("Role") != "Patient")
                return Unauthorized();

            review.CreatedAt = DateTime.Now;

            _context.Review.Add(review); // لا تعطيه ReviewID
            _context.SaveChanges();

            return RedirectToAction("Details", "DoctorProfile", new { id = review.DoctorID });
        }
        [HttpPost]
        public IActionResult DeleteReview(int ReviewID)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return Unauthorized();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                string deleteSql = "DELETE FROM Review WHERE ReviewID = @id";
                using (SqlCommand cmd = new SqlCommand(deleteSql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", ReviewID);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "✅ تم حذف التعليق بنجاح.";
            return Redirect(Request.Headers["Referer"].ToString());
        }



    }
}