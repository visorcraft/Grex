using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Grex.Models;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class ExportServiceTests
    {
        private readonly ExportService _exportService = new ExportService();

        private static List<SearchResult> CreateTestContentResults()
        {
            return new List<SearchResult>
            {
                new SearchResult
                {
                    FileName = "file1.cs",
                    LineNumber = 10,
                    ColumnNumber = 5,
                    LineContent = "public void Test()",
                    FullPath = "C:\\Project\\file1.cs",
                    RelativePath = "file1.cs",
                    MatchCount = 1
                },
                new SearchResult
                {
                    FileName = "file2.cs",
                    LineNumber = 25,
                    ColumnNumber = 15,
                    LineContent = "private string value = \"test\";",
                    FullPath = "C:\\Project\\src\\file2.cs",
                    RelativePath = "src\\file2.cs",
                    MatchCount = 1
                }
            };
        }

        private static List<FileSearchResult> CreateTestFileResults()
        {
            return new List<FileSearchResult>
            {
                new FileSearchResult
                {
                    FileName = "file1.cs",
                    Size = 1024,
                    MatchCount = 5,
                    Extension = ".cs",
                    Encoding = "UTF-8",
                    DateModified = new DateTime(2024, 1, 15, 10, 30, 0),
                    FullPath = "C:\\Project\\file1.cs",
                    RelativePath = "file1.cs"
                },
                new FileSearchResult
                {
                    FileName = "file2.txt",
                    Size = 2048,
                    MatchCount = 3,
                    Extension = ".txt",
                    Encoding = "ASCII",
                    DateModified = new DateTime(2024, 2, 20, 14, 45, 0),
                    FullPath = "C:\\Project\\docs\\file2.txt",
                    RelativePath = "docs\\file2.txt"
                }
            };
        }

        #region CSV Export Tests

        [Fact]
        public void ExportContentResultsToCsv_WithValidResults_ReturnsValidCsv()
        {
            // Arrange
            var results = CreateTestContentResults();

            // Act
            var csv = _exportService.ExportContentResultsToCsv(results);

            // Assert
            csv.Should().NotBeNullOrEmpty();
            csv.Should().Contain("FileName,LineNumber,ColumnNumber,LineContent,FullPath,RelativePath");
            csv.Should().Contain("file1.cs");
            csv.Should().Contain("file2.cs");
            csv.Should().Contain("10,5");
            csv.Should().Contain("25,15");
        }

        [Fact]
        public void ExportContentResultsToCsv_WithEmptyResults_ReturnsHeaderOnly()
        {
            // Arrange
            var results = new List<SearchResult>();

            // Act
            var csv = _exportService.ExportContentResultsToCsv(results);

            // Assert
            csv.Should().NotBeNullOrEmpty();
            csv.Should().Contain("FileName,LineNumber,ColumnNumber,LineContent,FullPath,RelativePath");
            csv.Split(Environment.NewLine).Where(l => !string.IsNullOrEmpty(l)).Should().HaveCount(1);
        }

        [Fact]
        public void ExportFileResultsToCsv_WithValidResults_ReturnsValidCsv()
        {
            // Arrange
            var results = CreateTestFileResults();

            // Act
            var csv = _exportService.ExportFileResultsToCsv(results);

            // Assert
            csv.Should().NotBeNullOrEmpty();
            csv.Should().Contain("FileName,Size,MatchCount,Extension,Encoding,DateModified,FullPath,RelativePath");
            csv.Should().Contain("file1.cs");
            csv.Should().Contain("file2.txt");
            csv.Should().Contain("1024");
            csv.Should().Contain(".cs");
            csv.Should().Contain("UTF-8");
        }

        [Fact]
        public void ExportContentResultsToCsv_WithSpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var results = new List<SearchResult>
            {
                new SearchResult
                {
                    FileName = "file,with,commas.cs",
                    LineNumber = 1,
                    ColumnNumber = 1,
                    LineContent = "line with \"quotes\" and, commas",
                    FullPath = "C:\\Path\\file,with,commas.cs",
                    RelativePath = "file,with,commas.cs"
                }
            };

            // Act
            var csv = _exportService.ExportContentResultsToCsv(results);

            // Assert
            csv.Should().Contain("\"file,with,commas.cs\"");
            csv.Should().Contain("\"line with \"\"quotes\"\" and, commas\"");
        }

        #endregion

        #region JSON Export Tests

        [Fact]
        public void ExportContentResultsToJson_WithValidResults_ReturnsValidJson()
        {
            // Arrange
            var results = CreateTestContentResults();

            // Act
            var json = _exportService.ExportContentResultsToJson(results);

            // Assert
            json.Should().NotBeNullOrEmpty();

            // Verify it's valid JSON
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            parsed.ValueKind.Should().Be(JsonValueKind.Array);
            parsed.GetArrayLength().Should().Be(2);
        }

        [Fact]
        public void ExportContentResultsToJson_ContainsExpectedFields()
        {
            // Arrange
            var results = CreateTestContentResults();

            // Act
            var json = _exportService.ExportContentResultsToJson(results);

            // Assert
            json.Should().Contain("FileName");
            json.Should().Contain("LineNumber");
            json.Should().Contain("ColumnNumber");
            json.Should().Contain("LineContent");
            json.Should().Contain("FullPath");
            json.Should().Contain("RelativePath");
            json.Should().Contain("file1.cs");
        }

        [Fact]
        public void ExportFileResultsToJson_WithValidResults_ReturnsValidJson()
        {
            // Arrange
            var results = CreateTestFileResults();

            // Act
            var json = _exportService.ExportFileResultsToJson(results);

            // Assert
            json.Should().NotBeNullOrEmpty();

            // Verify it's valid JSON
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            parsed.ValueKind.Should().Be(JsonValueKind.Array);
            parsed.GetArrayLength().Should().Be(2);
        }

        [Fact]
        public void ExportFileResultsToJson_ContainsExpectedFields()
        {
            // Arrange
            var results = CreateTestFileResults();

            // Act
            var json = _exportService.ExportFileResultsToJson(results);

            // Assert
            json.Should().Contain("FileName");
            json.Should().Contain("Size");
            json.Should().Contain("MatchCount");
            json.Should().Contain("Extension");
            json.Should().Contain("Encoding");
            json.Should().Contain("FullPath");
            json.Should().Contain("RelativePath");
        }

        [Fact]
        public void ExportContentResultsToJson_WithEmptyResults_ReturnsEmptyArray()
        {
            // Arrange
            var results = new List<SearchResult>();

            // Act
            var json = _exportService.ExportContentResultsToJson(results);

            // Assert
            json.Should().Be("[]");
        }

        #endregion

        #region Clipboard Export Tests

        [Fact]
        public void ExportContentResultsToClipboard_WithValidResults_ReturnsTabSeparated()
        {
            // Arrange
            var results = CreateTestContentResults();

            // Act
            var clipboard = _exportService.ExportContentResultsToClipboard(results);

            // Assert
            clipboard.Should().NotBeNullOrEmpty();
            clipboard.Should().Contain("\t");
            clipboard.Should().Contain("FileName\tLine\tColumn\tContent\tPath");
            clipboard.Should().Contain("file1.cs\t10\t5");
        }

        [Fact]
        public void ExportFileResultsToClipboard_WithValidResults_ReturnsTabSeparated()
        {
            // Arrange
            var results = CreateTestFileResults();

            // Act
            var clipboard = _exportService.ExportFileResultsToClipboard(results);

            // Assert
            clipboard.Should().NotBeNullOrEmpty();
            clipboard.Should().Contain("\t");
            clipboard.Should().Contain("FileName\tSize\tMatches\tExtension\tEncoding\tDateModified\tPath");
            clipboard.Should().Contain("file1.cs");
        }

        [Fact]
        public void ExportContentResultsToClipboard_WithEmptyResults_ReturnsHeaderOnly()
        {
            // Arrange
            var results = new List<SearchResult>();

            // Act
            var clipboard = _exportService.ExportContentResultsToClipboard(results);

            // Assert
            clipboard.Should().NotBeNullOrEmpty();
            clipboard.Should().Contain("FileName\tLine\tColumn\tContent\tPath");
            clipboard.Split(Environment.NewLine).Where(l => !string.IsNullOrEmpty(l)).Should().HaveCount(1);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ExportContentResultsToCsv_WithNewlinesInContent_EscapesCorrectly()
        {
            // Arrange
            var results = new List<SearchResult>
            {
                new SearchResult
                {
                    FileName = "file.cs",
                    LineNumber = 1,
                    ColumnNumber = 1,
                    LineContent = "line with\nnewline",
                    FullPath = "C:\\file.cs",
                    RelativePath = "file.cs"
                }
            };

            // Act
            var csv = _exportService.ExportContentResultsToCsv(results);

            // Assert
            csv.Should().Contain("\"line with\nnewline\"");
        }

        [Fact]
        public void ExportContentResultsToJson_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var results = new List<SearchResult>
            {
                new SearchResult
                {
                    FileName = "file.cs",
                    LineNumber = 1,
                    ColumnNumber = 1,
                    LineContent = "string with \"quotes\" and \\backslashes",
                    FullPath = "C:\\file.cs",
                    RelativePath = "file.cs"
                }
            };

            // Act
            var json = _exportService.ExportContentResultsToJson(results);

            // Assert
            // JSON should be valid and parseable
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            parsed.ValueKind.Should().Be(JsonValueKind.Array);
        }

        [Fact]
        public void ExportContentResultsToCsv_WithNullContent_HandlesGracefully()
        {
            // Arrange
            var results = new List<SearchResult>
            {
                new SearchResult
                {
                    FileName = "file.cs",
                    LineNumber = 1,
                    ColumnNumber = 1,
                    LineContent = null!,
                    FullPath = "C:\\file.cs",
                    RelativePath = "file.cs"
                }
            };

            // Act
            var csv = _exportService.ExportContentResultsToCsv(results);

            // Assert
            csv.Should().NotBeNullOrEmpty();
            csv.Should().Contain("file.cs");
        }

        [Fact]
        public void ExportFileResultsToCsv_WithZeroMatchCount_HandlesCorrectly()
        {
            // Arrange
            var results = new List<FileSearchResult>
            {
                new FileSearchResult
                {
                    FileName = "file.cs",
                    Size = 0,
                    MatchCount = 0,
                    Extension = ".cs",
                    Encoding = "UTF-8",
                    DateModified = DateTime.Now,
                    FullPath = "C:\\file.cs",
                    RelativePath = "file.cs"
                }
            };

            // Act
            var csv = _exportService.ExportFileResultsToCsv(results);

            // Assert
            csv.Should().NotBeNullOrEmpty();
            csv.Should().Contain(",0,"); // Size and MatchCount
        }

        #endregion
    }
}
