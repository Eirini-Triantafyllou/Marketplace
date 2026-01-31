namespace Technical_Exercise.Models
{
    public class ProviderSkill
    {
        public int? MaxUsersSupported { get; set; }
        public int YearsOfExperience { get; set; }

        public int ProviderId { get; set; }  // Foreign key
        public Provider Provider { get; set; } = null!;  // Navigation property
        public int ServiceId { get; set; }  // Foreign key
        public Service Service { get; set; } = null!;  // Navigation property
    }
}
