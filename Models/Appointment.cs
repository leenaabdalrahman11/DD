using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Appointment")]

    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }

        public int DoctorID { get; set; }

        public int PatientID { get; set; }

        public DateTime DateTime { get; set; }

        [MaxLength(255)]
        public string Reason { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }
    }
}
