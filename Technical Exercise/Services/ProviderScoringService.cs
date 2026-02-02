using Technical_Exercise.Models;
using Technical_Exercise.Services.Interfaces;

namespace Technical_Exercise.Services
{
    public class ProviderScoringService : IProviderScoringService
    {

        private readonly ILogger<ProviderScoringService> logger;
        public ProviderScoringService(ILogger<ProviderScoringService> logger)
        {
            this.logger = logger;
        }

        public virtual decimal CalculateProviderScore(Provider provider, List<Certification> certifications)
        {

            if (provider == null)
            {
                logger.LogWarning("Provider is null. Returning score 0.");
                return 0;
            }

            try
            {
                var factorScores = GetAvailableFactorScores(provider, certifications);

                var finalScore = factorScores.Any() ? Math.Round(factorScores.Average(), 2) : 0;

                return finalScore;
            }

            catch (Exception ex)
            {
                logger.LogError(ex, "Error calculating provider score for Provider ID: {ProviderId}", provider.Id);
                return 0;
            }
        }

        private List<decimal> GetAvailableFactorScores(Provider provider, List<Certification> certifications)
        {
            var scores = new List<decimal>();
            
            var certificationScore = CalculateCertificationScore(certifications);
            if (certificationScore.HasValue)
                scores.Add(certificationScore.Value);

            var assessmentScore = CalculateAssessmentScore(provider.AssessmentScore);
            if (assessmentScore.HasValue)
                scores.Add(assessmentScore.Value);

            var recencyScore = CalculateRecencyScore(provider.LastActivityDate);
            if (recencyScore.HasValue)
                scores.Add(recencyScore.Value);

            var frequencyScore = CalculateFrequencyScore(provider.ProjectCount);
            if (frequencyScore.HasValue)
                scores.Add(frequencyScore.Value);

            var monetaryScore = CalculateMonetaryScore(provider.AverageProjectValue);
            if (monetaryScore.HasValue)
                scores.Add(monetaryScore.Value);

            return scores;
        }

        private decimal? CalculateCertificationScore(List<Certification> certifications)
        {
            if (certifications == null) return 1;

            try
            { 
                bool hasValidCertifications = certifications.Any(c => !string.IsNullOrWhiteSpace(c.CertificationName));
                return hasValidCertifications ? 9m : 1m;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calculating certification score.");
                return null;
            }
        }

        private decimal? CalculateAssessmentScore(decimal assessmentScore)
        {
            if (assessmentScore < 0 || assessmentScore > 5)
            {
                logger.LogDebug("Assessment score {Score} outside valid range (0-5)", assessmentScore);
                return null;
            }

            return assessmentScore switch
            {
                >= 0 and < 2.26m => 1m,
                >= 2.26m and < 3.76m => 3m,
                >= 3.76m and <= 5m => 9m,
                _ => null
            };

        }

        private decimal? CalculateRecencyScore(DateTime? lastActivityDate)
        {

            if (lastActivityDate == null)
            {
                logger.LogDebug("Last activity date is null.");
                return null;
            }

            try
            {
                var activityDate = lastActivityDate.Value;

                if (activityDate > DateTime.UtcNow)
                {
                    logger.LogWarning("Last activity date {Date} cannot be in the future", activityDate);
                    return null;
                }

                var monthsSinceLastActivity = (DateTime.UtcNow - activityDate).TotalDays / 30;
                return monthsSinceLastActivity switch
                {
                    > 12 => 1m,
                    >= 6 => 3m,
                    < 6 => 6m,
                    _ => null
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calculating recency score");
                return null;
            }
        }

        private decimal? CalculateFrequencyScore(int projectCount)
        {
            if (projectCount < 0)
            {
                logger.LogDebug("Project count {Count} cannot be negative", projectCount);
                return null;
            }
            return projectCount switch
            {
                < 24 => 1m,
                <= 48 => 6m,
                > 48 => 12m,
            };
        }

        private decimal? CalculateMonetaryScore(decimal averageProjectValue)
        {
            if (averageProjectValue < 0)
            {
                logger.LogDebug("Average project value {Value} cannot be negative", averageProjectValue);
                return null;
            }
            return averageProjectValue switch
            {
                < 100000m => 1m,
                < 250000m => 3m,
                >= 250000m => 6m,
            };
        }
    }

}
