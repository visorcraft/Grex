using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Grex.Models;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class RecentSearchesServiceTests : IDisposable
    {
        private readonly List<RecentSearch> _originalSearches;

        public RecentSearchesServiceTests()
        {
            // Save original searches to restore after tests
            _originalSearches = new List<RecentSearch>(RecentSearchesService.GetRecentSearches());
        }

        public void Dispose()
        {
            // Clear current searches and restore originals
            RecentSearchesService.ClearHistory();
            foreach (var search in _originalSearches)
            {
                RecentSearchesService.AddRecentSearch(search);
            }
        }

        private static RecentSearch CreateTestSearch(string term, string path = "C:\\Test")
        {
            return new RecentSearch
            {
                SearchTerm = term,
                SearchPath = path,
                MatchFileNames = "*.cs",
                ExcludeDirs = "bin,obj",
                IsRegexSearch = false,
                IsFilesSearch = false,
                SearchCaseSensitive = false,
                RespectGitignore = true,
                IncludeSubfolders = true,
                IncludeHiddenItems = false,
                IncludeBinaryFiles = false,
                Timestamp = DateTime.Now,
                ResultCount = 42
            };
        }

        [Fact]
        public void GetRecentSearches_WithNoHistory_ReturnsEmptyList()
        {
            // Arrange
            RecentSearchesService.ClearHistory();

            // Act
            var searches = RecentSearchesService.GetRecentSearches();

            // Assert
            searches.Should().NotBeNull();
            searches.Should().BeEmpty();
        }

        [Fact]
        public void AddRecentSearch_WithValidSearch_AddsToHistory()
        {
            // Arrange
            RecentSearchesService.ClearHistory();
            var search = CreateTestSearch("test query");

            // Act
            RecentSearchesService.AddRecentSearch(search);
            var searches = RecentSearchesService.GetRecentSearches();

            // Assert
            searches.Should().HaveCount(1);
            searches[0].SearchTerm.Should().Be("test query");
            searches[0].SearchPath.Should().Be("C:\\Test");
        }

        [Fact]
        public void AddRecentSearch_WithDuplicateKey_MovesToTop()
        {
            // Arrange
            RecentSearchesService.ClearHistory();
            var search1 = CreateTestSearch("first query", "C:\\First");
            var search2 = CreateTestSearch("second query", "C:\\Second");
            var search1Updated = CreateTestSearch("first query", "C:\\First");
            search1Updated.ResultCount = 100; // Different result count

            // Act
            RecentSearchesService.AddRecentSearch(search1);
            RecentSearchesService.AddRecentSearch(search2);
            RecentSearchesService.AddRecentSearch(search1Updated);
            var searches = RecentSearchesService.GetRecentSearches();

            // Assert
            searches.Should().HaveCount(2);
            searches[0].SearchTerm.Should().Be("first query"); // Should be at top
            searches[0].ResultCount.Should().Be(100); // Should have updated count
            searches[1].SearchTerm.Should().Be("second query");
        }

        [Fact]
        public void AddRecentSearch_WithEmptySearchTerm_DoesNotAdd()
        {
            // Arrange
            RecentSearchesService.ClearHistory();
            var search = new RecentSearch { SearchTerm = "", SearchPath = "C:\\Test" };

            // Act
            RecentSearchesService.AddRecentSearch(search);
            var searches = RecentSearchesService.GetRecentSearches();

            // Assert
            searches.Should().BeEmpty();
        }

        [Fact]
        public void AddRecentSearch_WithNullSearch_DoesNotThrow()
        {
            // Act & Assert
            RecentSearchesService.AddRecentSearch(null!);
            // Should not throw
        }

        [Fact]
        public void RemoveRecentSearch_WithExistingSearch_RemovesFromHistory()
        {
            // Arrange
            RecentSearchesService.ClearHistory();
            var search1 = CreateTestSearch("query 1");
            var search2 = CreateTestSearch("query 2");
            RecentSearchesService.AddRecentSearch(search1);
            RecentSearchesService.AddRecentSearch(search2);

            // Act
            RecentSearchesService.RemoveRecentSearch(search1);
            var searches = RecentSearchesService.GetRecentSearches();

            // Assert
            searches.Should().HaveCount(1);
            searches[0].SearchTerm.Should().Be("query 2");
        }

        [Fact]
        public void ClearHistory_RemovesAllSearches()
        {
            // Arrange
            RecentSearchesService.AddRecentSearch(CreateTestSearch("query 1"));
            RecentSearchesService.AddRecentSearch(CreateTestSearch("query 2"));

            // Act
            RecentSearchesService.ClearHistory();
            var searches = RecentSearchesService.GetRecentSearches();

            // Assert
            searches.Should().BeEmpty();
        }

        [Fact]
        public void AddRecentSearch_ExceedsMaxSearches_KeepsOnlyMostRecent()
        {
            // Arrange
            RecentSearchesService.ClearHistory();
            for (int i = 0; i < 25; i++) // Add more than MaxRecentSearches (20)
            {
                var search = CreateTestSearch($"query {i}");
                RecentSearchesService.AddRecentSearch(search);
            }

            // Act
            var searches = RecentSearchesService.GetRecentSearches();

            // Assert
            searches.Should().HaveCount(20);
            searches[0].SearchTerm.Should().Be("query 24"); // Most recent
        }

        [Fact]
        public void FilterSearches_WithEmptySearchText_ReturnsAllSearches()
        {
            // Arrange
            RecentSearchesService.ClearHistory();
            RecentSearchesService.AddRecentSearch(CreateTestSearch("alpha"));
            RecentSearchesService.AddRecentSearch(CreateTestSearch("beta"));

            // Act
            var filtered = RecentSearchesService.FilterSearches("");

            // Assert
            filtered.Should().HaveCount(2);
        }

        [Fact]
        public void FilterSearches_WithMatchingText_ReturnsFilteredResults()
        {
            // Arrange
            RecentSearchesService.ClearHistory();
            RecentSearchesService.AddRecentSearch(CreateTestSearch("search for foo"));
            RecentSearchesService.AddRecentSearch(CreateTestSearch("bar query"));
            RecentSearchesService.AddRecentSearch(CreateTestSearch("another foo"));

            // Act
            var filtered = RecentSearchesService.FilterSearches("foo");

            // Assert
            filtered.Should().HaveCount(2);
            filtered.Should().OnlyContain(s => s.SearchTerm.Contains("foo"));
        }

        [Fact]
        public void FilterSearches_MatchesPath_ReturnsFilteredResults()
        {
            // Arrange
            RecentSearchesService.ClearHistory();
            RecentSearchesService.AddRecentSearch(CreateTestSearch("query 1", "C:\\Projects\\MyApp"));
            RecentSearchesService.AddRecentSearch(CreateTestSearch("query 2", "D:\\Other"));

            // Act
            var filtered = RecentSearchesService.FilterSearches("Projects");

            // Assert
            filtered.Should().HaveCount(1);
            filtered[0].SearchPath.Should().Contain("Projects");
        }

        [Fact]
        public void RecentSearch_DisplayText_FormatsCorrectly()
        {
            // Arrange
            var search = new RecentSearch
            {
                SearchTerm = "test query",
                IsRegexSearch = false,
                ResultCount = 42
            };

            // Act & Assert
            search.DisplayText.Should().Contain("test query");
            search.DisplayText.Should().Contain("42 results");
        }

        [Fact]
        public void RecentSearch_DisplayText_WithRegex_ShowsRegexIndicator()
        {
            // Arrange
            var search = new RecentSearch
            {
                SearchTerm = "test.*pattern",
                IsRegexSearch = true,
                ResultCount = 5
            };

            // Act & Assert
            search.DisplayText.Should().Contain("(Regex)");
            search.DisplayText.Should().Contain("5 results");
        }

        [Fact]
        public void RecentSearch_DisplayText_WithLongTerm_Truncates()
        {
            // Arrange
            var search = new RecentSearch
            {
                SearchTerm = new string('a', 50), // 50 characters
                IsRegexSearch = false,
                ResultCount = 1
            };

            // Act & Assert
            search.DisplayText.Should().Contain("...");
            search.DisplayText.Length.Should().BeLessThan(60);
        }

        [Fact]
        public void RecentSearch_SecondaryText_FormatsCorrectly()
        {
            // Arrange
            var search = new RecentSearch
            {
                SearchPath = "C:\\Short\\Path",
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 0)
            };

            // Act & Assert
            search.SecondaryText.Should().Contain("C:\\Short\\Path");
            search.SecondaryText.Should().Contain("|");
        }

        [Fact]
        public void RecentSearch_GetKey_CreatesUniqueKey()
        {
            // Arrange
            var search1 = new RecentSearch
            {
                SearchTerm = "query",
                SearchPath = "C:\\Path",
                IsRegexSearch = false,
                IsFilesSearch = false,
                SearchCaseSensitive = false,
                MatchFileNames = "*.cs",
                ExcludeDirs = "bin"
            };

            var search2 = new RecentSearch
            {
                SearchTerm = "query",
                SearchPath = "C:\\Path",
                IsRegexSearch = true, // Different
                IsFilesSearch = false,
                SearchCaseSensitive = false,
                MatchFileNames = "*.cs",
                ExcludeDirs = "bin"
            };

            // Act & Assert
            search1.GetKey().Should().NotBe(search2.GetKey());
        }

        [Fact]
        public void RecentSearch_GetKey_SameSettingsProduceSameKey()
        {
            // Arrange
            var search1 = CreateTestSearch("query", "C:\\Path");
            var search2 = CreateTestSearch("query", "C:\\Path");
            search2.ResultCount = 999; // Different result count should not affect key

            // Act & Assert
            search1.GetKey().Should().Be(search2.GetKey());
        }
    }
}
