using FluentAssertions;
using Grex.Cli.Options;
using Grex.Models;
using Grex.Services;
using Moq;
using Xunit;

namespace Grex.Cli.Tests;

public class CliSearchRunnerTests
{
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly CliSearchRunner _runner;

    public CliSearchRunnerTests()
    {
        _mockSearchService = new Mock<ISearchService>();
        _runner = new CliSearchRunner(_mockSearchService.Object);
    }

    [Fact]
    public async Task RunAsync_WithMatches_ReturnsZero()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            _mockSearchService
                .Setup(s => s.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<SizeLimitType>(),
                    It.IsAny<long?>(),
                    It.IsAny<SizeUnit>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<StringComparisonMode>(),
                    It.IsAny<UnicodeNormalizationMode>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchResult>
                {
                    new() { FileName = "test.cs", LineNumber = 1, LineContent = "match" }
                });

            var options = new SearchOptions
            {
                Path = tempDir,
                SearchTerm = "test",
                Quiet = true
            };

            // Act
            var exitCode = await _runner.RunAsync(options);

            // Assert
            exitCode.Should().Be(0);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task RunAsync_NoMatches_ReturnsOne()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            _mockSearchService
                .Setup(s => s.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<SizeLimitType>(),
                    It.IsAny<long?>(),
                    It.IsAny<SizeUnit>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<StringComparisonMode>(),
                    It.IsAny<UnicodeNormalizationMode>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchResult>());

            var options = new SearchOptions
            {
                Path = tempDir,
                SearchTerm = "notfound",
                Quiet = true
            };

            // Act
            var exitCode = await _runner.RunAsync(options);

            // Assert
            exitCode.Should().Be(1);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task RunAsync_InvalidPath_ReturnsTwo()
    {
        // Arrange
        var options = new SearchOptions
        {
            Path = "C:\\NonExistent\\Path\\That\\Does\\Not\\Exist",
            SearchTerm = "test",
            Quiet = true
        };

        // Act
        var exitCode = await _runner.RunAsync(options);

        // Assert
        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_CountMode_OutputsMatchCount()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            _mockSearchService
                .Setup(s => s.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<SizeLimitType>(),
                    It.IsAny<long?>(),
                    It.IsAny<SizeUnit>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<StringComparisonMode>(),
                    It.IsAny<UnicodeNormalizationMode>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchResult>
                {
                    new() { MatchCount = 3 },
                    new() { MatchCount = 2 }
                });

            var options = new SearchOptions
            {
                Path = tempDir,
                SearchTerm = "test",
                Count = true
            };

            // Capture console output
            var originalOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);

            try
            {
                // Act
                var exitCode = await _runner.RunAsync(options);

                // Assert
                exitCode.Should().Be(0);
                sw.ToString().Trim().Should().Be("5"); // 3 + 2 = 5
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task RunAsync_FilesOnlyMode_OutputsUniqueFilePaths()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            _mockSearchService
                .Setup(s => s.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<SizeLimitType>(),
                    It.IsAny<long?>(),
                    It.IsAny<SizeUnit>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<StringComparisonMode>(),
                    It.IsAny<UnicodeNormalizationMode>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchResult>
                {
                    new() { FullPath = "C:\\file1.cs", LineNumber = 1 },
                    new() { FullPath = "C:\\file1.cs", LineNumber = 5 }, // Same file
                    new() { FullPath = "C:\\file2.cs", LineNumber = 1 }
                });

            var options = new SearchOptions
            {
                Path = tempDir,
                SearchTerm = "test",
                FilesOnly = true
            };

            var originalOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);

            try
            {
                // Act
                var exitCode = await _runner.RunAsync(options);

                // Assert
                exitCode.Should().Be(0);
                var output = sw.ToString();
                output.Should().Contain("C:\\file1.cs");
                output.Should().Contain("C:\\file2.cs");
                // Should only have 2 lines (unique files)
                output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .Should().HaveCount(2);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task RunAsync_PassesOptionsToSearchService()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            _mockSearchService
                .Setup(s => s.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<SizeLimitType>(),
                    It.IsAny<long?>(),
                    It.IsAny<SizeUnit>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<StringComparisonMode>(),
                    It.IsAny<UnicodeNormalizationMode>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchResult>());

            var options = new SearchOptions
            {
                Path = tempDir,
                SearchTerm = "pattern",
                Regex = true,
                CaseSensitive = true,
                Gitignore = true,
                IncludeHidden = true,
                NoSubfolders = true,
                MatchFiles = "*.cs",
                ExcludeDirs = "bin;obj",
                Quiet = true
            };

            // Act
            await _runner.RunAsync(options);

            // Assert
            _mockSearchService.Verify(s => s.SearchAsync(
                It.IsAny<string>(),
                "pattern",
                true,  // isRegex
                true,  // respectGitignore
                true,  // searchCaseSensitive
                false, // includeSystemFiles
                false, // includeSubfolders (NoSubfolders = true)
                true,  // includeHiddenItems
                false, // includeBinaryFiles
                false, // includeSymbolicLinks
                SizeLimitType.LessThan, // default SizeLimitType is "less"
                null,
                SizeUnit.KB,
                "*.cs",
                "bin;obj",
                false,
                StringComparisonMode.Ordinal,
                UnicodeNormalizationMode.None,
                true,
                null,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
