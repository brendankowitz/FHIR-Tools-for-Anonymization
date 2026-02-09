using System;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Anonymizer.Core.Validation;
using Xunit;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests.Validation
{
    public class ReidentificationRiskAssessorTests
    {
        [Fact]
        public void GivenEquivalenceClasses_WhenAssessingRisk_ThenCalculatesProsecutorRisk()
        {
            // Arrange
            var assessor = new ReidentificationRiskAssessor();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>
            {
                new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>()
                },
                new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>()
                }
            };

            // Act
            var report = assessor.AssessRisk(equivalenceClasses);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(0.5, report.ProsecutorRisk, 2); // Max risk is 1/2
            Assert.True(report.ProsecutorRisk <= 1.0);
        }

        [Fact]
        public void GivenEquivalenceClasses_WhenAssessingRisk_ThenCalculatesJournalistRisk()
        {
            // Arrange
            var assessor = new ReidentificationRiskAssessor();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>
            {
                new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>()
                },
                new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>()
                }
            };

            // Act
            var report = assessor.AssessRisk(equivalenceClasses);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(0.375, report.JournalistRisk, 3); // Average of (1/2 + 1/4)
            Assert.True(report.JournalistRisk <= 1.0);
        }

        [Fact]
        public void GivenEquivalenceClasses_WhenAssessingRisk_ThenCalculatesUniquenessRatio()
        {
            // Arrange
            var assessor = new ReidentificationRiskAssessor();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>
            {
                new List<Dictionary<string, string>> { new Dictionary<string, string>() },
                new List<Dictionary<string, string>> { new Dictionary<string, string>(), new Dictionary<string, string>() },
                new List<Dictionary<string, string>> { new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>() }
            };

            // Act
            var report = assessor.AssessRisk(equivalenceClasses);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(1.0 / 3.0, report.UniquenessRatio, 2); // 1 unique group out of 3
        }

        [Fact]
        public void GivenHighRisk_WhenAssessingRisk_ThenClassifiesAsHigh()
        {
            // Arrange
            var assessor = new ReidentificationRiskAssessor();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>
            {
                new List<Dictionary<string, string>> { new Dictionary<string, string>() },
                new List<Dictionary<string, string>> { new Dictionary<string, string>() },
                new List<Dictionary<string, string>> { new Dictionary<string, string>() }
            };

            // Act
            var report = assessor.AssessRisk(equivalenceClasses);

            // Assert
            Assert.NotNull(report);
            Assert.Equal("High", report.RiskLevel);
        }

        [Fact]
        public void GivenLowRisk_WhenAssessingRisk_ThenClassifiesAsLow()
        {
            // Arrange
            var assessor = new ReidentificationRiskAssessor();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>();
            for (int i = 0; i < 10; i++)
            {
                var group = new List<Dictionary<string, string>>();
                for (int j = 0; j < 100; j++)
                {
                    group.Add(new Dictionary<string, string>());
                }
                equivalenceClasses.Add(group);
            }

            // Act
            var report = assessor.AssessRisk(equivalenceClasses);

            // Assert
            Assert.NotNull(report);
            Assert.Equal("Low", report.RiskLevel);
        }

        [Fact]
        public void GivenEmptyData_WhenAssessingRisk_ThenReturnsZeroRisk()
        {
            // Arrange
            var assessor = new ReidentificationRiskAssessor();
            var equivalenceClasses = new List<List<Dictionary<string, string>>>();

            // Act
            var report = assessor.AssessRisk(equivalenceClasses);

            // Assert
            Assert.NotNull(report);
            Assert.Equal(0.0, report.ProsecutorRisk);
            Assert.Equal(0.0, report.JournalistRisk);
            Assert.Equal(0.0, report.UniquenessRatio);
        }
    }
}
