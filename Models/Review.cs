using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public int Rating { get; set; }  // تقييم من 1 إلى 5
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("DoctorID")]
        public DoctorProfile DoctorProfile { get; set; }

        [ForeignKey("PatientID")]
        public AppUser Patient { get; set; }


    }

}
