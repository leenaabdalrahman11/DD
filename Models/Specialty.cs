using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class Specialty
    {
        [Key]
        public int? SpecialtyID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(250)]
        [Required]
        public string Description { get; set; }

        // الصورة مخزنة داخل قاعدة البيانات
        public byte[] Photo { get; set; }
    }
}
