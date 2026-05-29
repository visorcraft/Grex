using System;
using System.Collections.Generic;
using FluentAssertions;
using Grex.Models;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class SearchProfilesServiceTests : IDisposable
    {
        private readonly List<SearchProfile> _originalProfiles;

        public SearchProfilesServiceTests()
        {
            _originalProfiles = new List<SearchProfile>(SearchProfilesService.GetProfiles());
        }

        public void Dispose()
        {
            SearchProfilesService.ClearProfiles();

            for (int i = _originalProfiles.Count - 1; i >= 0; i--)
            {
                SearchProfilesService.AddOrUpdateProfile(_originalProfiles[i]);
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
            SearchProfilesService.ClearProfiles();

            var profiles = SearchProfilesService.GetProfiles();

            profiles.Should().NotBeNull();
            profiles.Should().BeEmpty();
        }

        [Fact]
        public void AddOrUpdateProfile_WithValidProfile_AddsToTop()
        {
            SearchProfilesService.ClearProfiles();

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
            SearchProfilesService.ClearProfiles();

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
            profiles[0].UpdatedAt.Should().BeAfter(createdAt);
        }

        [Fact]
        public void Exists_IsCaseInsensitive()
        {
            SearchProfilesService.ClearProfiles();
            SearchProfilesService.AddOrUpdateProfile(CreateProfile("CaseTest"));

            SearchProfilesService.Exists("casetest").Should().BeTrue();
            SearchProfilesService.Exists("CASETEST").Should().BeTrue();
        }

        [Fact]
        public void DeleteProfile_RemovesProfile()
        {
            SearchProfilesService.ClearProfiles();
            SearchProfilesService.AddOrUpdateProfile(CreateProfile("ToDelete"));

            SearchProfilesService.DeleteProfile("todelete");

            SearchProfilesService.GetProfiles().Should().BeEmpty();
        }

        [Fact]
        public void AddOrUpdateProfile_WithInvalidInput_DoesNotThrowOrAdd()
        {
            SearchProfilesService.ClearProfiles();

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
    }
}

