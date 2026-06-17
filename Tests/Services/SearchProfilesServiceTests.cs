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

        private static SearchProfile CreateProfile(string name, string path = "C:\\Test", string term = "needle")
        {
            return new SearchProfile
            {
                Name = name,
                SearchPath = path,
                SearchTerm = term,
                IsRegexSearch = false,
                IsFilesSearch = false,
                RespectGitignore = true,
                SearchCaseSensitive = false,
                IncludeSystemFiles = false,
                IncludeSubfolders = true,
                IncludeHiddenItems = false,
                IncludeBinaryFiles = false,
                IncludeSymbolicLinks = false,
                UseWindowsSearchIndex = false,
                MatchFileNames = "*.cs",
                ExcludeDirs = "bin,obj",
                SizeLimitType = SizeLimitType.NoLimit,
                SizeLimitKB = null,
                SizeUnit = SizeUnit.KB,
                StringComparisonMode = StringComparisonMode.Ordinal,
                UnicodeNormalizationMode = UnicodeNormalizationMode.None,
                DiacriticSensitive = true,
                Culture = "en-US",
                CreatedAt = default,
                UpdatedAt = default
            };
        }

        [Fact]
        public void GetProfiles_WhenFileMissing_ReturnsEmptyList()
        {
            // The temp profiles file does not exist yet for a fresh test fixture.
            var profiles = SearchProfilesService.GetProfiles();

            profiles.Should().NotBeNull();
            profiles.Should().BeEmpty();
        }

        [Fact]
        public void AddOrUpdateProfile_WithValidProfile_AddsToTop()
        {
            var profile = CreateProfile("My Profile");
            SearchProfilesService.AddOrUpdateProfile(profile);

            var profiles = SearchProfilesService.GetProfiles();
            profiles.Should().HaveCount(1);
            profiles[0].Name.Should().Be("My Profile");
            profiles[0].CreatedAt.Should().NotBe(default);
            profiles[0].UpdatedAt.Should().NotBe(default);
        }

        [Fact]
        public void AddOrUpdateProfile_WithExistingName_UpdatesAndKeepsCreatedAt()
        {
            var first = CreateProfile("Profile", path: "C:\\First", term: "alpha");
            SearchProfilesService.AddOrUpdateProfile(first);

            var createdAt = SearchProfilesService.GetProfiles()[0].CreatedAt;

            var updated = CreateProfile("profile", path: "D:\\Second", term: "beta");
            SearchProfilesService.AddOrUpdateProfile(updated);

            var profiles = SearchProfilesService.GetProfiles();
            profiles.Should().HaveCount(1);
            profiles[0].Name.Should().Be("profile");
            profiles[0].SearchPath.Should().Be("D:\\Second");
            profiles[0].SearchTerm.Should().Be("beta");
            profiles[0].CreatedAt.Should().Be(createdAt);
            profiles[0].UpdatedAt.Should().BeOnOrAfter(createdAt);
        }

        [Fact]
        public void Exists_IsCaseInsensitive()
        {
            SearchProfilesService.AddOrUpdateProfile(CreateProfile("CaseTest"));

            SearchProfilesService.Exists("casetest").Should().BeTrue();
            SearchProfilesService.Exists("CASETEST").Should().BeTrue();
        }

        [Fact]
        public void DeleteProfile_RemovesProfile()
        {
            SearchProfilesService.AddOrUpdateProfile(CreateProfile("ToDelete"));

            SearchProfilesService.DeleteProfile("todelete");

            SearchProfilesService.GetProfiles().Should().BeEmpty();
        }

        [Fact]
        public void AddOrUpdateProfile_WithInvalidInput_DoesNotThrowOrAdd()
        {
            SearchProfilesService.AddOrUpdateProfile(null!);
            SearchProfilesService.AddOrUpdateProfile(new SearchProfile { Name = "" });

            SearchProfilesService.GetProfiles().Should().BeEmpty();
        }

        [Fact]
        public void SearchProfile_SecondaryText_FormatsAndTruncates()
        {
            var profile = new SearchProfile
            {
                Name = "Test",
                SearchPath = new string('p', 80),
                SearchTerm = new string('t', 80)
            };

            profile.SecondaryText.Should().Contain(" | ");
            profile.SecondaryText.Should().Contain("...");
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
