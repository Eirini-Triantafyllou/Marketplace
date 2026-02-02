using System.ComponentModel.DataAnnotations;

namespace Technical_Exercise.Models
{
    public class Provider
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = null!;
        public string CompanyDetails { get; set; } = null!;
        public int EmployeeCount { get; set; }
        public string Location { get; set; } = null!;

        [Required]
        [Range(0, 5, ErrorMessage = "Assessment Score must be between 0 and 5")]
        public decimal AssessmentScore { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int ProjectCount { get; set; }
        public decimal AverageProjectValue { get; set; }
        public decimal CurrentScore { get; set; }

        public List<ProviderSkill> ProviderSkills { get; set; } = new();  // Navigation property
        public ICollection<ProviderCertification> Certifications { get; set; } = new List<ProviderCertification>();
        public ICollection<MatchingResult> MatchingResults { get; set; } = new List<MatchingResult>();
    }
}
