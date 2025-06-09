using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models
{
    [Keyless]
    public class SpecialtyDoctor
    {
        public int SpecialtyID { get; set; }
        public string SpecialtyName { get; set; }
        public string SpecialtyDescription { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; }
        public string Bio { get; set; }
        public decimal? Rating { get; set; }
        public string AvailableDays { get; set; }
        public string ClinicAddress { get; set; }
    }
}
