using Microsoft.EntityFrameworkCore;
using WebApplication2.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)

        {


        }

        public DbSet<SpecialtyDoctor> SpecialtyDoctor{ get; set; }

        public DbSet<Specialty> Specialty { get; set; }
        public DbSet<DoctorProfile> DoctorProfile { get; set; }  // ✅ أضف هذا
        //public DbSet<AppUser> Users { get; set; }
        public DbSet<AppSession> Sessions { get; set; }
        public DbSet<Appointment> Appointment { get; set; }
        public DbSet<Review> Review { get; set; }
        public DbSet<AppUser> AppUser { get; set; }




    }
}
