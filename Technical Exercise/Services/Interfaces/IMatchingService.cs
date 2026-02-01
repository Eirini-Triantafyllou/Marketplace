using Technical_Exercise.Models;

namespace Technical_Exercise.Services.Interfaces
{
    public interface IMatchingService
    {
        List<MatchingResult> FindTopProviders(MatchingRequest request, List<Provider> providers);
    }
}
