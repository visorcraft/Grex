using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class LogServiceTests : IDisposable
    {
        private readonly string _originalLogPath;
        private readonly string _testLogPath;

        public LogServiceTests()
        {
            _originalLogPath = Path.Combine(Path.GetTempPath(), "Grex.log");
            _testLogPath = Path.Combine(Path.GetTempPath(), $"GrexTest_{Guid.NewGuid()}.log");
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testLogPath))
                {
                    File.Delete(_testLogPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public void Write_CreatesLogFileWithMessage()
        {
            // Arrange
            var logFile = Path.Combine(Path.GetTempPath(), $"GrexTest_{Guid.NewGuid()}.log");
            try
            {
                // Act
                LogService.Write("Test message", logFile);

                // Assert
                File.Exists(logFile).Should().BeTrue();
                var content = File.ReadAllText(logFile);
                content.Should().Contain("Test message");
            }
            finally
            {
                try { File.Delete(logFile); } catch { }
            }
        }

        [Fact]
        public void Write_MultipleCalls_AppendMessages()
        {
            // Arrange
            var logFile = Path.Combine(Path.GetTempPath(), $"GrexTest_{Guid.NewGuid()}.log");
            try
            {
                // Act
                LogService.Write("First", logFile);
                LogService.Write("Second", logFile);

                // Assert
                var lines = File.ReadAllLines(logFile);
                lines.Length.Should().Be(2);
                lines[0].Should().Contain("First");
                lines[1].Should().Contain("Second");
            }
            finally
            {
                try { File.Delete(logFile); } catch { }
            }
        }

        [Fact]
        public void Write_ExceedsMaxSize_TrimsOldestEntries()
        {
            // Arrange
            var logFile = Path.Combine(Path.GetTempPath(), $"GrexTest_{Guid.NewGuid()}.log");
            try
            {
                // Write enough data to exceed the 1 MB cap.
                var payload = new string('a', 200);
                for (int i = 0; i < 6000; i++)
                {
                    LogService.Write($"{i:D6}: {payload}", logFile);
                }

                // Act
                var fileInfo = new FileInfo(logFile);

                // Assert
                fileInfo.Exists.Should().BeTrue();
                fileInfo.Length.Should().BeLessThan(1024 * 1024);

                var content = File.ReadAllText(logFile);
                content.Should().NotContain("000000:");
                content.Should().Contain("005999:");
            }
            finally
            {
                try { File.Delete(logFile); } catch { }
            }
        }
    }
}
