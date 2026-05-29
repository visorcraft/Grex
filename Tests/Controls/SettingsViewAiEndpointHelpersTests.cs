using System;
using System.Reflection;
using FluentAssertions;
using Grex.Controls;
using Xunit;

namespace Grex.Tests.Controls
{
    public class SettingsViewAiEndpointHelpersTests
    {
        private static readonly Type SettingsViewType = typeof(SettingsView);

        [Theory]
        [InlineData("api.example.test", "https://api.example.test/v1/models")]
        [InlineData("https://api.example.test/v1", "https://api.example.test/v1/models")]
        [InlineData("https://api.example.test/v1/", "https://api.example.test/v1/models")]
        [InlineData("https://api.example.test/models", "https://api.example.test/models")]
        [InlineData("http://api.example.test/custom", "http://api.example.test/custom/v1/models")]
        public void BuildModelsEndpoint_ShouldNormalizeToExpectedModelsEndpoint(string endpoint, string expected)
        {
            // Act
            var result = InvokePrivateStatic<string>("BuildModelsEndpoint", endpoint);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractFirstModelId_ShouldReturnFirstNonEmptyTrimmedId()
        {
            // Arrange
            const string response = @"{
                ""data"": [
                    { ""id"": """" },
                    { ""id"": ""  first-model  "" },
                    { ""id"": ""second-model"" }
                ]
            }";

            // Act
            var modelId = InvokePrivateStatic<string>("ExtractFirstModelId", response);

            // Assert
            modelId.Should().Be("first-model");
        }

        [Fact]
        public void ExtractFirstModelId_WithInvalidJson_ShouldReturnEmptyString()
        {
            // Act
            var modelId = InvokePrivateStatic<string>("ExtractFirstModelId", "{ invalid");

            // Assert
            modelId.Should().BeEmpty();
        }

        [Fact]
        public void ExtractEndpointErrorMessage_ShouldPreferNestedErrorMessage()
        {
            // Arrange
            const string response = @"{ ""error"": { ""message"": ""Invalid API key."" } }";

            // Act
            var error = InvokePrivateStatic<string>("ExtractEndpointErrorMessage", response, "Bad Request");

            // Assert
            error.Should().Be("Invalid API key.");
        }

        [Fact]
        public void ExtractEndpointErrorMessage_ShouldSupportStringErrorPayloads()
        {
            // Arrange
            const string response = @"{ ""error"": ""rate limit exceeded"" }";

            // Act
            var error = InvokePrivateStatic<string>("ExtractEndpointErrorMessage", response, "Too Many Requests");

            // Assert
            error.Should().Be("rate limit exceeded");
        }

        [Fact]
        public void ExtractEndpointErrorMessage_WhenNoPayloadMessage_ShouldUseFallbackReason()
        {
            // Act
            var error = InvokePrivateStatic<string>("ExtractEndpointErrorMessage", "{}", "Unauthorized");

            // Assert
            error.Should().Be("Unauthorized");
        }

        [Fact]
        public void ExtractEndpointErrorMessage_WhenNoPayloadOrFallback_ShouldUseDefault()
        {
            // Act
            var error = InvokePrivateStatic<string>("ExtractEndpointErrorMessage", string.Empty, null);

            // Assert
            error.Should().Be("Request failed.");
        }

        private static T InvokePrivateStatic<T>(string methodName, params object?[] args)
        {
            var method = SettingsViewType.GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);

            method.Should().NotBeNull($"Expected private static method '{methodName}' to exist.");
            var result = method!.Invoke(null, args);
            result.Should().NotBeNull();
            return (T)result!;
        }
    }
}
