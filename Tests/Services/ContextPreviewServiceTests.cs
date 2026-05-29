using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class ContextPreviewServiceTests : IDisposable
    {
        private readonly ContextPreviewService _service;
        private readonly string _tempDir;
        private readonly EncodingDetectionService _encodingService;

        public ContextPreviewServiceTests()
        {
            _encodingService = new EncodingDetectionService();
            _service = new ContextPreviewService(_encodingService);
            _tempDir = Path.Combine(Path.GetTempPath(), $"ContextPreviewTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_tempDir, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        [Fact]
        public async Task GetContextAsync_WithValidFile_ReturnsCorrectContext()
        {
            // Arrange
            var content = string.Join(Environment.NewLine, new[]
            {
                "Line 1",
                "Line 2",
                "Line 3",
                "Line 4",
                "Line 5 - Match",
                "Line 6",
                "Line 7",
                "Line 8",
                "Line 9",
                "Line 10"
            });
            var filePath = CreateTestFile("test.txt", content);

            // Act
            var result = await _service.GetContextAsync(filePath, lineNumber: 5, linesBefore: 2, linesAfter: 2);

            // Assert
            result.Should().NotBeNull();
            result.FileName.Should().Be("test.txt");
            result.FullPath.Should().Be(filePath);
            result.MatchLineNumber.Should().Be(5);
            result.Lines.Should().HaveCount(5);
            result.Lines[0].LineNumber.Should().Be(3);
            result.Lines[0].Content.Should().Be("Line 3");
            result.Lines[0].IsMatchLine.Should().BeFalse();
            result.Lines[2].LineNumber.Should().Be(5);
            result.Lines[2].Content.Should().Be("Line 5 - Match");
            result.Lines[2].IsMatchLine.Should().BeTrue();
            result.MatchLineIndex.Should().Be(2);
        }

        [Fact]
        public async Task GetContextAsync_WithFirstLine_ReturnsLimitedContext()
        {
            // Arrange
            var content = string.Join(Environment.NewLine, new[]
            {
                "First Line - Match",
                "Line 2",
                "Line 3",
                "Line 4",
                "Line 5"
            });
            var filePath = CreateTestFile("first.txt", content);

            // Act
            var result = await _service.GetContextAsync(filePath, lineNumber: 1, linesBefore: 3, linesAfter: 2);

            // Assert
            result.Should().NotBeNull();
            result.Lines.Should().HaveCount(3); // Only line 1, 2, 3 (no lines before 1)
            result.Lines[0].LineNumber.Should().Be(1);
            result.Lines[0].Content.Should().Be("First Line - Match");
            result.Lines[0].IsMatchLine.Should().BeTrue();
            result.MatchLineIndex.Should().Be(0);
        }

        [Fact]
        public async Task GetContextAsync_WithLastLine_ReturnsLimitedContext()
        {
            // Arrange
            var content = string.Join(Environment.NewLine, new[]
            {
                "Line 1",
                "Line 2",
                "Last Line - Match"
            });
            var filePath = CreateTestFile("last.txt", content);

            // Act
            var result = await _service.GetContextAsync(filePath, lineNumber: 3, linesBefore: 2, linesAfter: 5);

            // Assert
            result.Should().NotBeNull();
            result.Lines.Should().HaveCount(3); // Only lines 1, 2, 3 (no lines after 3)
            result.Lines[2].LineNumber.Should().Be(3);
            result.Lines[2].Content.Should().Be("Last Line - Match");
            result.Lines[2].IsMatchLine.Should().BeTrue();
            result.MatchLineIndex.Should().Be(2);
        }

        [Fact]
        public async Task GetContextAsync_WithDefaultContext_ReturnsFiveLinesBeforeAndAfter()
        {
            // Arrange
            var lines = new string[15];
            for (int i = 0; i < 15; i++)
            {
                lines[i] = $"Line {i + 1}";
            }
            var content = string.Join(Environment.NewLine, lines);
            var filePath = CreateTestFile("default.txt", content);

            // Act
            var result = await _service.GetContextAsync(filePath, lineNumber: 8);

            // Assert
            result.Should().NotBeNull();
            result.Lines.Should().HaveCount(11); // 5 before + 1 match + 5 after
            result.Lines[0].LineNumber.Should().Be(3);
            result.Lines[5].LineNumber.Should().Be(8);
            result.Lines[5].IsMatchLine.Should().BeTrue();
            result.Lines[10].LineNumber.Should().Be(13);
        }

        [Fact]
        public async Task GetContextAsync_WithEmptyFile_ReturnsEmptyLines()
        {
            // Arrange
            var filePath = CreateTestFile("empty.txt", "");

            // Act
            var result = await _service.GetContextAsync(filePath, lineNumber: 1);

            // Assert
            result.Should().NotBeNull();
            result.Lines.Should().BeEmpty();
        }

        [Fact]
        public async Task GetContextAsync_WithNullFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetContextAsync(null!, lineNumber: 1));
        }

        [Fact]
        public async Task GetContextAsync_WithInvalidLineNumber_ThrowsArgumentException()
        {
            // Arrange
            var filePath = CreateTestFile("test.txt", "content");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetContextAsync(filePath, lineNumber: 0));
        }

        [Fact]
        public async Task GetContextAsync_WithNonExistentFile_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDir, "nonexistent.txt");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetContextAsync(nonExistentPath, lineNumber: 1));
        }

        [Theory]
        [InlineData("\\\\wsl$\\Ubuntu\\home\\user\\file.txt", true)]
        [InlineData("\\\\wsl.localhost\\Ubuntu\\home\\user\\file.txt", true)]
        [InlineData("/mnt/c/Users/test", true)]
        [InlineData("/home/user/file.txt", true)]
        [InlineData("C:\\Users\\test\\file.txt", false)]
        [InlineData("D:\\Projects\\file.cs", false)]
        [InlineData("", false)]
        public void IsWslPath_ReturnsCorrectResult(string path, bool expected)
        {
            // Act
            var result = ContextPreviewService.IsWslPath(path);

            // Assert
            result.Should().Be(expected);
        }
    }
}
