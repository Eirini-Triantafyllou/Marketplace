using Technical_Exercise.Models;

namespace Technical_Exercise.Services.Interfaces
{
    public interface IProviderScoringService
    {
        decimal CalculateProviderScore(Provider provider, List<Certification> certifications);
    }
}
