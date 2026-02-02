using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Technical_Exercise.Models;
using Technical_Exercise.Services;
using ProviderScoringService = Technical_Exercise.Services.ProviderScoringService;
using Xunit;

namespace TestProject
{
    public class ProviderScoringServiceTests
    {
        private readonly Mock<ILogger<ProviderScoringService>> mockLogger;
        private readonly ProviderScoringService service;

        public ProviderScoringServiceTests()
        {
            mockLogger = new Mock<ILogger<ProviderScoringService>>();
            service = new ProviderScoringService(mockLogger.Object);
        }

        [Fact]
        public void CalculateProviderScore_AllFactorsPresent_ReturnsCorrectScore()
        {
            // ARRANGE
            var provider = new Provider
            {
                Id = 1,
                AssessmentScore = 4.0m,           // 9 points
                LastActivityDate = DateTime.UtcNow.AddMonths(-3), // 6 points (<6 months)
                ProjectCount = 50,                // 12 points (>48)
                AverageProjectValue = 300000m     // 6 points (>250k)
            };

            var certifications = new List<Certification>
            {
                new Certification { Id = 1, CertificationName = "AWS Certified" }
            };

            // ACT
            var score = service.CalculateProviderScore(provider, certifications);

            // ASSERT
            // (9 + 9 + 6 + 12 + 6) / 5 = 42 / 5 = 8.4
            Assert.Equal(8.4m, score);
        }

        // TEST 2: Test with missing factors
        [Fact]
        public void CalculateProviderScore_WithMissingData_ReturnsAverageOfAvailable()
        {
            // ARRANGE
            var provider = new Provider
            {
                Id = 2,
                AssessmentScore = 3.0m,           // 3 points
                LastActivityDate = null,          // null - δεν μετράει
                ProjectCount = 30,                // 6 points (24-48)
                AverageProjectValue = 50000m      // 1 point (<100k)
            };

            var certifications = new List<Certification>(); // Κενή λίστα - 1 point

            // ACT
            var score = service.CalculateProviderScore(provider, certifications);

            // ASSERT
            // (1 + 3 + 6 + 1) / 4 = 11 / 4 = 2.75
            Assert.Equal(2.75m, score);
        }

        // TEST 3: Test edge cases (boundary values)
        [Theory]
        [InlineData(2.25, 1.0)]   // boundary για 1 point
        [InlineData(2.26, 3.0)]   // boundary για 3 points
        [InlineData(3.75, 3.0)]   // boundary για 3 points
        [InlineData(3.76, 9.0)]   // boundary για 9 points
        public void CalculateProviderScore_AssessmentScoreBoundaries_ReturnsCorrectPoints(
            decimal assessmentScore, decimal expectedPoints)
        {
            // ARRANGE
            var provider = new Provider
            {
                Id = 3,
                AssessmentScore = assessmentScore,
                LastActivityDate = DateTime.UtcNow.AddMonths(-1),
                ProjectCount = 30,
                AverageProjectValue = 150000m
            };

            var certifications = new List<Certification>
            {
                new Certification { Id = 1, CertificationName = "Test" }
            };

            // ACT
            var score = service.CalculateProviderScore(provider, certifications);

            // ASSERT
            // Γνωρίζουμε ότι με certifications θα πάρει 9 points
            // Άρα ο μέσος όρος θα είναι: (9 + expectedPoints + 6 + 6 + 3) / 5
            var expectedAverage = (9m + expectedPoints + 6m + 6m + 3m) / 5m;
            Assert.Equal(expectedAverage, score);
        }

        //  TEST 4: Test with null provider
        [Fact]
        public void CalculateProviderScore_NullProvider_ReturnsZero()
        {
            // ARRANGE
            Provider provider = null;
            var certifications = new List<Certification>();

            // ACT
            var score = service.CalculateProviderScore(provider, certifications);

            // ASSERT
            Assert.Equal(0, score);
        }

        // TEST 5: Test recency score calculations
        [Theory]
        [InlineData(13, 1.0)]   // >12 μήνες = 1 point
        [InlineData(8, 3.0)]    // 6-12 μήνες = 3 points
        [InlineData(3, 6.0)]    // <6 μήνες = 6 points
        public void CalculateProviderScore_RecencyCalculations_ReturnsCorrectPoints(
            int monthsAgo, decimal expectedRecencyPoints)
        {
            // ARRANGE
            var provider = new Provider
            {
                Id = 4,
                AssessmentScore = 3.0m,
                LastActivityDate = DateTime.UtcNow.AddMonths(-monthsAgo),
                ProjectCount = 25,
                AverageProjectValue = 120000m
            };

            // Χωρίς certifications για να εστιάσουμε στο recency
            var certifications = new List<Certification>();

            // ACT
            var score = service.CalculateProviderScore(provider, certifications);

            // ASSERT
            // Μόνο τα scores που υπάρχουν: assessment(3), recency, frequency(6), monetary(3)
            var expectedAverage = (3m + expectedRecencyPoints + 6m + 3m + 1m) / 5m;
            Assert.Equal(expectedAverage, score);
        }

    }
}
