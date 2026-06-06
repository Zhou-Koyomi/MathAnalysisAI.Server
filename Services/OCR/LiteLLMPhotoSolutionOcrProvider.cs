using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MathAnalysisAI.Server.DTOs.PhotoSolutions;

namespace MathAnalysisAI.Server.Services.OCR
{
    public class LiteLLMPhotoSolutionOcrProvider : IPhotoSolutionOcrProvider
    {
        private const string ProviderName = "litellm";
        private const string DefaultAlias = "photo-solution-ocr";
        private const int DefaultMaxImageBytes = 10 * 1024 * 1024;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LiteLLMPhotoSolutionOcrProvider> _logger;

        public LiteLLMPhotoSolutionOcrProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<LiteLLMPhotoSolutionOcrProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PhotoSolutionOcrResponseDto> RecognizeAsync(
            PhotoSolutionOcrRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = BuildFallback();
            result.RawProvider = ProviderName;

            if (request.CourseId <= 0)
            {
                result.Warnings.Add("invalid_course_id");
                return result;
            }

            if (request.ImageBytes == null || request.ImageBytes.Length == 0)
            {
                result.Warnings.Add("empty_image_bytes");
                return result;
            }

            var maxImageBytes = _configuration.GetValue<int?>("PhotoSolutionOcr:MaxImageBytes") ?? DefaultMaxImageBytes;
            if (request.ImageBytes.Length > maxImageBytes)
            {
                result.Warnings.Add($"image_exceeds_limit:{maxImageBytes}");
                return result;
            }

            var baseUrl = _configuration["LiteLLM:BaseUrl"] ?? string.Empty;
            var apiKey = _configuration["LiteLLM:ApiKey"] ?? string.Empty;
            var modelAlias = _configuration["PhotoSolutionOcr:ModelAlias"] ?? DefaultAlias;
            result.ModelName = modelAlias;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                result.Warnings.Add("missing_litellm_base_url");
                return result;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                result.Warnings.Add("missing_litellm_api_key");
                return result;
            }

            if (!IsAscii(apiKey))
            {
                result.Warnings.Add("invalid_litellm_api_key_non_ascii");
                return result;
            }

            var contentType = string.IsNullOrWhiteSpace(request.ContentType)
                ? "image/png"
                : request.ContentType.Trim();
            var base64 = Convert.ToBase64String(request.ImageBytes);
            var dataUrl = $"data:{contentType};base64,{base64}";

            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(request);
            var payload = new
            {
                model = modelAlias,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = userPrompt },
                            new { type = "image_url", image_url = new { url = dataUrl } }
                        }
                    }
                }
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);
                var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    result.Warnings.Add($"provider_http_{(int)httpResponse.StatusCode}");
                    return result;
                }

                if (!TryExtractMessageContent(body, out var llmContent))
                {
                    result.Warnings.Add("provider_response_parse_failed");
                    return result;
                }

                if (!TryParseOcrJson(llmContent, out var parsed, out var parseWarning))
                {
                    result.Warnings.Add(parseWarning ?? "ocr_json_parse_failed");
                    return result;
                }

                parsed.RawProvider = ProviderName;
                parsed.ModelName = modelAlias;
                return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Photo solution OCR provider call failed.");
                result.Warnings.Add("provider_exception");
                return result;
            }
        }

        private static PhotoSolutionOcrResponseDto BuildFallback()
        {
            return new PhotoSolutionOcrResponseDto
            {
                ProblemText = string.Empty,
                StudentSolutionText = string.Empty,
                DetectedSections = new List<DetectedSectionDto>(),
                Formulas = new List<FormulaCandidateDto>(),
                Warnings = new List<string>(),
                Confidence = null,
                RawProvider = ProviderName,
                ModelName = null
            };
        }

        private static string BuildSystemPrompt()
        {
            return """
你是数学作业 OCR 转写助手。你只能做转写，不能做解题、批改、解释、扩写。
请严格输出一个合法 JSON object，不要输出 markdown/code fence/额外文字。
规则：
1) 只转写图像中实际出现内容；
2) 中文保持中文；
3) 数学公式输出 LaTeX；
4) 所有 LaTeX 反斜杠必须双重转义，例如 \\frac, \\int, \\alpha；
5) 必须做题目/解答分区：
   - problemText 只放题目原文与要求；
   - studentSolutionText 只放学生作答过程；
6) 若出现以下标志，通常归入 studentSolutionText：
   解、证明、设、因为、所以、因此、∴、计算步骤、方程组求解过程、矩阵化简过程；
7) 若题干与解答混在一起，尽量按语义分段：
   - 题号、问题描述、求什么 -> problemText
   - 推导、计算、证明过程 -> studentSolutionText
8) 若确实无法判断：
   - problemText 保留可识别题干；
   - studentSolutionText 可写 [unclear]；
   - warnings 中加入 section_split_uncertain；
9) detectedSections 的 type 仅使用：
   problem, student_solution, formula, unclear；
10) 无法识别写 [unclear]；
11) 输出字段必须包含：
problemText, studentSolutionText, detectedSections, formulas, warnings, confidence
""";
        }

        private static string BuildUserPrompt(PhotoSolutionOcrRequest request)
        {
            return $"""
请识别这张作业图片，并按 JSON 返回。
必须只输出合法 JSON object，不要输出解释文字或 Markdown。
若输出 LaTeX，请确保反斜杠做 JSON 双重转义，如 \\frac 或 \\int 或 \\alpha。
请重点把“题目”和“学生解答过程”分开：
- problemText 只保留题干/要求；
- studentSolutionText 只保留学生计算、推导、证明步骤。
如果分区不确定，请在 warnings 加 section_split_uncertain。
courseId: {request.CourseId}
chapterId: {(request.ChapterId?.ToString() ?? "null")}
fileName: {request.FileName}
contentType: {request.ContentType}
userHint: {request.UserHint ?? ""}
""";
        }

        private static bool TryExtractMessageContent(string body, out string content)
        {
            content = string.Empty;
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    return false;
                }

                var first = choices[0];
                if (!first.TryGetProperty("message", out var message))
                {
                    return false;
                }

                if (!message.TryGetProperty("content", out var contentNode))
                {
                    return false;
                }

                if (contentNode.ValueKind == JsonValueKind.String)
                {
                    content = contentNode.GetString() ?? string.Empty;
                }
                else if (contentNode.ValueKind == JsonValueKind.Array)
                {
                    // Some OpenAI-compatible vision providers return content blocks.
                    foreach (var block in contentNode.EnumerateArray())
                    {
                        if (block.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        if (block.TryGetProperty("type", out var typeNode)
                            && string.Equals(typeNode.GetString(), "text", StringComparison.OrdinalIgnoreCase)
                            && block.TryGetProperty("text", out var textNode)
                            && textNode.ValueKind == JsonValueKind.String)
                        {
                            content = textNode.GetString() ?? string.Empty;
                            break;
                        }
                    }
                }

                return !string.IsNullOrWhiteSpace(content);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseOcrJson(
            string content,
            out PhotoSolutionOcrResponseDto dto,
            out string? warning)
        {
            warning = null;
            dto = BuildFallback();
            try
            {
                var raw = ExtractJsonObject(StripCodeFence(content.Trim()));
                if (TryParseOcrJsonCore(raw, out dto))
                {
                    return true;
                }

                var repaired = EscapeInvalidJsonBackslashes(raw);
                if (TryParseOcrJsonCore(repaired, out dto))
                {
                    return true;
                }

                warning = "ocr_json_parse_failed";
                return false;
            }
            catch (Exception ex)
            {
                warning = $"ocr_json_parse_failed:{Truncate(ex.Message, 120)}";
                return false;
            }
        }

        private static bool TryParseOcrJsonCore(string rawJson, out PhotoSolutionOcrResponseDto dto)
        {
            dto = BuildFallback();
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                dto.ProblemText = ReadString(root, "problemText");
                dto.StudentSolutionText = ReadString(root, "studentSolutionText");
                dto.Confidence = ReadDecimalNullable(root, "confidence");
                dto.Warnings = ReadStringList(root, "warnings");
                dto.DetectedSections = ReadSections(root, "detectedSections");
                dto.Formulas = ReadFormulas(root, "formulas");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<DetectedSectionDto> ReadSections(JsonElement root, string propName)
        {
            var output = new List<DetectedSectionDto>();
            if (!root.TryGetProperty(propName, out var node) || node.ValueKind != JsonValueKind.Array)
            {
                return output;
            }

            foreach (var item in node.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                output.Add(new DetectedSectionDto
                {
                    Type = ReadString(item, "type", "unknown"),
                    Content = ReadString(item, "content")
                });
            }

            return output;
        }

        private static List<FormulaCandidateDto> ReadFormulas(JsonElement root, string propName)
        {
            var output = new List<FormulaCandidateDto>();
            if (!root.TryGetProperty(propName, out var node) || node.ValueKind != JsonValueKind.Array)
            {
                return output;
            }

            foreach (var item in node.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                output.Add(new FormulaCandidateDto
                {
                    Latex = ReadString(item, "latex"),
                    Context = ReadNullableString(item, "context")
                });
            }

            return output;
        }

        private static List<string> ReadStringList(JsonElement root, string propName)
        {
            var output = new List<string>();
            if (!root.TryGetProperty(propName, out var node) || node.ValueKind != JsonValueKind.Array)
            {
                return output;
            }

            foreach (var item in node.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        output.Add(s);
                    }
                }
            }

            return output;
        }

        private static string ReadString(JsonElement obj, string propName, string fallback = "")
        {
            if (!obj.TryGetProperty(propName, out var v))
            {
                return fallback;
            }

            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString() ?? fallback,
                JsonValueKind.Number => v.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => fallback
            };
        }

        private static string? ReadNullableString(JsonElement obj, string propName)
        {
            if (!obj.TryGetProperty(propName, out var v))
            {
                return null;
            }

            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static decimal? ReadDecimalNullable(JsonElement obj, string propName)
        {
            if (!obj.TryGetProperty(propName, out var v))
            {
                return null;
            }

            if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d))
            {
                return d;
            }

            if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var s))
            {
                return s;
            }

            return null;
        }

        private static bool IsAscii(string value) => value.All(c => c <= sbyte.MaxValue);

        private static string StripCodeFence(string input)
        {
            if (!input.StartsWith("```", StringComparison.Ordinal))
            {
                return input;
            }

            var lines = input.Split('\n').ToList();
            if (lines.Count > 0 && lines[0].StartsWith("```", StringComparison.Ordinal))
            {
                lines.RemoveAt(0);
            }

            if (lines.Count > 0 && lines[^1].Trim().StartsWith("```", StringComparison.Ordinal))
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join('\n', lines).Trim();
        }

        private static string ExtractJsonObject(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var start = input.IndexOf('{');
            var end = input.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                return input.Substring(start, end - start + 1).Trim();
            }

            return input.Trim();
        }

        private static string EscapeInvalidJsonBackslashes(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            var sb = new StringBuilder(json.Length + 32);
            var inString = false;
            var escaped = false;

            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];

                if (!inString)
                {
                    sb.Append(c);
                    if (c == '"')
                    {
                        inString = true;
                    }
                    continue;
                }

                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    if (i + 1 >= json.Length)
                    {
                        sb.Append("\\\\");
                        continue;
                    }

                    var next = json[i + 1];
                    if (IsValidJsonEscapeLeader(next))
                    {
                        sb.Append('\\');
                        escaped = true;
                    }
                    else
                    {
                        sb.Append("\\\\");
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = false;
                    sb.Append(c);
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static bool IsValidJsonEscapeLeader(char c)
        {
            return c is '"' or '\\' or '/' or 'b' or 'f' or 'n' or 'r' or 't' or 'u';
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= max ? value : value[..max];
        }
    }
}
