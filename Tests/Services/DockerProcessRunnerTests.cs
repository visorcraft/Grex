using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class DockerProcessRunnerTests
    {
        [Fact]
        public async Task RunAsync_WhenCancelled_KillsChildProcess()
        {
            // Arrange
            var runner = new DockerProcessRunner();
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c ping 127.0.0.1 -n 30 > nul",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var cts = new CancellationTokenSource();
            var runTask = runner.RunAsync(startInfo, cts.Token);

            // Act
            cts.CancelAfter(TimeSpan.FromMilliseconds(200));
            var sw = Stopwatch.StartNew();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
            sw.Stop();

            // Assert
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        }
    }
}
