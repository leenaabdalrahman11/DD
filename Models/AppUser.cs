using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("User")]
   public class AppUser
    {
        [Key]

        public int UserID { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Phone { get; set; }
    public string Role { get; set; }

    // Patient only
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; }
    public string Address { get; set; }

        // Doctor only
        public int? SpecialtyID { get; set; } // <-- هذا الجديد
        public string Bio { get; set; }
    public decimal? Rating { get; set; }
    public string AvailableDays { get; set; }
    public string ClinicAddress { get; set; }
    public byte[] Photo { get; set; }
}

}
