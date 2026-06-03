using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.LLM;
using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Services.LLM
{
    public class LLMGateway
    {
        private const string GatewayModeDirect = "direct";
        private const string GatewayModeLiteLlm = "litellm";
        private const string ProviderDeepSeek = "deepseek";
        private const string ProviderLiteLlm = "litellm";
        private const string DefaultDeepSeekModel = "deepseek-chat";
        private const string DefaultLiteLlmAlias = "math-reviewer";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _db;

        public LLMGateway(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ApplicationDbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _db = db;
        }

        public async Task<LLMChatResponseDto> ChatAsync(LLMChatRequestDto request, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new LLMChatResponseDto { IsSuccess = false };

            var mode = (_configuration["LLMGateway:Mode"] ?? GatewayModeDirect).Trim().ToLowerInvariant();
            var provider = mode == GatewayModeLiteLlm ? ProviderLiteLlm : ProviderDeepSeek;
            var modelName = string.IsNullOrWhiteSpace(request.ModelName) ? DefaultDeepSeekModel : request.ModelName.Trim();
            var requestType = string.IsNullOrWhiteSpace(request.RequestType) ? "unknown" : request.RequestType.Trim();

            var status = "failed";
            string? errorCode = null;
            string? errorMessage = null;
            int? promptTokens = null;
            int? completionTokens = null;
            int? totalTokens = null;

            try
            {
                var messages = request.Messages
                    .Where(m => !string.IsNullOrWhiteSpace(m.Role) && !string.IsNullOrWhiteSpace(m.Content))
                    .Select(m => new { role = m.Role.Trim(), content = m.Content })
                    .ToArray();

                if (messages.Length == 0)
                {
                    errorCode = "empty_messages";
                    errorMessage = "Messages must contain at least one non-empty item.";
                    response.ErrorCode = errorCode;
                    response.ErrorMessage = errorMessage;
                    status = "failed";
                    return response;
                }

                string baseUrl;
                string apiKey;
                object payload;
                if (mode == GatewayModeLiteLlm)
                {
                    baseUrl = _configuration["LiteLLM:BaseUrl"] ?? string.Empty;
                    apiKey = _configuration["LiteLLM:ApiKey"] ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        errorCode = "missing_litellm_base_url";
                        errorMessage = "LiteLLM:BaseUrl is not configured.";
                        response.ErrorCode = errorCode;
                        response.ErrorMessage = errorMessage;
                        status = "failed";
                        return response;
                    }

                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        errorCode = "missing_litellm_api_key";
                        errorMessage = "LiteLLM API key is not configured. Set LiteLLM__ApiKey.";
                        response.ErrorCode = errorCode;
                        response.ErrorMessage = errorMessage;
                        status = "failed";
                        return response;
                    }

                    if (!IsAscii(apiKey))
                    {
                        errorCode = "invalid_litellm_api_key";
                        errorMessage = "LiteLLM API key contains non-ASCII characters.";
                        response.ErrorCode = errorCode;
                        response.ErrorMessage = errorMessage;
                        status = "failed";
                        return response;
                    }

                    modelName = ResolveLiteLlmAlias(requestType, request.ModelName);
                    payload = new { model = modelName, messages };
                }
                else
                {
                    baseUrl = _configuration["DeepSeek:BaseUrl"] ?? string.Empty;
                    apiKey = _configuration["DeepSeek:ApiKey"] ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        errorCode = "missing_base_url";
                        errorMessage = "DeepSeek:BaseUrl is not configured.";
                        response.ErrorCode = errorCode;
                        response.ErrorMessage = errorMessage;
                        status = "failed";
                        return response;
                    }

                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        errorCode = "missing_api_key";
                        errorMessage = "DeepSeek API key is not configured. Set DeepSeek__ApiKey.";
                        response.ErrorCode = errorCode;
                        response.ErrorMessage = errorMessage;
                        status = "failed";
                        return response;
                    }

                    if (!IsAscii(apiKey))
                    {
                        errorCode = "invalid_api_key";
                        errorMessage = "DeepSeek API key contains non-ASCII characters.";
                        response.ErrorCode = errorCode;
                        response.ErrorMessage = errorMessage;
                        status = "failed";
                        return response;
                    }

                    modelName = string.IsNullOrWhiteSpace(request.ModelName) ? DefaultDeepSeekModel : request.ModelName.Trim();
                    payload = new { model = modelName, messages };
                }

                var client = _httpClientFactory.CreateClient();
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);
                var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    errorCode = mode == GatewayModeLiteLlm
                        ? "litellm_request_failed"
                        : $"http_{(int)httpResponse.StatusCode}";
                    errorMessage = Truncate(responseBody, 1000);
                    response.ErrorCode = errorCode;
                    response.ErrorMessage = errorMessage;
                    status = "failed";
                    return response;
                }

                if (!TryExtractContentAndTokens(responseBody, out var content, out promptTokens, out completionTokens, out totalTokens))
                {
                    errorCode = mode == GatewayModeLiteLlm
                        ? "litellm_response_parse_failed"
                        : "response_parse_failed";
                    errorMessage = "Failed to parse LLM response.";
                    response.ErrorCode = errorCode;
                    response.ErrorMessage = errorMessage;
                    status = "failed";
                    return response;
                }

                response.IsSuccess = true;
                response.Content = content;
                response.PromptTokenCount = promptTokens;
                response.CompletionTokenCount = completionTokens;
                response.TotalTokenCount = totalTokens;
                status = "success";
                return response;
            }
            catch (OperationCanceledException)
            {
                errorCode = "request_canceled";
                errorMessage = "LLM request canceled.";
                response.ErrorCode = errorCode;
                response.ErrorMessage = errorMessage;
                status = "failed";
                return response;
            }
            catch (Exception ex)
            {
                errorCode = "gateway_exception";
                errorMessage = Truncate(ex.Message, 1000);
                response.ErrorCode = errorCode;
                response.ErrorMessage = errorMessage;
                status = "failed";
                return response;
            }
            finally
            {
                stopwatch.Stop();
                response.LatencyMs = stopwatch.ElapsedMilliseconds;

                var log = new LLMRequestLog
                {
                    Provider = provider,
                    ModelName = modelName,
                    RequestType = requestType,
                    PromptTokenCount = promptTokens,
                    CompletionTokenCount = completionTokens,
                    TotalTokenCount = totalTokens,
                    LatencyMs = (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds),
                    Status = status,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage,
                    UserId = request.UserId,
                    AnalysisResultId = request.AnalysisResultId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.LLMRequestLogs.Add(log);
                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    // Logging failures should not mask the original LLM response.
                }
            }
        }

        private string ResolveLiteLlmAlias(string requestType, string? requestedModelName)
        {
            var mapped = _configuration[$"LLMGateway:RequestTypeModelMap:{requestType}"];
            if (!string.IsNullOrWhiteSpace(mapped))
            {
                return mapped.Trim();
            }

            if (!string.IsNullOrWhiteSpace(requestedModelName))
            {
                return requestedModelName.Trim();
            }

            return DefaultLiteLlmAlias;
        }

        private static bool IsAscii(string value)
        {
            return value.All(c => c <= sbyte.MaxValue);
        }

        private static bool TryExtractContentAndTokens(
            string responseBody,
            out string? content,
            out int? promptTokens,
            out int? completionTokens,
            out int? totalTokens)
        {
            content = null;
            promptTokens = null;
            completionTokens = null;
            totalTokens = null;

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(responseBody);
            }
            catch
            {
                return false;
            }

            using (doc)
            {
                if (doc.RootElement.TryGetProperty("choices", out var choices)
                    && choices.ValueKind == JsonValueKind.Array
                    && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var message)
                        && message.TryGetProperty("content", out var contentElement)
                        && contentElement.ValueKind == JsonValueKind.String)
                    {
                        content = contentElement.GetString();
                    }
                }

                if (doc.RootElement.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var prompt)
                        && prompt.ValueKind == JsonValueKind.Number
                        && prompt.TryGetInt32(out var p))
                    {
                        promptTokens = p;
                    }

                    if (usage.TryGetProperty("completion_tokens", out var completion)
                        && completion.ValueKind == JsonValueKind.Number
                        && completion.TryGetInt32(out var c))
                    {
                        completionTokens = c;
                    }

                    if (usage.TryGetProperty("total_tokens", out var total)
                        && total.ValueKind == JsonValueKind.Number
                        && total.TryGetInt32(out var t))
                    {
                        totalTokens = t;
                    }
                }
            }

            return !string.IsNullOrWhiteSpace(content);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }
    }
}
