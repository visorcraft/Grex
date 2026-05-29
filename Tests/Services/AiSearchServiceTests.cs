using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grex.Services;
using Xunit;

namespace Grex.Tests.Services
{
    public class AiSearchServiceTests
    {
        [Fact]
        public async Task SendDiscussionTurnAsync_WithPreferredModel_UsesProvidedModelAndSkipsModelDiscovery()
        {
            // Arrange
            var handler = new RecordingHttpMessageHandler(request =>
            {
                request.Method.Should().Be(HttpMethod.Post.Method);
                request.Uri.Should().Be("https://api.example.test/v1/chat/completions");

                return CreateJsonResponse(HttpStatusCode.OK, @"{
                    ""choices"": [
                        {
                            ""message"": {
                                ""content"": ""I found likely files to inspect.""
                            }
                        }
                    ]
                }");
            });
            var httpClient = new HttpClient(handler);
            var service = new AiSearchService(httpClient);

            var context = new AiSearchContext
            {
                SearchPath = @"C:\Repo",
                SearchQuery = "auth middleware",
                IsRegexSearch = false,
                IsFilesSearch = true,
                FilterSuggestions = new[] { "Match files: *.cs", "Exclude dirs: bin,obj" }
            };

            var conversation = new[]
            {
                new AiConversationTurn
                {
                    Role = "user",
                    Content = "Find where auth middleware is configured."
                }
            };

            // Act
            var response = await service.SendDiscussionTurnAsync(
                endpoint: "api.example.test/v1/",
                apiKey: " test-key ",
                preferredModel: "  custom-model  ",
                context: context,
                conversation: conversation);

            // Assert
            response.Success.Should().BeTrue();
            response.Message.Should().Be("I found likely files to inspect.");

            handler.Requests.Should().HaveCount(1);
            var request = handler.Requests.Single();
            request.Authorization.Should().Be("Bearer test-key");

            using var body = JsonDocument.Parse(request.BodyJson);
            var root = body.RootElement;
            root.GetProperty("model").GetString().Should().Be("custom-model");

            var messages = root.GetProperty("messages");
            messages.GetArrayLength().Should().Be(3);

            var contextPrompt = messages[1].GetProperty("content").GetString();
            contextPrompt.Should().NotBeNullOrWhiteSpace();
            contextPrompt.Should().Contain(@"Search path: C:\Repo");
            contextPrompt.Should().Contain("Search query: auth middleware");
            contextPrompt.Should().Contain("- Match files: *.cs");
        }

        [Fact]
        public async Task SendDiscussionTurnAsync_WithoutPreferredModel_ResolvesOnceAndCachesPerEndpoint()
        {
            // Arrange
            var modelsCalls = 0;
            var postedModels = new List<string>();

            var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.Method == HttpMethod.Get.Method)
                {
                    modelsCalls++;
                    request.Uri.Should().Be("https://model-cache.example.test/v1/models");

                    return CreateJsonResponse(HttpStatusCode.OK, @"{
                        ""data"": [
                            { ""id"": ""resolved-model-id"" }
                        ]
                    }");
                }

                request.Method.Should().Be(HttpMethod.Post.Method);
                using (var body = JsonDocument.Parse(request.BodyJson))
                {
                    postedModels.Add(body.RootElement.GetProperty("model").GetString() ?? string.Empty);
                }

                return CreateJsonResponse(HttpStatusCode.OK, @"{
                    ""choices"": [
                        { ""message"": { ""content"": ""ok"" } }
                    ]
                }");
            });

            var httpClient = new HttpClient(handler);
            var service = new AiSearchService(httpClient);
            var context = CreateContext();
            var conversation = CreateConversation();

            // Act
            var first = await service.SendDiscussionTurnAsync(
                endpoint: "model-cache.example.test",
                apiKey: null,
                preferredModel: null,
                context: context,
                conversation: conversation);

            var second = await service.SendDiscussionTurnAsync(
                endpoint: "model-cache.example.test",
                apiKey: null,
                preferredModel: "",
                context: context,
                conversation: conversation);

            // Assert
            first.Success.Should().BeTrue();
            second.Success.Should().BeTrue();
            modelsCalls.Should().Be(1);
            postedModels.Should().HaveCount(2);
            postedModels.Should().OnlyContain(model => model == "resolved-model-id");
        }

        [Fact]
        public async Task SendDiscussionTurnAsync_WhenModelDiscoveryFails_FallsBackToDefaultModel()
        {
            // Arrange
            var postedModel = string.Empty;
            var handler = new RecordingHttpMessageHandler(request =>
            {
                if (request.Method == HttpMethod.Get.Method)
                {
                    return CreateJsonResponse(HttpStatusCode.InternalServerError, @"{ ""error"": ""boom"" }");
                }

                using (var body = JsonDocument.Parse(request.BodyJson))
                {
                    postedModel = body.RootElement.GetProperty("model").GetString() ?? string.Empty;
                }

                return CreateJsonResponse(HttpStatusCode.OK, @"{
                    ""choices"": [
                        { ""message"": { ""content"": ""fallback used"" } }
                    ]
                }");
            });

            var service = new AiSearchService(new HttpClient(handler));

            // Act
            var response = await service.SendDiscussionTurnAsync(
                endpoint: "https://fallback-model.example.test/v1",
                apiKey: null,
                preferredModel: null,
                context: CreateContext(),
                conversation: CreateConversation());

            // Assert
            response.Success.Should().BeTrue();
            postedModel.Should().Be("gpt-4o-mini");
        }

        [Fact]
        public async Task SendDiscussionTurnAsync_WithMissingEndpoint_ReturnsValidationErrorAndSkipsHttpCall()
        {
            // Arrange
            var handler = new RecordingHttpMessageHandler(_ =>
                CreateJsonResponse(HttpStatusCode.OK, @"{ ""choices"": [ { ""message"": { ""content"": ""unused"" } } ] }"));
            var service = new AiSearchService(new HttpClient(handler));

            // Act
            var response = await service.SendDiscussionTurnAsync(
                endpoint: " ",
                apiKey: null,
                preferredModel: null,
                context: CreateContext(),
                conversation: CreateConversation());

            // Assert
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Contain("not configured");
            handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task SendDiscussionTurnAsync_WithApiError_UsesErrorMessageFromResponsePayload()
        {
            // Arrange
            var handler = new RecordingHttpMessageHandler(_ =>
                CreateJsonResponse(HttpStatusCode.BadRequest, @"{
                    ""error"": {
                        ""message"": ""Invalid API key.""
                    }
                }"));

            var service = new AiSearchService(new HttpClient(handler));

            // Act
            var response = await service.SendDiscussionTurnAsync(
                endpoint: "https://api-errors.example.test/v1",
                apiKey: "bad-key",
                preferredModel: "gpt-4o-mini",
                context: CreateContext(),
                conversation: CreateConversation());

            // Assert
            response.Success.Should().BeFalse();
            response.ErrorMessage.Should().Be("Invalid API key.");
        }

        private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private static AiSearchContext CreateContext()
        {
            return new AiSearchContext
            {
                SearchPath = @"C:\Repo",
                SearchQuery = "needle",
                IsRegexSearch = false,
                IsFilesSearch = false,
                FilterSuggestions = new[] { "Case-sensitive: Disabled" }
            };
        }

        private static IReadOnlyList<AiConversationTurn> CreateConversation()
        {
            return new[]
            {
                new AiConversationTurn
                {
                    Role = "user",
                    Content = "help me find references"
                }
            };
        }

        private sealed class RecordingHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<RecordedRequest, HttpResponseMessage> _responder;

            public RecordingHttpMessageHandler(Func<RecordedRequest, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var body = request.Content != null
                    ? await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
                    : string.Empty;

                var recorded = new RecordedRequest(
                    request.Method.Method,
                    request.RequestUri?.ToString() ?? string.Empty,
                    request.Headers.Authorization?.ToString(),
                    body);

                Requests.Add(recorded);
                return _responder(recorded);
            }
        }

        private sealed class RecordedRequest
        {
            public RecordedRequest(string method, string uri, string? authorization, string bodyJson)
            {
                Method = method;
                Uri = uri;
                Authorization = authorization;
                BodyJson = bodyJson;
            }

            public string Method { get; }
            public string Uri { get; }
            public string? Authorization { get; }
            public string BodyJson { get; }
        }
    }
}
