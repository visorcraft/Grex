using FluentAssertions;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Controls
{
    public class ExcludeDirsValidationTests
    {
        [Theory]
        [InlineData("^(**|resources)$", false)] // Nested quantifiers
        [InlineData("**", false)] // Nested quantifiers
        [InlineData("\\\\\\", false)] // Trailing unescaped backslash (3 backslashes = odd)
        [InlineData("AABB???", false)] // Nested quantifiers (???)
        [InlineData("AA(C(B)A", false)] // Missing group closure
        [InlineData("\\\\b[M]\\\\w+\\\\\\", false)] // Trailing unescaped backslash (3 backslashes = odd)
        [InlineData("^(test|resources)$", true)] // Valid Regex
        [InlineData("test,vendor", true)] // Comma-separated (not validated as Regex)
        [InlineData("", true)] // Empty string
        [InlineData("test", true)] // Simple text (not validated as Regex)
        public void IsValidRegexPattern_ShouldValidateCorrectly(string pattern, bool expected)
        {
            // Act
            var result = SearchService.IsValidRegexPattern(pattern);

            // Assert
            result.Should().Be(expected, $"Pattern '{pattern}' should be {(expected ? "valid" : "invalid")}");
        }
    }
}

