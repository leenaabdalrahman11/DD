namespace WebApplication2.Models
{
    public class AppointmentWithDoctor
    {
        public int AppointmentID { get; set; }
        public DateTime DateTime { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }

        public string DoctorName { get; set; }
        public string ClinicAddress { get; set; }
    }

}
