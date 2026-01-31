namespace Technical_Exercise.Models
{
    public class MatchingRequest
    {
        public int Id { get; set; }
        public int NumberOfUsers { get; set; }
        public bool LocationProximityRequired { get; set; } = false;

        public int RequestorId { get; set; }  // Foreign key
        public Requestor Requestor { get; set; } = null!;  // Navigation property
        public int ServiceId { get; set; }  // Foreign key
        public Service Service { get; set; } = null!;  // Navigation property

        public List<MatchingResult> MatchingResults { get; set; } = new();  // Navigation property
    }
}
