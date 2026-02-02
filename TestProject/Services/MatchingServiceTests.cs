using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Technical_Exercise.Models;
using Technical_Exercise.Services;
using Technical_Exercise.Core.Enums;
using MatchingService = Technical_Exercise.Services.MatchingService;
using Xunit;

namespace TestProject.Services
{
    public class MatchingServiceTests
    {
        private readonly Mock<ILogger<MatchingService>> mockLogger;
        private readonly Mock<ProviderScoringService> scoringServiceMock;
        private readonly MatchingService service;

        public MatchingServiceTests()
        {
            mockLogger = new Mock<ILogger<MatchingService>>();
            scoringServiceMock = new Mock<ProviderScoringService>(
                Mock.Of<ILogger<ProviderScoringService>>());
            service = new MatchingService(mockLogger.Object, scoringServiceMock.Object);
        }

        //  TEST 1: Test finding exact matches
        [Fact]
        public void FindTopProviders_ExactMatches_ReturnsRankedProviders()
        {
            // ARRANGE
            var request = new MatchingRequest
            {
                ServiceId = 1,
                NumberOfUsers = 10,
                Requestor = new Requestor
                {
                    CostProfile = CostProfile.Medium,
                    DigitalMaturityIndex = 3,
                    Location = "Athens"
                },
                LocationProximityRequired = true
            };

            var providers = new List<Provider>
            {
                new Provider
                {
                    Id = 1,
                    EmployeeCount = 40,  // Small (matches Medium cost profile)
                    Location = "Athens",
                    ProjectCount = 30,
                    AssessmentScore = 4.0m,
                    ProviderSkills = new List<ProviderSkill>
                    {
                        new ProviderSkill
                        {
                            ServiceId = 1,
                            MaxUsersSupported = 20,
                            Service = new Service { MaturityStage = 3 }
                        }
                    }
                },
                new Provider
                {
                    Id = 2,
                    EmployeeCount = 100, // SME (matches Medium cost profile)
                    Location = "Athens",
                    ProjectCount = 25,
                    AssessmentScore = 3.5m,
                    ProviderSkills = new List<ProviderSkill>
                    {
                        new ProviderSkill
                        {
                            ServiceId = 1,
                            MaxUsersSupported = 15,
                            Service = new Service { MaturityStage = 4 }
                        }
                    }
                }
            };

            // Mock το scoring service να επιστρέφει προβλέψιμα scores
            scoringServiceMock.Setup(s => s.CalculateProviderScore(
                It.IsAny<Provider>(), It.IsAny<List<Certification>>()))
                .Returns((Provider p, List<Certification> c) =>
                    p.Id == 1 ? 8.0m : 7.0m);

            // ACT
            var results = service.FindTopProviders(request, providers);

            // ASSERT
            Assert.Equal(2, results.Count);
            Assert.Equal(1, results[0].Rank);
            Assert.Equal(2, results[1].Rank);
            Assert.Equal(1, results[0].Provider.Id); // Πρώτος λόγω υψηλότερου score
        }

        // TEST 2: Test when no providers match
        [Fact]
        public void FindTopProviders_NoMatchingProviders_ReturnsEmptyList()
        {
            // ARRANGE
            var request = new MatchingRequest
            {
                ServiceId = 99,  // Service που κανένας δεν προσφέρει
                Requestor = new Requestor { CostProfile = CostProfile.Low }
            };

            var providers = new List<Provider>
            {
                new Provider
                {
                    Id = 1,
                    ProviderSkills = new List<ProviderSkill>
                    {
                        new ProviderSkill { ServiceId = 1 }  // Διαφορετικό service
                    }
                }
            };

            // ACT
            var results = service.FindTopProviders(request, providers);

            // ASSERT
            Assert.Empty(results);
        }

        //  TEST 3: Test with fewer than 3 providers
        [Fact]
        public void FindTopProviders_FewerThanThreeProviders_ReturnsAvailable()
        {
            // ARRANGE
            var request = new MatchingRequest
            {
                ServiceId = 1,
                Requestor = new Requestor { CostProfile = CostProfile.High }
            };

            // Μόνο 2 providers που ταιριάζουν
            var providers = new List<Provider>
            {
                new Provider
                {
                    Id = 1,
                    EmployeeCount = 300,  // Big (matches High cost profile)
                    ProviderSkills = new List<ProviderSkill>
                    {
                        new ProviderSkill { ServiceId = 1 }
                    }
                },
                new Provider
                {
                    Id = 2,
                    EmployeeCount = 200,  // SME (matches High cost profile)
                    ProviderSkills = new List<ProviderSkill>
                    {
                        new ProviderSkill { ServiceId = 1 }
                    }
                }
            };

            scoringServiceMock.Setup(s => s.CalculateProviderScore(
                It.IsAny<Provider>(), It.IsAny<List<Certification>>()))
                .Returns(5.0m);

            // ACT
            var results = service.FindTopProviders(request, providers);

            // ASSERT
            Assert.Equal(2, results.Count);  // Μόνο 2, όχι 3
            Assert.Equal(1, results[0].Rank);
            Assert.Equal(2, results[1].Rank);
        }

        //  TEST 4: Test proper ranking
        [Fact]
        public void FindTopProviders_MultipleProviders_ReturnsProperRanking()
        {
            // ARRANGE
            var request = new MatchingRequest
            {
                ServiceId = 1,
                Requestor = new Requestor { CostProfile = CostProfile.Medium }
            };

            var providers = new List<Provider>
            {
                new Provider { Id = 1, ProjectCount = 10, AssessmentScore = 2.0m,
                    ProviderSkills = new List<ProviderSkill> { new() { ServiceId = 1 } } },
                new Provider { Id = 2, ProjectCount = 20, AssessmentScore = 3.0m,
                    ProviderSkills = new List<ProviderSkill> { new() { ServiceId = 1 } } },
                new Provider { Id = 3, ProjectCount = 15, AssessmentScore = 4.0m,
                    ProviderSkills = new List<ProviderSkill> { new() { ServiceId = 1 } } }
            };

            // Mock διαφορετικά scores για κάθε provider
            scoringServiceMock.Setup(s => s.CalculateProviderScore(
                It.Is<Provider>(p => p.Id == 1), It.IsAny<List<Certification>>()))
                .Returns(6.0m);  // Χαμηλότερο score

            scoringServiceMock.Setup(s => s.CalculateProviderScore(
                It.Is<Provider>(p => p.Id == 2), It.IsAny<List<Certification>>()))
                .Returns(8.0m);  // Μεσαίο score

            scoringServiceMock.Setup(s => s.CalculateProviderScore(
                It.Is<Provider>(p => p.Id == 3), It.IsAny<List<Certification>>()))
                .Returns(7.0m);  // Υψηλό score αλλά λιγότερα projects

            // ACT
            var results = service.FindTopProviders(request, providers);

            // ASSERT
            Assert.Equal(3, results.Count);
            Assert.Equal(2, results[0].Provider.Id);  // 1ος: Id 2 (υψηλότερο score)
            Assert.Equal(3, results[1].Provider.Id);  // 2ος: Id 3 
            Assert.Equal(1, results[2].Provider.Id);  // 3ος: Id 1
        }

        //  TEST 5: Test filtering by user capacity
        [Fact]
        public void FindTopProviders_UserCapacityFiltering_FiltersCorrectly()
        {
            // ARRANGE
            var request = new MatchingRequest
            {
                ServiceId = 1,
                NumberOfUsers = 50,  // Απαιτείται capacity 50 users
                Requestor = new Requestor { CostProfile = CostProfile.Low }
            };

            var providers = new List<Provider>
            {
                new Provider
                {
                    Id = 1,
                    EmployeeCount = 5,
                    ProviderSkills = new List<ProviderSkill>
                    {
                        new ProviderSkill
                        {
                            ServiceId = 1,
                            MaxUsersSupported = 30  // ΔΕΝ φτάνει
                        }
                    }
                },
                new Provider
                {
                    Id = 2,
                    EmployeeCount = 8,
                    ProviderSkills = new List<ProviderSkill>
                    {
                        new ProviderSkill
                        {
                            ServiceId = 1,
                            MaxUsersSupported = 60  // ΦΤΑΝΕΙ
                        }
                    }
                }
            };

            scoringServiceMock.Setup(s => s.CalculateProviderScore(
                It.IsAny<Provider>(), It.IsAny<List<Certification>>()))
                .Returns(5.0m);

            // ACT
            var results = service.FindTopProviders(request, providers);

            // ASSERT
            Assert.Single(results);  // Μόνο 1 provider
            Assert.Equal(2, results[0].Provider.Id);  // Μόνο ο 2ος έχει αρκετό capacity
        }

        // TEST 6: Test cost profile matching
        [Fact]
        public void MatchesCostProfile_VariousScenarios_ReturnsCorrectMatches()
        {
            // ARRANGE
            var service = new MatchingService(
                mockLogger.Object, scoringServiceMock.Object);

            // Χρησιμοποιούμε reflection για να καλέσουμε private method
            var method = typeof(MatchingService).GetMethod(
                "MatchesCostProfile",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            // ACT & ASSERT για διάφορα scenarios
            // Low cost profile
            var providerLow = new Provider { EmployeeCount = 5 };  // VerySmall
            var result1 = (bool)method.Invoke(service,
                new object[] { providerLow, CostProfile.Low });
            Assert.True(result1);

            // Medium cost profile
            var providerMedium = new Provider { EmployeeCount = 100 };  // SME
            var result2 = (bool)method.Invoke(service,
                new object[] { providerMedium, CostProfile.Medium });
            Assert.True(result2);

            // High cost profile
            var providerHigh = new Provider { EmployeeCount = 300 };  // Big
            var result3 = (bool)method.Invoke(service,
                new object[] { providerHigh, CostProfile.High });
            Assert.True(result3);

            // Non-matching
            var providerNonMatch = new Provider { EmployeeCount = 5 };  // VerySmall
            var result4 = (bool)method.Invoke(service,
                new object[] { providerNonMatch, CostProfile.High });
            Assert.False(result4);  // High + VerySmall = false
        }

        //  TEST 7: Test relaxed matching
        [Fact]
        public void FindTopProviders_StrictNoMatches_UsesRelaxedMatching()
        {
            // ARRANGE
            var request = new MatchingRequest
            {
                ServiceId = 1,
                Requestor = new Requestor
                {
                    CostProfile = CostProfile.High,
                    Location = "Thessaloniki"
                },
                LocationProximityRequired = true
            };

            // Provider που δεν ταιριάζει σε location (μόνο στο relaxed)
            var providers = new List<Provider>
            {
                new Provider
                {
                    Id = 1,
                    EmployeeCount = 300,  // Big (matches High)
                    Location = "Athens",  // ΔΙΑΦΟΡΕΤΙΚΟ location
                    ProviderSkills = new List<ProviderSkill>
                    {
                        new ProviderSkill { ServiceId = 1 }
                    }
                }
            };

            scoringServiceMock.Setup(s => s.CalculateProviderScore(
                It.IsAny<Provider>(), It.IsAny<List<Certification>>()))
                .Returns(5.0m);

            // ACT
            var results = service.FindTopProviders(request, providers);

            // ASSERT
            // Στο strict: 0 matches (λόγω location)
            // Στο relaxed: 1 match (παραλείπουμε location check)
            Assert.Single(results);
        }

    }
}
