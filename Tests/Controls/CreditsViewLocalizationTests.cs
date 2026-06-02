using System;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Grex.Tests.Controls
{
    /// <summary>
    /// Tests for Credits page localization keys.
    /// </summary>
    public class CreditsViewLocalizationTests
    {
        private readonly string _reswPath;

        public CreditsViewLocalizationTests()
        {
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            _reswPath = Path.Combine(projectRoot, "Strings", "en-US", "Resources.resw");
        }

        [Fact]
        public void Resources_ShouldContain_CreditsNavItemContent()
        {
            CheckResourceKeyExists("CreditsNavItem.Content")
                .Should().BeTrue("CreditsNavItem.Content should exist in Resources.resw");
        }

        [Fact]
        public void Resources_ShouldContain_CreditsHeadingText()
        {
            CheckResourceKeyExists("CreditsHeadingTextBlock.Text")
                .Should().BeTrue("CreditsHeadingTextBlock.Text should exist in Resources.resw");
        }

        [Fact]
        public void Resources_ShouldContain_CreditsIntroText()
        {
            CheckResourceKeyExists("CreditsIntroTextBlock.Text")
                .Should().BeTrue("CreditsIntroTextBlock.Text should exist in Resources.resw");
        }

        [Fact]
        public void CreditsNavItem_ShouldEqual_Credits()
        {
            GetResourceValue("CreditsNavItem.Content").Should().Be("Credits");
        }

        [Fact]
        public void CreditsHeading_ShouldContain_Licenses()
        {
            var value = GetResourceValue("CreditsHeadingTextBlock.Text");
            value.Should().NotBeNull();
            value.Should().Contain("Licenses", "Credits heading should mention Licenses");
        }

        [Fact]
        public void CreditsIntro_ShouldNotBeEmpty()
        {
            GetResourceValue("CreditsIntroTextBlock.Text").Should().NotBeNullOrWhiteSpace();
        }

        private bool CheckResourceKeyExists(string key)
        {
            if (!File.Exists(_reswPath))
            {
                return false;
            }

            try
            {
                var doc = XDocument.Load(_reswPath);
                foreach (var data in doc.Descendants("data"))
                {
                    var nameAttr = data.Attribute("name");
                    if (nameAttr != null && nameAttr.Value == key)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private string? GetResourceValue(string key)
        {
            if (!File.Exists(_reswPath))
            {
                return null;
            }

            try
            {
                var doc = XDocument.Load(_reswPath);
                foreach (var data in doc.Descendants("data"))
                {
                    var nameAttr = data.Attribute("name");
                    if (nameAttr != null && nameAttr.Value == key)
                    {
                        return data.Element("value")?.Value;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
