using FluentAssertions;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class WindowsSubsystemLinuxServiceTests
    {
        [Theory]
        [InlineData(@"P:\home\user", true)]
        [InlineData(@"p:\HOME\user", true)]
        [InlineData(@"P:/home/user", true)]
        [InlineData(@"C:\Users\user", false)]
        [InlineData(@"\\wsl$\Ubuntu\home\user", false)]
        [InlineData("", false)]
        public void IsLikelyMountedWslPath_ReturnsExpected(string path, bool expected)
        {
            WindowsSubsystemLinuxService.IsLikelyMountedWslPath(path).Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData(@"\\wsl$\Ubuntu\home\user")]
        [InlineData(@"\\server\share\home\user")]
        public void TryConvertToNativeWslPath_WhenNotDrivePath_ReturnsOriginal(string path)
        {
            WindowsSubsystemLinuxService.TryConvertToNativeWslPath(path).Should().Be(path);
        }
    }
}

