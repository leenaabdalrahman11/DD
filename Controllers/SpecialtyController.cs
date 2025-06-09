using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication2.Models;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace WebApplication2.Controllers
{
    public class SpecialtyController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IConfiguration _configuration;



        public SpecialtyController(ApplicationDbContext context, IConfiguration configuration)
        {
            db = context;
            _configuration = configuration;
        }

        // ✅ GET: Specialty
        public IActionResult Index()
        {
            // استخدم SQL صريح لقراءة البيانات
            var specialties = db.Specialty
                .FromSqlRaw("SELECT * FROM Specialty")
                .ToList();

            return View(specialties);
        }

        // ✅ Details: عرض تخصص معين
        public IActionResult Details(int id)
        {
            var specialty = db.Specialty
                .FromSqlRaw("SELECT * FROM Specialty WHERE SpecialtyID = {0}", id)
                .FirstOrDefault();

            if (specialty == null)
            {
                return NotFound();
            }

            return View(specialty);
        }

        // ✅ GetPhoto: عرض صورة تخصص
        public IActionResult GetPhoto(int id)
        {
            var specialty = db.Specialty
                .FromSqlRaw("SELECT * FROM Specialty WHERE SpecialtyID = {0}", id)
                .FirstOrDefault();

            if (specialty?.Photo != null)
            {
                return File(specialty.Photo, "image/png");
            }

            return NotFound();
        }
        public IActionResult getSpacilityDetails(int id)
        {
            var specialty = db.Specialty.FirstOrDefault(s => s.SpecialtyID == id);
            if (specialty == null) return NotFound();

            var doctors = db.SpecialtyDoctor
                .FromSqlRaw(@"SELECT 
                        s.SpecialtyID,
                        s.Name AS SpecialtyName,
                        s.Description AS SpecialtyDescription,
                        d.DoctorID,
                        d.DoctorName,
                        d.Bio,
                        d.Rating,
                        d.AvailableDays,
                        d.ClinicAddress
                      FROM Specialty s
                      INNER JOIN DoctorProfile d ON s.SpecialtyID = d.SpecialtyID
                      WHERE s.SpecialtyID = {0}", id)
                .ToList();

            var viewModel = new SpecialtyDetailsViewModel
            {
                Specialty = specialty,
                Doctors = doctors
            };

            return View(viewModel);

        }
        [HttpPost]
        public IActionResult EditSpecialty(int SpecialtyID, string Name, string Description, IFormFile Photo)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Unauthorized();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                string sql = "UPDATE Specialty SET Name = @Name, Description = @Description WHERE SpecialtyID = @ID";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", Name);
                    cmd.Parameters.AddWithValue("@Description", Description);
                    cmd.Parameters.AddWithValue("@ID", SpecialtyID);
                    cmd.ExecuteNonQuery();
                }

                // إذا تم رفع صورة جديدة
                if (Photo != null && Photo.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        Photo.CopyTo(ms);
                        byte[] photoBytes = ms.ToArray();

                        string updatePhotoSql = "UPDATE Specialty SET Photo = @Photo WHERE SpecialtyID = @ID";
                        using (SqlCommand cmd = new SqlCommand(updatePhotoSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Photo", photoBytes);
                            cmd.Parameters.AddWithValue("@ID", SpecialtyID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            return RedirectToAction("getSpacilityDetails", new { id = SpecialtyID });
        }


    }
}