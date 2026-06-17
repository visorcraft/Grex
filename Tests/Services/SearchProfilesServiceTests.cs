using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Grex.Models;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class SearchProfilesServiceTests : IDisposable
    {
        private readonly string _originalProfilesFile;
        private readonly string _testProfilesFile;

        public SearchProfilesServiceTests()
        {
            _originalProfilesFile = SearchProfilesService.ProfilesFilePath;
            _testProfilesFile = Path.Combine(Path.GetTempPath(), $"GrexProfilesTest_{Guid.NewGuid()}.json");
            SearchProfilesService.ProfilesFilePath = _testProfilesFile;
        }

        public void Dispose()
        {
            SearchProfilesService.ProfilesFilePath = _originalProfilesFile;
            try
            {
                if (File.Exists(_testProfilesFile))
                {
                    File.Delete(_testProfilesFile);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public void AddOrUpdateProfile_EnforcesMaxProfilesCap()
        {
            // Arrange
            for (int i = 0; i < 60; i++)
            {
                SearchProfilesService.AddOrUpdateProfile(new SearchProfile
                {
                    Name = $"Profile {i}",
                    SearchPath = $"C:\\Test{i}",
                    SearchTerm = "test"
                });
            }

            // Act
            var profiles = SearchProfilesService.GetProfiles();

            // Assert
            profiles.Count.Should().Be(50);
            profiles.First().Name.Should().Be("Profile 59");
            profiles.Last().Name.Should().Be("Profile 10");
        }
    }
}
