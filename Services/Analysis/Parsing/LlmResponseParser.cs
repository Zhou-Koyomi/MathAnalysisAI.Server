using System.Text.Json;
using System.Text.Json.Nodes;
using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.DTOs.Visualization;

namespace MathAnalysisAI.Server.Services.Analysis.Parsing
{
    public sealed class LlmResponseParser : ILlmResponseParser
    {
        public LlmParseResult Parse(string? rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return new LlmParseResult
                {
                    Success = false,
                    ErrorMessage = "Empty model content."
                };
            }

            var cleaned = StripCodeFence(rawContent.Trim());
            var normalized = NormalizeResponseJsonKeys(cleaned);
            var payloadToParse = normalized ?? cleaned;

            try
            {
                using var doc = JsonDocument.Parse(payloadToParse);
                var parsed = MapToAnalysisResponse(doc.RootElement);
                return new LlmParseResult
                {
                    Success = true,
                    Parsed = parsed,
                    NormalizedJson = payloadToParse
                };
            }
            catch (Exception ex)
            {
                return new LlmParseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    NormalizedJson = payloadToParse
                };
            }
        }

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

            return string.Join("\n", lines).Trim();
        }

        private static string? NormalizeResponseJsonKeys(string input)
        {
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(input);
            }
            catch
            {
                return null;
            }

            if (root is null)
            {
                return null;
            }

            NormalizeNodeKeys(root);
            return root.ToJsonString();
        }

        private static void NormalizeNodeKeys(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var pairs = obj.ToList();
                obj.Clear();

                foreach (var pair in pairs)
                {
                    var mappedKey = MapSnakeOrLegacyKey(pair.Key);
                    var value = pair.Value;
                    if (value != null)
                    {
                        NormalizeNodeKeys(value);
                    }

                    obj[mappedKey] = value;
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        NormalizeNodeKeys(item);
                    }
                }
            }
        }

        private static string MapSnakeOrLegacyKey(string key)
        {
            return key switch
            {
                "problem_type" => "problemType",
                "knowledge_points" => "knowledgePoints",
                "solution_overview" => "solutionOverview",
                "standard_solution" => "standardSolution",
                "student_solution_review" => "studentSolutionReview",
                "mistake_tags" => "mistakeTags",
                "review_suggestions" => "reviewSuggestions",
                "should_use" => "shouldUse",
                "visualization_type" => "visualizationType",
                "geogebra_commands" => "geoGebraCommands",
                "is_correct" => "isCorrect",
                "main_issue" => "mainIssue",
                "logic_gaps" => "logicGaps",
                _ => key
            };
        }

        private static AnalysisResponseDto MapToAnalysisResponse(JsonElement root)
        {
            var course = GetFlexibleString(root, "course");
            var chapter = GetFlexibleString(root, "chapter");
            var problemType = GetFlexibleString(root, "problemType");
            var difficulty = GetFlexibleString(root, "difficulty");
            var solutionOverview = GetFlexibleString(root, "solutionOverview");

            var knowledgePoints = ParseStringList(root, "knowledgePoints");
            var standardSolution = ParseStandardSolution(root, "standardSolution");
            var studentSolutionReview = ParseStudentSolutionReview(root, "studentSolutionReview");
            var mistakeTags = ParseStringList(root, "mistakeTags");
            var reviewSuggestions = ParseStringList(root, "reviewSuggestions");
            var visualization = ParseVisualization(root, "visualization");

            return new AnalysisResponseDto
            {
                Course = course ?? string.Empty,
                Chapter = chapter,
                ProblemType = problemType ?? string.Empty,
                Difficulty = difficulty ?? string.Empty,
                KnowledgePoints = knowledgePoints,
                SolutionOverview = solutionOverview ?? string.Empty,
                StandardSolution = standardSolution,
                StudentSolutionReview = studentSolutionReview,
                MistakeTags = mistakeTags,
                ReviewSuggestions = reviewSuggestions,
                Visualization = visualization
            };
        }

        private static List<StandardSolutionStepDto> ParseStandardSolution(JsonElement root, string property)
        {
            if (!TryGetProperty(root, property, out var element) || element.ValueKind == JsonValueKind.Null)
            {
                return new List<StandardSolutionStepDto>();
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                var list = new List<StandardSolutionStepDto>();
                var step = 1;
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        list.Add(new StandardSolutionStepDto
                        {
                            Step = GetFlexibleInt(item, "step") ?? step,
                            Title = GetFlexibleString(item, "title") ?? $"步骤{step}",
                            Content = GetFlexibleString(item, "content") ?? item.GetRawText()
                        });
                    }
                    else
                    {
                        list.Add(new StandardSolutionStepDto
                        {
                            Step = step,
                            Title = $"步骤{step}",
                            Content = ToReadableString(item)
                        });
                    }

                    step++;
                }

                return list;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return new List<StandardSolutionStepDto>
                {
                    new()
                    {
                        Step = 1,
                        Title = "标准解答",
                        Content = element.GetString() ?? string.Empty
                    }
                };
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                var list = new List<StandardSolutionStepDto>();
                if (TryGetProperty(element, "content", out var contentElement))
                {
                    list.Add(new StandardSolutionStepDto
                    {
                        Step = GetFlexibleInt(element, "step") ?? 1,
                        Title = GetFlexibleString(element, "title") ?? "标准解答",
                        Content = ToReadableString(contentElement)
                    });
                    return list;
                }

                var step = 1;
                foreach (var prop in element.EnumerateObject())
                {
                    list.Add(new StandardSolutionStepDto
                    {
                        Step = step++,
                        Title = prop.Name,
                        Content = ToReadableString(prop.Value)
                    });
                }

                if (list.Count == 0)
                {
                    list.Add(new StandardSolutionStepDto
                    {
                        Step = 1,
                        Title = "标准解答",
                        Content = element.GetRawText()
                    });
                }

                return list;
            }

            return new List<StandardSolutionStepDto>
            {
                new()
                {
                    Step = 1,
                    Title = "标准解答",
                    Content = ToReadableString(element)
                }
            };
        }

        private static StudentSolutionReviewDto ParseStudentSolutionReview(JsonElement root, string property)
        {
            if (!TryGetProperty(root, property, out var element) || element.ValueKind == JsonValueKind.Null)
            {
                return BuildDefaultStudentSolutionReview();
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return new StudentSolutionReviewDto
                {
                    IsCorrect = null,
                    MainIssue = element.GetString(),
                    LogicGaps = new List<string>(),
                    Suggestions = new List<string>()
                };
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                bool? isCorrect = null;
                if (TryGetProperty(element, "isCorrect", out var isCorrectElement))
                {
                    isCorrect = isCorrectElement.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Number => isCorrectElement.GetDouble() >= 0.8d,
                        JsonValueKind.String => ParseFlexibleCorrectness(isCorrectElement.GetString()),
                        _ => null
                    };
                }

                return new StudentSolutionReviewDto
                {
                    IsCorrect = isCorrect,
                    MainIssue = GetFlexibleString(element, "mainIssue"),
                    LogicGaps = ParseStringList(element, "logicGaps"),
                    Suggestions = ParseStringList(element, "suggestions")
                };
            }

            return BuildDefaultStudentSolutionReview();
        }

        private static VisualizationDto ParseVisualization(JsonElement root, string property)
        {
            if (!TryGetProperty(root, property, out var element) || element.ValueKind != JsonValueKind.Object)
            {
                return new VisualizationDto
                {
                    ShouldUse = false,
                    Engine = "none",
                    VisualizationType = "none",
                    GeoGebraCommands = new List<string>()
                };
            }

            var shouldUse = false;
            if (TryGetProperty(element, "shouldUse", out var shouldUseElement))
            {
                shouldUse = shouldUseElement.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => shouldUseElement.GetDouble() > 0d,
                    JsonValueKind.String => bool.TryParse(shouldUseElement.GetString(), out var b) && b,
                    _ => false
                };
            }

            return new VisualizationDto
            {
                ShouldUse = shouldUse,
                Engine = GetFlexibleString(element, "engine") ?? "none",
                VisualizationType = GetFlexibleString(element, "visualizationType") ?? "none",
                Reason = GetFlexibleString(element, "reason"),
                GeoGebraCommands = ParseStringList(element, "geoGebraCommands"),
                Caption = GetFlexibleString(element, "caption")
            };
        }

        private static List<string> ParseStringList(JsonElement root, string property)
        {
            if (!TryGetProperty(root, property, out var element) || element.ValueKind == JsonValueKind.Null)
            {
                return new List<string>();
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var single = element.GetString();
                return string.IsNullOrWhiteSpace(single) ? new List<string>() : new List<string> { single };
            }

            if (element.ValueKind != JsonValueKind.Array)
            {
                var value = ToReadableString(element);
                return string.IsNullOrWhiteSpace(value) ? new List<string>() : new List<string> { value };
            }

            var list = new List<string>();
            foreach (var item in element.EnumerateArray())
            {
                var value = ToReadableString(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    list.Add(value);
                }
            }

            return list;
        }

        private static string? GetFlexibleString(JsonElement root, string property)
        {
            if (!TryGetProperty(root, property, out var element) || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return ToReadableString(element);
        }

        private static int? GetFlexibleInt(JsonElement root, string property)
        {
            if (!TryGetProperty(root, property, out var element) || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var i))
            {
                return i;
            }

            if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static bool TryGetProperty(JsonElement root, string name, out JsonElement element)
        {
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        element = prop.Value;
                        return true;
                    }
                }
            }

            element = default;
            return false;
        }

        private static string ToReadableString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Object => element.GetRawText(),
                JsonValueKind.Array => element.GetRawText(),
                _ => string.Empty
            };
        }

        private static bool? ParseFlexibleCorrectness(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var text = value.Trim().ToLowerInvariant();
            if (text is "true" or "correct")
            {
                return true;
            }

            if (text is "false" or "incorrect" or "部分正确")
            {
                return false;
            }

            return bool.TryParse(text, out var parsed) ? parsed : null;
        }

        private static StudentSolutionReviewDto BuildDefaultStudentSolutionReview()
        {
            return new StudentSolutionReviewDto
            {
                IsCorrect = null,
                MainIssue = null,
                LogicGaps = new List<string>(),
                Suggestions = new List<string>()
            };
        }
    }
}
