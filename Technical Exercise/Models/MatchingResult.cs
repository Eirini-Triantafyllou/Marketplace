using System.ComponentModel.DataAnnotations;

namespace Technical_Exercise.Models
{
    public class MatchingResult
    {
        public int Id { get; set; }
        public decimal MatchScore { get; set; }

        [Required]
        [Range(1, 3, ErrorMessage = "Rank must be between 1 and 3")]
        public int Rank { get; set; }

        public int MatchingRequestId { get; set; }  // Foreign key
        public MatchingRequest MatchingRequest { get; set; } = null!;  // Navigation property
        public int ProviderId { get; set; }  // Foreign key
        public Provider Provider { get; set; } = null!;  // Navigation property

    }
}
