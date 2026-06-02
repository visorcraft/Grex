using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Grex.IntegrationTests
{
    /// <summary>
    /// Integration tests for the Credits page assets.
    /// </summary>
    public class CreditsPageTests
    {
        private static string ProjectRoot()
        {
            var baseDir = AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        }

        [Fact]
        public void CreditsPage_XamlFile_Exists()
        {
            var xamlPath = Path.Combine(ProjectRoot(), "Controls", "CreditsView.xaml");
            File.Exists(xamlPath).Should().BeTrue("CreditsView.xaml should exist in Controls folder");
        }

        [Fact]
        public void CreditsPage_CodeBehindFile_Exists()
        {
            var csPath = Path.Combine(ProjectRoot(), "Controls", "CreditsView.xaml.cs");
            File.Exists(csPath).Should().BeTrue("CreditsView.xaml.cs should exist in Controls folder");
        }

        [Fact]
        public void CreditsPage_Manifest_Exists()
        {
            var jsonPath = Path.Combine(ProjectRoot(), "Assets", "third-party-licenses.json");
            File.Exists(jsonPath).Should().BeTrue("third-party-licenses.json should exist in Assets folder");
        }
    }
}
