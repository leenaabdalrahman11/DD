using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class DoctorProfile
    {
        [Key]
        public int DoctorID { get; set; }

        public int? UserID { get; set; }

        [Required]
        public string DoctorName { get; set; }

        public string? Bio { get; set; }

        public decimal? Rating { get; set; }

        public string? AvailableDays { get; set; }

        public string? ClinicAddress { get; set; }

        public byte[]? Photo { get; set; }

        public int? SpecialtyID { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        // علاقات:
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        // تقييم معدل غير محفوظ في قاعدة البيانات:
        [NotMapped]
        public double AverageRating => Reviews != null && Reviews.Any() ? Reviews.Average(r => r.Rating) : 0;
    }
}
