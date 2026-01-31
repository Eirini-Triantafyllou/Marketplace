using Technical_Exercise.Models;

namespace Technical_Exercise.Services
{
    public class MatchingService
    {

        private readonly ILogger<MatchingService> logger;
        private readonly ProviderScoringService providerScoringService;
        public MatchingService(ILogger<MatchingService> logger, ProviderScoringService providerScoringService)
        {
            this.logger = logger;
            this.providerScoringService = providerScoringService;
        }

        public List<MatchingResult> FindTopProviders(MatchingRequest request,List<Provider> providers)
        {
            if (providers == null || !providers.Any())
            {
                logger.LogWarning("No providers available for matching.");
                return new List<MatchingResult>();
            }

            var filteredProviders = FilterProviders(providers, request);

        }


        private List<Provider> FilterProviders(List<Provider> providers, MatchingRequest request)
        {
            return providers
                .Where(p => HasRequiredService(p, request.ServiceId)
                .Where(p => MeetsUserCapacity(p, request))
                .Where(p => MatchesCostProfile(p, request.RequestorCostProfile))
                .ToList();
        }

        private bool HasRequiredService(Provider provider, int serviceId)
        {
            return provider.ProviderSkills.Any(ps => ps.ServiceId == serviceId);
        }

        private bool MeetsUserCapacity(Provider provider, MatchingRequest request)
        {
            if (request.NumberOfUsers <= 0)
            {
                return true;                 // No user capacity requirement
            }

            var skill = provider.ProviderSkills
                .FirstOrDefault(ps => ps.ServiceId == request.ServiceId);

            if (skill == null || !skill.MaxUsersSupported.HasValue) return false;

            return skill.MaxUsersSupported.Value >= request.NumberOfUsers;
        }


    }

}
