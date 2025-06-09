namespace WebApplication2.Models
{
    public class DoctorProfileWithReviewsViewModel
    {
        public DoctorProfile Doctor { get; set; }
        public List<Review> Reviews { get; set; }
        public double AverageRating => Reviews.Any() ? Math.Round(Reviews.Average(r => r.Rating), 1) : 0;

    }

}
