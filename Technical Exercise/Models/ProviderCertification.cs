namespace Technical_Exercise.Models
{
    public class ProviderCertification
    {
        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public int ProviderId { get; set; }  // Foreign key
        public Provider Provider { get; set; } = null!;  // Navigation property
        public int CertificationId { get; set; }  // Foreign key
        public Certification Certification { get; set; } = null!;  // Navigation property

    }
}
