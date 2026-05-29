using System;
using FluentAssertions;
using Grex.Services;
using Xunit;

namespace Grex.IntegrationTests
{
    [Collection("Integration SettingsOverride collection")]
    public class AiSearchSettingsIntegrationTests : IDisposable
    {
        public AiSearchSettingsIntegrationTests()
        {
            SettingsService.DeleteSettingsFile();
            SettingsService.InvalidateCache();
        }

        public void Dispose()
        {
            SettingsService.DeleteSettingsFile();
            SettingsService.InvalidateCache();
        }

        [Fact]
        public void GetDefaultSettings_ShouldIncludeAiSearchDefaults()
        {
            // Act
            var settings = SettingsService.GetDefaultSettings();

            // Assert
            settings.AiSearchEndpoint.Should().Be("https://api.openai.com/v1");
            settings.AiSearchApiKey.Should().BeEmpty();
            settings.AiSearchModel.Should().Be("gpt-4o-mini");
        }

        [Fact]
        public void SetAiSearchSettings_ShouldPersistAcrossCacheInvalidation()
        {
            // Arrange & Act
            SettingsService.SetAiSearchEndpoint("  https://llm.company.test/v1  ");
            SettingsService.SetAiSearchApiKey("  token-with-spaces  ");
            SettingsService.SetAiSearchModel("  my-model-1  ");

            SettingsService.InvalidateCache();
            var settings = SettingsService.GetDefaultSettings();

            // Assert
            settings.AiSearchEndpoint.Should().Be("https://llm.company.test/v1");
            settings.AiSearchApiKey.Should().Be("  token-with-spaces  ");
            settings.AiSearchModel.Should().Be("my-model-1");
        }

        [Fact]
        public void ExportImportAiSettings_RoundTrip_ShouldPreserveEndpointApiKeyAndModel()
        {
            // Arrange
            SettingsService.SetAiSearchEndpoint("https://ai.roundtrip.test/v1");
            SettingsService.SetAiSearchApiKey("rt-key");
            SettingsService.SetAiSearchModel("rt-model");
            var exported = SettingsService.ExportSettingsAsJson();

            // Act
            SettingsService.DeleteSettingsFile();
            var (success, error) = SettingsService.ImportSettingsFromJson(exported);
            SettingsService.InvalidateCache();
            var imported = SettingsService.GetDefaultSettings();

            // Assert
            success.Should().BeTrue();
            error.Should().BeNull();
            imported.AiSearchEndpoint.Should().Be("https://ai.roundtrip.test/v1");
            imported.AiSearchApiKey.Should().Be("rt-key");
            imported.AiSearchModel.Should().Be("rt-model");
        }
    }
}
