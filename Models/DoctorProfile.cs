using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class DoctorProfile
    {
        [Key]
        public int DoctorID { get; set; }
        public int? SpecialtyID { get; set; } // ✅
        public string DoctorName { get; set; }
        public string Bio { get; set; }
        public decimal? Rating { get; set; }
        public string AvailableDays { get; set; }
        public string ClinicAddress { get; set; }
        public byte[]? Photo { get; set; }
        public int? UserID { get; set; }

    }

}
