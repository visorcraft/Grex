using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Grex.IntegrationTests
{
    [Collection("Integration SettingsOverride collection")]
    public class AiSearchLocalizationIntegrationTests
    {
        private static readonly IReadOnlyList<string> ExpectedKeys = new[]
        {
            "AppBarAiButton.Label",
            "AiSearchSettingsHeaderTextBlock.Text",
            "AiSearchSettingsDescriptionTextBlock.Text",
            "AiSearchEndpointLabelTextBlock.Text",
            "AiSearchEndpointTextBox.PlaceholderText",
            "AiSearchApiKeyLabelTextBlock.Text",
            "AiSearchApiKeyPasswordBox.PlaceholderText",
            "AiSearchModelLabelTextBlock.Text",
            "AiSearchModelTextBox.PlaceholderText",
            "TestAiEndpointButton.Content",
            "TestAiEndpointButtonTesting.Content",
            "Controls.SettingsView.TestAiEndpointButton.ToolTip",
            "AiEndpointTestSuccessTitle",
            "AiEndpointTestSuccessMessage",
            "AiEndpointTestErrorTitle",
            "AiEndpointTestErrorMessage",
            "AiEndpointTestEndpointRequiredMessage",
            "AiEndpointTestUnknownModel"
        };

        [Fact]
        public void AiSearchLocalizationKeys_ShouldExistInAllLanguages()
        {
            // Arrange
            var stringsDirectory = GetStringsDirectory();
            if (!Directory.Exists(stringsDirectory))
            {
                return;
            }

            // Act
            var languageDirectories = Directory.GetDirectories(stringsDirectory);

            // Assert
            foreach (var languageDirectory in languageDirectories)
            {
                var languageCode = Path.GetFileName(languageDirectory);
                var resourcePath = Path.Combine(languageDirectory, "Resources.resw");
                if (!File.Exists(resourcePath))
                {
                    continue;
                }

                foreach (var key in ExpectedKeys)
                {
                    ContainsResourceKey(resourcePath, key)
                        .Should()
                        .BeTrue($"Key '{key}' should exist in {languageCode}/Resources.resw");
                }
            }
        }

        [Fact]
        public void AiSearchEnglishStrings_ShouldDescribeEndpointTesting()
        {
            // Arrange
            var resourcePath = Path.Combine(GetStringsDirectory(), "en-US", "Resources.resw");
            if (!File.Exists(resourcePath))
            {
                return;
            }

            // Act
            var testButtonText = GetResourceValue(resourcePath, "TestAiEndpointButton.Content");
            var successTitle = GetResourceValue(resourcePath, "AiEndpointTestSuccessTitle");
            var errorTitle = GetResourceValue(resourcePath, "AiEndpointTestErrorTitle");

            // Assert
            testButtonText.Should().Be("Test Endpoint");
            successTitle.Should().Be("AI Endpoint Test Succeeded");
            errorTitle.Should().Be("AI Endpoint Test Failed");
        }

        private static string GetStringsDirectory()
        {
            var baseDirectory = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", ".."));
            return Path.Combine(projectRoot, "Strings");
        }

        private static bool ContainsResourceKey(string resourcePath, string key)
        {
            try
            {
                var document = XDocument.Load(resourcePath);
                foreach (var data in document.Descendants("data"))
                {
                    if (data.Attribute("name")?.Value == key)
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

        private static string? GetResourceValue(string resourcePath, string key)
        {
            try
            {
                var document = XDocument.Load(resourcePath);
                foreach (var data in document.Descendants("data"))
                {
                    if (data.Attribute("name")?.Value == key)
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
