using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using WebApplication1.Data;
using WebApplication2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace WebApplication2.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Auth/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            string token = null;
            int userId = 0;
            string userRole = "";

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                // 1. التحقق من المستخدم
                string userSql = "SELECT UserID FROM [AppUser] WHERE Email = @Email AND PasswordHash = @Password";
                using (SqlCommand cmd = new SqlCommand(userSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);

                    var result = cmd.ExecuteScalar();
                    if (result == null)
                    {
                        ViewBag.Error = "Invalid email or password.";
                        return View();
                    }

                    userId = Convert.ToInt32(result);
                }

                // 2. إنشاء جلسة جديدة
                token = Guid.NewGuid().ToString();
                string sessionSql = @"INSERT INTO LoginSession (UserID, Token, CreatedAt, ExpiresAt)
                                      VALUES (@UserID, @Token, @CreatedAt, @ExpiresAt)";

                using (SqlCommand cmd = new SqlCommand(sessionSql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@Token", token);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ExpiresAt", DateTime.Now.AddDays(1));

                    cmd.ExecuteNonQuery();
                }

                string roleSql = "SELECT Role FROM [AppUser] WHERE UserID = @UserID";
                using (SqlCommand cmd = new SqlCommand(roleSql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    userRole = (string)cmd.ExecuteScalar();
                }

                // 3. في حال كان دكتور: جلب DoctorID وربطه
                if (userRole == "Doctor")
                {
                    string doctorSql = "SELECT DoctorID FROM DoctorProfile WHERE UserID = @uid";
                    using (SqlCommand cmd = new SqlCommand(doctorSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        var docResult = cmd.ExecuteScalar();
                        if (docResult != null)
                        {
                            int doctorId = Convert.ToInt32(docResult);
                            HttpContext.Session.SetInt32("DoctorID", doctorId);
                        }
                    }
                }
            }

            HttpContext.Session.SetString("Token", token);
            HttpContext.Session.SetString("Role", userRole);
            HttpContext.Session.SetInt32("UserID", userId);

            return RedirectToAction("IndexUser", "Home");
        }

        public IActionResult AdminOnlyPage()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return Unauthorized();
            }

            return View();
        }


        [HttpPost]
        public IActionResult Register(RegisterViewModel model)

        {
            int newUserId;

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();

                // التحقق من البريد
                string checkEmailSql = "SELECT COUNT(*) FROM [AppUser] WHERE Email = @Email";
                using (SqlCommand checkCmd = new SqlCommand(checkEmailSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", model.Email);
                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        LoadSpecialties(); // عشان لو رجع يعرض الصفحة
                        ViewBag.Error = "❌ البريد الإلكتروني مستخدم من قبل.";
                        return View(model);
                    }
                }

                // إدخال المستخدم الأساسي
                string insertUserSql = @"
            INSERT INTO [AppUser] (FullName, Email, PasswordHash, Phone, Role)
            OUTPUT INSERTED.UserID
            VALUES (@FullName, @Email, @PasswordHash, @Phone, @Role)";
                using (SqlCommand insertCmd = new SqlCommand(insertUserSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@FullName", model.FullName);
                    insertCmd.Parameters.AddWithValue("@Email", model.Email);
                    insertCmd.Parameters.AddWithValue("@PasswordHash", model.PasswordHash);
                    insertCmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Role", model.Role);
                    newUserId = (int)insertCmd.ExecuteScalar();
                }

                // رفع صورة الطبيب (إن وجدت)
                byte[] photoBytes = null;
               
                if (model.PhotoFile != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        model.PhotoFile.CopyTo(ms);
                        photoBytes = ms.ToArray();
                    }
                }


                // إذا كان مريض
                if (model.Role == "Patient")
                {
                    string insertPatientSql = @"
                INSERT INTO PatientProfile (UserID, DateOfBirth, Gender, Address, MedicalFileNumber)
                VALUES (@UserID, @DOB, @Gender, @Address, @FileNumber)";
                    using (SqlCommand cmd = new SqlCommand(insertPatientSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", newUserId);
                        cmd.Parameters.AddWithValue("@DOB", (object?)model.DateOfBirth ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", (object?)model.Address ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FileNumber", "MF" + newUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
                // إذا كان دكتور
                else if (model.Role == "Doctor")
                {
                    model.Rating = 4; // 👈 دائماً نخزن التقييم 4

                    string insertDoctorSql = @"
    INSERT INTO DoctorProfile (UserID, DoctorName, Bio, Rating, AvailableDays, ClinicAddress, Photo, SpecialtyID)
    VALUES (@UserID, @DoctorName, @Bio, @Rating, @Days, @Clinic, @Photo, @SpecialtyID)";
                    using (SqlCommand cmd = new SqlCommand(insertDoctorSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", newUserId);
                        cmd.Parameters.AddWithValue("@DoctorName", model.FullName);
                        cmd.Parameters.AddWithValue("@Bio", (object?)model.Bio ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Rating", model.Rating); // 👈 هنا تُرسل القيمة 4 دائمًا
                        cmd.Parameters.AddWithValue("@Days", (object?)model.AvailableDays ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Clinic", (object?)model.ClinicAddress ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Photo", (object?)photoBytes ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SpecialtyID", (object?)model.SpecialtyID ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

            }

            return RedirectToAction("Login");
        }
        private void LoadSpecialties()
        {
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = "SELECT SpecialtyID, Name FROM Specialty";
                var list = new List<Specialty>();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Specialty
                        {
                            SpecialtyID = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }

                ViewBag.Specialties = list;
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            LoadSpecialties();
            return View();
        }

        public IActionResult DoctorsWithSpecialties()
        {
            var results = new List<DoctorWithSpecialtyViewModel>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = @"
            SELECT a.FullName, s.Name AS Specialty, d.ClinicAddress
            FROM DoctorProfile d
            JOIN AppUser a ON d.UserID = a.UserID
            JOIN Specialty s ON d.SpecialtyID = s.SpecialtyID";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new DoctorWithSpecialtyViewModel
                        {
                            DoctorName = reader["FullName"].ToString(),
                            SpecialtyName = reader["Specialty"].ToString(),
                            ClinicAddress = reader["ClinicAddress"].ToString()
                        });
                    }
                }
            }

            return View(results);
        }


        public IActionResult Logout()
        {
            var token = HttpContext.Session.GetString("Token");

            if (!string.IsNullOrEmpty(token))
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    string deleteSql = "DELETE FROM LoginSession WHERE Token = @Token";

                    using (SqlCommand cmd = new SqlCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Token", token);
                        cmd.ExecuteNonQuery();
                    }
                }

                HttpContext.Session.Clear();
            }

            return RedirectToAction("Login");
        }
        public IActionResult AllUsers()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Unauthorized(); // يمنع الوصول إن لم يكن Admin

            List<AppUser> users = new List<AppUser>();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = "SELECT * FROM AppUser";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new AppUser
                        {
                            UserID = Convert.ToInt32(reader["UserID"]),
                            FullName = reader["FullName"].ToString(),
                            Email = reader["Email"].ToString(),
                            Phone = reader["Phone"]?.ToString(),
                            Role = reader["Role"].ToString()
                        });
                    }
                }
            }

            return View(users);
        }
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    // 1. حذف الملاحظات الطبية المرتبطة بالمريض (لو موجودة)
                    string deleteNotes = @"
                DELETE FROM MedicalNote 
                WHERE AppointmentID IN (
                    SELECT AppointmentID FROM Appointment WHERE PatientID = @id
                )";
                    using (SqlCommand cmd = new SqlCommand(deleteNotes, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. حذف التقييمات المرتبطة بالمريض
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Review WHERE PatientID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. 🔥 حذف المواعيد المرتبطة بالمريض فقط
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Appointment WHERE PatientID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        int rows = cmd.ExecuteNonQuery();
                        TempData["Success"] = $"✅ تم حذف {rows} موعدًا مرتبطًا بالمريض.";
                    }

                    // 4. حذف الجلسات والإشعارات
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM LoginSession WHERE UserID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Notification WHERE UserID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // 5. حذف الملف الشخصي للمريض
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM PatientProfile WHERE UserID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // 6. أخيرًا حذف الحساب نفسه
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM AppUser WHERE UserID = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] += "\n✅ تم حذف المريض بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ فشل الحذف: " + ex.Message;
            }

            return RedirectToAction("AllUsers");
        }

        [HttpPost]
        public IActionResult UpdateUser(int UserID, string FullName, string Phone, string Role)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Unauthorized();

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                string sql = @"UPDATE AppUser 
                       SET FullName = @FullName, Phone = @Phone, Role = @Role 
                       WHERE UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", UserID);
                    cmd.Parameters.AddWithValue("@FullName", FullName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(Phone) ? DBNull.Value : Phone);
                    cmd.Parameters.AddWithValue("@Role", Role ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("AllUsers");
        }

    }
}