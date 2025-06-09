using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication1.Data;
using WebApplication2.Models;

public class DoctorProfileController : Controller
{
    private readonly ApplicationDbContext db;

    public DoctorProfileController(ApplicationDbContext context)
    {
        db = context;
    }
   
    // عرض قائمة الدكاترة المرتبة بالتقييم
    public IActionResult TopRated()
    {
        var doctors = db.DoctorProfile

                              .OrderByDescending(d => d.Rating)
                              .ToList();

        return View(doctors);
    }

    // عرض صورة الدكتور من قاعدة البيانات
    public IActionResult GetPhoto(int id)
    {
        var doctor = db.DoctorProfile.FirstOrDefault(d => d.DoctorID == id);
        if (doctor?.Photo == null)
            return NotFound();

        return File(doctor.Photo, "image/jpeg");
    }
    public IActionResult Details(int id)
    {
        var doctor = db.DoctorProfile.FirstOrDefault(d => d.DoctorID == id);
        if (doctor == null)
        {
            return NotFound();
        }

        var reviews = db.Review
            .Where(r => r.DoctorID == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var viewModel = new DoctorProfileWithReviewsViewModel
        {
            Doctor = doctor,
            Reviews = reviews
        };

        return View(viewModel); // هذا هو المهم! نرسل ViewModel وليس Doctor فقط
    }

    [HttpPost]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
        {
            return BadRequest("No file selected.");
        }

        byte[] photoBytes;
        using (var ms = new MemoryStream())
        {
            await photo.CopyToAsync(ms);
            photoBytes = ms.ToArray();
        }

        // Optional: Check if doctor exists first
        var doctor = db.DoctorProfile
            .FromSqlRaw("SELECT * FROM DoctorProfile WHERE DoctorID = {0}", id)
            .FirstOrDefault();

        if (doctor == null)
        {
            return NotFound();
        }

        // Update photo using SQL
        var param = new[]
        {
        new SqlParameter("@Photo", photoBytes),
        new SqlParameter("@DoctorID", id)
        };

        db.Database.ExecuteSqlRaw("UPDATE DoctorProfile SET Photo = @Photo WHERE DoctorID = @DoctorID", param);

        return RedirectToAction("Details", new { id = id });
    }
    private readonly string connectionString = "Server=.;Database=MediTrackDBSystem;Trusted_Connection=True;TrustServerCertificate=True";

    public IActionResult GetDoctorPhoto(int id)
    {
        byte[] photoData = null;

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT Photo FROM DoctorProfile WHERE DoctorID = @id";
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read() && !reader.IsDBNull(0))
                    {
                        photoData = (byte[])reader["Photo"];
                    }
                }
            }
        }

        if (photoData != null)
        {
            return File(photoData, "image/png"); // تأكد أن الصورة فعلاً PNG أو عدّل النوع حسب المحتوى
        }

        return NotFound(); // لو الصورة مش موجودة
    }
    public IActionResult Search(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return View(new List<DoctorProfile>());
        }

        var results = db.DoctorProfile
                        .Where(d => d.DoctorName.Contains(query) || d.Bio.Contains(query))
                        .ToList();

        return View(results);
    }


    [HttpPost]
    public IActionResult UpdateAvailableDays(int DoctorID, string[] SelectedDays)
    {
        string days = string.Join(",", SelectedDays);

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string sql = "UPDATE DoctorProfile SET AvailableDays = @days WHERE DoctorID = @id";
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@days", days);
                cmd.Parameters.AddWithValue("@id", DoctorID);
                cmd.ExecuteNonQuery();
            }
        }

        return RedirectToAction("Details", new { id = DoctorID });

    }


    [HttpGet]
    public IActionResult EditAvailableDays(int id)
    {
        var role = HttpContext.Session.GetString("Role");
        var currentDoctorId = HttpContext.Session.GetInt32("DoctorID");

        // حماية: المسموح فقط للدكتور نفسه
        if (role != "Doctor" || currentDoctorId != id)
        {
            return Unauthorized(); // أو Redirect مع رسالة
        }

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT DoctorID, AvailableDays FROM DoctorProfile WHERE DoctorID = @id";
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var model = new DoctorProfile
                        {
                            DoctorID = reader.GetInt32(0),
                            AvailableDays = reader.IsDBNull(1) ? "" : reader.GetString(1)
                        };
                        return View(model);
                    }
                }
            }
        }

        return NotFound();
    }
    [HttpPost]
    public IActionResult UpdateBio(int DoctorID, string Bio)
    {
        int? currentUserId = HttpContext.Session.GetInt32("UserID");
        string role = HttpContext.Session.GetString("Role");

        using (var conn = new SqlConnection("Server=.;Database=MediTrackDBSystem;Trusted_Connection=True;TrustServerCertificate=True"))
        {
            conn.Open();

            // تحقق من أن الدكتور المسجل حالياً هو نفسه صاحب البروفايل
            string checkSql = "SELECT UserID FROM DoctorProfile WHERE DoctorID = @id";
            using (var cmd = new SqlCommand(checkSql, conn))
            {
                cmd.Parameters.AddWithValue("@id", DoctorID);
                var result = cmd.ExecuteScalar();

                if (result == null || Convert.ToInt32(result) != currentUserId || role != "Doctor")
                    return Unauthorized();
            }

            // التحديث
            string updateSql = "UPDATE DoctorProfile SET Bio = @bio WHERE DoctorID = @id";
            using (var cmd = new SqlCommand(updateSql, conn))
            {
                cmd.Parameters.AddWithValue("@bio", Bio);
                cmd.Parameters.AddWithValue("@id", DoctorID);
                cmd.ExecuteNonQuery();
            }
        }

        return RedirectToAction("Details", new { id = DoctorID });
    }
    [HttpPost]
    [HttpPost]
    public IActionResult UpdateDoctorInfo(int DoctorID, string DoctorName, string Bio, string ClinicAddress, string AvailableDays)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string sql = @"UPDATE DoctorProfile 
                       SET DoctorName = @DoctorName,
                           Bio = @Bio,
                           ClinicAddress = @Clinic,
                           AvailableDays = @Days
                       WHERE DoctorID = @id";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@DoctorName", DoctorName);
                cmd.Parameters.AddWithValue("@Bio", Bio);
                cmd.Parameters.AddWithValue("@Clinic", ClinicAddress);
                cmd.Parameters.AddWithValue("@Days", AvailableDays);
          
                cmd.Parameters.AddWithValue("@id", DoctorID);
                cmd.ExecuteNonQuery();
            }
        }

        return RedirectToAction("Details", new { id = DoctorID });
    }
    public virtual ICollection<Review> Reviews { get; set; }

    [NotMapped]
    public double AverageRating => Reviews != null && Reviews.Any()
        ? Reviews.Average(r => r.Rating)
        : 0;

}
