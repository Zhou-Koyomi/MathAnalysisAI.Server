using System.Text.RegularExpressions;
using MathAnalysisAI.Server.DTOs.Visualization;

namespace MathAnalysisAI.Server.Services.Visualization
{
    public class GeoGebraCommandValidator : IGeoGebraCommandValidator
    {
        private const int MaxCommands = 20;
        private const int MaxCommandLength = 300;

        private static readonly string[] AllowedFunctionNames =
        {
            "Function", "Point", "Line", "Segment", "Vector",
            "Tangent", "Integral", "Derivative", "Sequence", "Slider"
        };

        private static readonly Regex AssignmentRegex = new(
            @"^\s*[A-Za-z][A-Za-z0-9_]*(\([A-Za-z][A-Za-z0-9_]*\))?\s*=.+$",
            RegexOptions.Compiled
        );

        private static readonly Regex FunctionCallRegex = new(
            @"^\s*([A-Za-z][A-Za-z0-9_]*)\s*\(.*\)\s*$",
            RegexOptions.Compiled
        );

        private static readonly Regex DangerousPatternRegex = new(
            @"(;|`|\|\||&&|<script|javascript:|onerror=|onload=|\$\(|\bexec\b|\bsystem\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        public GeoGebraValidationResultDto Validate(IEnumerable<string>? commands)
        {
            var result = new GeoGebraValidationResultDto();
            var source = commands?.ToList() ?? new List<string>();

            if (source.Count > MaxCommands)
            {
                result.Errors.Add($"Too many commands. Maximum allowed is {MaxCommands}.");
            }

            foreach (var raw in source)
            {
                var cmd = (raw ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(cmd))
                {
                    result.RejectedCommands.Add(raw ?? string.Empty);
                    result.Errors.Add("Empty command is not allowed.");
                    continue;
                }

                if (cmd.Length > MaxCommandLength)
                {
                    result.RejectedCommands.Add(raw ?? string.Empty);
                    result.Errors.Add($"Command exceeds max length {MaxCommandLength}: {cmd}");
                    continue;
                }

                if (DangerousPatternRegex.IsMatch(cmd))
                {
                    result.RejectedCommands.Add(raw ?? string.Empty);
                    result.Errors.Add($"Command contains dangerous or abnormal content: {cmd}");
                    continue;
                }

                if (IsAllowedAssignment(cmd) || IsAllowedFunctionCall(cmd))
                {
                    result.ValidCommands.Add(cmd);
                }
                else
                {
                    result.RejectedCommands.Add(raw ?? string.Empty);
                    result.Errors.Add($"Unsupported command: {cmd}");
                }
            }

            result.IsValid = result.Errors.Count == 0 && source.Count <= MaxCommands;
            return result;
        }

        private static bool IsAllowedAssignment(string cmd)
        {
            return AssignmentRegex.IsMatch(cmd);
        }

        private static bool IsAllowedFunctionCall(string cmd)
        {
            var match = FunctionCallRegex.Match(cmd);
            if (!match.Success)
            {
                return false;
            }

            var fn = match.Groups[1].Value;
            return AllowedFunctionNames.Contains(fn, StringComparer.Ordinal);
        }
    }
}
