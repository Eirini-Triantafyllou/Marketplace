using System.ComponentModel.DataAnnotations;

namespace Technical_Exercise.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string ServiceName { get; set; } = null!;
        public string Domain { get; set; } = null!;
        public string Subdomain { get; set; } = null!;

        [Required]
        [Range(1, 4, ErrorMessage = "Maturity Stage must be between 1 and 4")]
        public int MaturityStage { get; set; }

        public List<ProviderSkill> ProviderSkills { get; set; } = new();  // Navigation property

    }
}
