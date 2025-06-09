// المسار: Models/RegisterViewModel.cs

using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class RegisterViewModel
    {
        // من AppUser
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string Phone { get; set; }

        [Required]
        public string Role { get; set; }

        // للمريض
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public decimal? Rating { get; set; }

        // للدكتور
        public string Bio { get; set; }
        public string AvailableDays { get; set; }
        public string ClinicAddress { get; set; }
        public int? SpecialtyID { get; set; }
        public IFormFile PhotoFile { get; set; }
    }
}
