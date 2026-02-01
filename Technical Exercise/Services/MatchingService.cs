using Technical_Exercise.Models;
using Technical_Exercise.Core.Enums;
using Technical_Exercise.Services.Interfaces;

namespace Technical_Exercise.Services
{
    public class MatchingService : IMatchingService
    {

        private readonly ILogger<MatchingService> logger;
        private readonly ProviderScoringService providerScoringService;
        public MatchingService(ILogger<MatchingService> logger, ProviderScoringService providerScoringService)
        {
            this.logger = logger;
            this.providerScoringService = providerScoringService;
        }

        public List<MatchingResult> FindTopProviders(MatchingRequest request, List<Provider> providers)
        {
            if (request == null)
            {
                logger.LogError("Matching request is null.");
                throw new ArgumentNullException(nameof(request), "Matching request cannot be null.");
            }

            if (providers == null || !providers.Any())
            {
                logger.LogWarning("No providers available for matching.");
                return new List<MatchingResult>();
            }

            try
            {
                var filteredProviders = FilterProvidersStrict(providers, request, request.Requestor);

                if (!filteredProviders.Any())
                {
                    logger.LogInformation("No providers found with strict matching. Trying relaxed matching.");
                    filteredProviders = FilterProvidersRelaxed(providers, request, request.Requestor);
                }

                if (!filteredProviders.Any())
                {
                    logger.LogInformation("No providers found even with relaxed matching.");
                    return new List<MatchingResult>();
                }

                var providersWithScores = CalculateProviderScores(filteredProviders, request);

                var rankedResults = RankProviders(providersWithScores);

                logger.LogInformation("Found {Count} matches", rankedResults.Count);
                return rankedResults;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while finding top providers.");
                return new List<MatchingResult>();
            }
        }


        private List<Provider> FilterProvidersStrict(List<Provider> providers, MatchingRequest request, Requestor requestor)
        {
            return providers
                .Where(p => HasRequiredService(p, request.ServiceId))
                .Where(p => MeetsUserCapacity(p, request))
                .Where(p => MatchesCostProfile(p, requestor.CostProfile))
                .Where(p => MatchesDigitalMaturity(p, requestor.DigitalMaturityIndex))
                .Where(p => !request.LocationProximityRequired || MatchesLocation(p, requestor.Location))
                .ToList();
        }

        private List<Provider> FilterProvidersRelaxed(List<Provider> providers, MatchingRequest request, Requestor requestor)
        {
            // RELAXED matching: Μόνο τα πιο κρίσιμα κριτήρια
            // 1. Υπηρεσία (υποχρεωτικό)
            // 2. User capacity (υποχρεωτικό αν καθοριστεί)
            // Τα άλλα κριτήρια (cost, maturity, location) 
            // λαμβάνονται υπόψη μόνο μέσω του bonus score
            return providers
                .Where(p => HasRequiredService(p, request.ServiceId))
                .Where(p => MeetsUserCapacity(p, request))
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

        private bool MatchesCostProfile(Provider provider, CostProfile requestorCostProfile)
        {
            if (provider == null) return false;

            var providerSize = GetProviderSize(provider.EmployeeCount);

            return (requestorCostProfile, providerSize) switch
            {
                (CostProfile.Low, "VerySmall") => true,
                (CostProfile.Low, "Small") => true,
                (CostProfile.Medium, "Small") => true,
                (CostProfile.Medium, "SME") => true,
                (CostProfile.High, "SME") => true,
                (CostProfile.High, "Big") => true,
                _ => false
            };
        }

        private string GetProviderSize(int employeeCount)
        {
            return employeeCount switch
            {
                < 10 => "VerySmall",
                < 50 => "Small",
                < 250 => "SME",
                _ => "Big"
            };
        }

        private bool MatchesDigitalMaturity(Provider provider, int digitalMaturity)
        {
            var providerService = provider.ProviderSkills?
               .FirstOrDefault(ps => ps.Service != null)?.Service;

            if (providerService == null || providerService.MaturityStage == 0)
                return false;

            int providerMaturity = providerService.MaturityStage;

            return Math.Abs(providerMaturity - digitalMaturity) <= 1;
        }

        private bool MatchesLocation(Provider provider, string requestorLocation)
        {
            return string.Equals(provider.Location, requestorLocation, StringComparison.OrdinalIgnoreCase);

        }

        private List<(Provider provider, decimal score)> CalculateProviderScores(List<Provider> providers, MatchingRequest request)
        {
            var result = new List<(Provider provider, decimal score)>();

            foreach (var provider in providers)
            {
                var certifications = provider.Certifications?
                    .Select(pc => pc.Certification)
                    .Where(c => c != null)
                    .ToList() ?? new List<Certification>();

                decimal score = providerScoringService.CalculateProviderScore(provider, certifications);   

                decimal bonusScore = CalculateBonusScore(provider, request, request.Requestor);
                decimal totalScore = score + bonusScore;

                result.Add((provider, totalScore));
            }
            return result;
        }

        private decimal CalculateBonusScore(Provider provider, MatchingRequest request, Requestor requestor)
        {
            decimal bonus = 0;

            if (MatchesCostProfile(provider, requestor.CostProfile))
                bonus += 3;

            if (MatchesDigitalMaturity(provider, requestor.DigitalMaturityIndex))
                bonus += 2;

            if (request.LocationProximityRequired &&
                MatchesLocation(provider, requestor.Location))
                bonus += 1;

            return bonus;
        }

        private List<MatchingResult> RankProviders(List<(Provider provider, decimal score)> providersWithScores)
        {
            
            return providersWithScores
                .OrderByDescending(p => p.score)  
                .ThenByDescending(p => p.provider.ProjectCount)  
                .ThenByDescending(p => p.provider.AssessmentScore)  
                .Select((p, index) => new MatchingResult
                {
                    Provider = p.provider,
                    MatchScore = p.score,
                    Rank = index + 1
                })
                .Take(3)  
                .ToList();
        }
    }
}
