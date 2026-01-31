using System.ComponentModel.DataAnnotations;
using Technical_Exercise.Core.Enums;

namespace Technical_Exercise.Models
{
    public class Requestor
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = null!;
        public string CompanyDetails { get; set; } = null!;
        public decimal Revenue { get; set; }
        public int EmployeeCount { get; set; }
        public CostProfile CostProfile { get; set; }

        [Required]
        [Range(1, 4, ErrorMessage = "Digital Maturity Index must be between 1 and 4")]
        public int DigitalMaturityIndex { get; set; }
        public string Location { get; set; } = null!;
        public List<MatchingRequest> MatchingRequests { get; set; } = new();   // Navigation property

    }
}
