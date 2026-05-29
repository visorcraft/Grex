using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Grex.Services
{
    public sealed class AiSearchContext
    {
        public string SearchPath { get; init; } = string.Empty;
        public string SearchQuery { get; init; } = string.Empty;
        public IReadOnlyList<string> FilterSuggestions { get; init; } = Array.Empty<string>();
        public bool IsRegexSearch { get; init; }
        public bool IsFilesSearch { get; init; }
    }

    public sealed class AiConversationTurn
    {
        public string Role { get; init; } = "user";
        public string Content { get; init; } = string.Empty;
    }

    public sealed class AiSearchResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string ErrorMessage { get; init; } = string.Empty;
    }

    public sealed class AiSearchService
    {
        private const string DefaultModel = "gpt-4o-mini";

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private string? _resolvedEndpointBase;
        private string? _resolvedModel;

        public AiSearchService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(90)
            };
        }

        public async Task<AiSearchResponse> SendDiscussionTurnAsync(
            string endpoint,
            string? apiKey,
            string? preferredModel,
            AiSearchContext context,
            IReadOnlyList<AiConversationTurn> conversation,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return new AiSearchResponse
                {
                    Success = false,
                    ErrorMessage = "AI endpoint is not configured."
                };
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (conversation == null)
            {
                throw new ArgumentNullException(nameof(conversation));
            }

            try
            {
                var model = string.IsNullOrWhiteSpace(preferredModel)
                    ? await ResolveModelAsync(endpoint, apiKey, cancellationToken).ConfigureAwait(false)
                    : preferredModel.Trim();
                var requestUri = BuildChatCompletionsEndpoint(endpoint);

                var payload = new
                {
                    model,
                    temperature = 0.2,
                    messages = BuildMessages(context, conversation)
                };

                var payloadJson = JsonSerializer.Serialize(payload, _serializerOptions);

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new AiSearchResponse
                    {
                        Success = false,
                        ErrorMessage = ExtractErrorMessage(responseJson, response.ReasonPhrase)
                    };
                }

                var assistantMessage = ExtractAssistantMessage(responseJson);
                if (string.IsNullOrWhiteSpace(assistantMessage))
                {
                    return new AiSearchResponse
                    {
                        Success = false,
                        ErrorMessage = "AI endpoint returned an empty response."
                    };
                }

                return new AiSearchResponse
                {
                    Success = true,
                    Message = assistantMessage.Trim()
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log($"AiSearchService.SendDiscussionTurnAsync ERROR: {ex}");
                return new AiSearchResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> ResolveModelAsync(string endpoint, string? apiKey, CancellationToken cancellationToken)
        {
            var normalizedBase = NormalizeEndpointBase(endpoint);
            if (!string.Equals(normalizedBase, _resolvedEndpointBase, StringComparison.OrdinalIgnoreCase))
            {
                _resolvedEndpointBase = normalizedBase;
                _resolvedModel = null;
            }

            if (!string.IsNullOrWhiteSpace(_resolvedModel))
            {
                return _resolvedModel!;
            }

            try
            {
                var modelsUri = BuildModelsEndpoint(endpoint);
                using var request = new HttpRequestMessage(HttpMethod.Get, modelsUri);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _resolvedModel = DefaultModel;
                    return _resolvedModel;
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var document = JsonDocument.Parse(responseJson);

                if (document.RootElement.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var modelElement in dataElement.EnumerateArray())
                    {
                        if (modelElement.TryGetProperty("id", out var idElement) &&
                            idElement.ValueKind == JsonValueKind.String)
                        {
                            var id = idElement.GetString();
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                _resolvedModel = id.Trim();
                                return _resolvedModel;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"AiSearchService.ResolveModelAsync fallback to default model: {ex.Message}");
            }

            _resolvedModel = DefaultModel;
            return _resolvedModel;
        }

        private static List<object> BuildMessages(AiSearchContext context, IReadOnlyList<AiConversationTurn> conversation)
        {
            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = "You are Grex AI Search. Help the user locate relevant files and code using the provided path, query, and filter suggestions. Ask concise follow-up questions when needed."
                },
                new
                {
                    role = "system",
                    content = BuildContextPrompt(context)
                }
            };

            foreach (var turn in conversation)
            {
                var role = NormalizeRole(turn.Role);
                var content = turn.Content?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                messages.Add(new
                {
                    role,
                    content
                });
            }

            return messages;
        }

        private static string BuildContextPrompt(AiSearchContext context)
        {
            var builder = new StringBuilder();
            builder.AppendLine("AI search context:");
            builder.AppendLine($"Search path: {context.SearchPath}");
            builder.AppendLine($"Search query: {context.SearchQuery}");
            builder.AppendLine($"Search type: {(context.IsRegexSearch ? "Regex" : "Text")}");
            builder.AppendLine($"Result mode: {(context.IsFilesSearch ? "Files" : "Content lines")}");
            builder.AppendLine("Filter suggestions:");

            if (context.FilterSuggestions != null && context.FilterSuggestions.Count > 0)
            {
                foreach (var suggestion in context.FilterSuggestions)
                {
                    if (!string.IsNullOrWhiteSpace(suggestion))
                    {
                        builder.AppendLine($"- {suggestion}");
                    }
                }
            }
            else
            {
                builder.AppendLine("- No additional filters");
            }

            builder.AppendLine("Treat filters as suggestions and explain reasoning with concrete next steps.");
            return builder.ToString().Trim();
        }

        private static string NormalizeRole(string? role)
        {
            if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                return "assistant";
            }

            if (string.Equals(role, "system", StringComparison.OrdinalIgnoreCase))
            {
                return "system";
            }

            return "user";
        }

        private static string ExtractAssistantMessage(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return string.Empty;
            }

            try
            {
                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;

                if (root.TryGetProperty("choices", out var choicesElement) &&
                    choicesElement.ValueKind == JsonValueKind.Array &&
                    choicesElement.GetArrayLength() > 0)
                {
                    var firstChoice = choicesElement[0];

                    if (firstChoice.TryGetProperty("message", out var messageElement) &&
                        messageElement.ValueKind == JsonValueKind.Object &&
                        messageElement.TryGetProperty("content", out var contentElement))
                    {
                        return ExtractContentText(contentElement);
                    }

                    if (firstChoice.TryGetProperty("text", out var textElement) &&
                        textElement.ValueKind == JsonValueKind.String)
                    {
                        return textElement.GetString() ?? string.Empty;
                    }
                }

                if (root.TryGetProperty("output_text", out var outputTextElement) &&
                    outputTextElement.ValueKind == JsonValueKind.String)
                {
                    return outputTextElement.GetString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log($"AiSearchService.ExtractAssistantMessage parse error: {ex.Message}");
            }

            return string.Empty;
        }

        private static string ExtractContentText(JsonElement contentElement)
        {
            if (contentElement.ValueKind == JsonValueKind.String)
            {
                return contentElement.GetString() ?? string.Empty;
            }

            if (contentElement.ValueKind == JsonValueKind.Array)
            {
                var builder = new StringBuilder();
                foreach (var part in contentElement.EnumerateArray())
                {
                    if (part.ValueKind == JsonValueKind.String)
                    {
                        builder.AppendLine(part.GetString());
                        continue;
                    }

                    if (part.ValueKind == JsonValueKind.Object)
                    {
                        if (part.TryGetProperty("text", out var textElement) &&
                            textElement.ValueKind == JsonValueKind.String)
                        {
                            builder.AppendLine(textElement.GetString());
                        }
                        else if (part.TryGetProperty("content", out var nestedContentElement) &&
                                 nestedContentElement.ValueKind == JsonValueKind.String)
                        {
                            builder.AppendLine(nestedContentElement.GetString());
                        }
                    }
                }

                return builder.ToString().Trim();
            }

            return string.Empty;
        }

        private static string ExtractErrorMessage(string responseJson, string? fallbackReason)
        {
            if (!string.IsNullOrWhiteSpace(responseJson))
            {
                try
                {
                    using var document = JsonDocument.Parse(responseJson);
                    if (document.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        if (errorElement.ValueKind == JsonValueKind.Object &&
                            errorElement.TryGetProperty("message", out var messageElement) &&
                            messageElement.ValueKind == JsonValueKind.String)
                        {
                            var message = messageElement.GetString();
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                return message.Trim();
                            }
                        }

                        if (errorElement.ValueKind == JsonValueKind.String)
                        {
                            var errorText = errorElement.GetString();
                            if (!string.IsNullOrWhiteSpace(errorText))
                            {
                                return errorText.Trim();
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parse failures and return fallback
                }
            }

            return !string.IsNullOrWhiteSpace(fallbackReason)
                ? fallbackReason
                : "AI request failed.";
        }

        private static string BuildChatCompletionsEndpoint(string endpoint)
        {
            var normalized = NormalizeEndpointBase(endpoint);
            if (normalized.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            if (normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return $"{normalized}/chat/completions";
            }

            return $"{normalized}/v1/chat/completions";
        }

        private static string BuildModelsEndpoint(string endpoint)
        {
            var normalized = NormalizeEndpointBase(endpoint);
            if (normalized.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            if (normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return $"{normalized}/models";
            }

            return $"{normalized}/v1/models";
        }

        private static string NormalizeEndpointBase(string endpoint)
        {
            var trimmed = (endpoint ?? string.Empty).Trim();
            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = $"https://{trimmed}";
            }

            return trimmed.TrimEnd('/');
        }

        private static void Log(string message)
        {
            try
            {
                var logFile = Path.Combine(Path.GetTempPath(), "Grex.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(logFile, $"[{timestamp}] {message}\n");
            }
            catch
            {
                // Ignore logging failures
            }
        }
    }
}
