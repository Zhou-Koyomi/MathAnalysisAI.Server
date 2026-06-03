using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MathAnalysisAI.Server.DTOs.Symbolic;

namespace MathAnalysisAI.Server.Services.Symbolic;

public sealed class SymPySymbolicMathService : ISymbolicMathService
{
    private const int HardMaxTimeoutMs = 15000;
    private static readonly JsonSerializerOptions WorkerJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SymPySymbolicMathService> _logger;

    public SymPySymbolicMathService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<SymPySymbolicMathService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<SymbolicComputeResponseDto> ComputeAsync(
        SymbolicComputeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.StartNew();

        if (!_configuration.GetValue("Symbolic:Enabled", true))
        {
            return Error(request, "symbolic_disabled", "Symbolic engine is disabled.", started.ElapsedMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(request.Operation))
        {
            return Error(request, "missing_required_field", "Field 'operation' is required.", started.ElapsedMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(request.Expression))
        {
            return Error(request, "missing_required_field", "Field 'expression' is required.", started.ElapsedMilliseconds);
        }

        var maxExpressionLength = Math.Max(1, _configuration.GetValue("Symbolic:MaxExpressionLength", 1000));
        if (request.Expression.Length > maxExpressionLength)
        {
            return Error(request, "expression_too_long", $"Expression exceeds max length {maxExpressionLength}.", started.ElapsedMilliseconds);
        }

        var timeoutMs = ResolveTimeoutMs(request.TimeoutMs);

        var pythonExecutable = _configuration["Symbolic:PythonExecutable"];
        if (string.IsNullOrWhiteSpace(pythonExecutable))
        {
            pythonExecutable = "python3";
        }

        var workerPath = ResolveWorkerPath(_configuration["Symbolic:WorkerPath"]);
        if (!File.Exists(workerPath))
        {
            _logger.LogWarning("Symbolic worker not found. WorkerPath={WorkerPath}", workerPath);
            return Error(request, "worker_unavailable", "Symbolic worker is unavailable.", started.ElapsedMilliseconds);
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.StartInfo.ArgumentList.Add(workerPath);

            if (!process.Start())
            {
                return Error(request, "worker_unavailable", "Failed to start symbolic worker.", started.ElapsedMilliseconds);
            }

            var payloadJson = JsonSerializer.Serialize(new
            {
                operation = request.Operation,
                expression = request.Expression,
                variable = request.Variable,
                lower = request.Lower,
                upper = request.Upper,
                point = request.Point,
                order = request.Order,
                assumptions = request.Assumptions,
                inputFormat = request.InputFormat,
                outputFormat = request.OutputFormat,
                timeoutMs = timeoutMs
            });

            await process.StandardInput.WriteAsync(payloadJson.AsMemory(), cancellationToken);
            process.StandardInput.Close();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var waitTask = process.WaitForExitAsync(cancellationToken);

            var completedTask = await Task.WhenAny(waitTask, Task.Delay(timeoutMs, cancellationToken));
            if (completedTask != waitTask)
            {
                TryKillProcess(process);
                _logger.LogWarning(
                    "Symbolic worker timeout. Operation={Operation}, ExpressionPreview={ExpressionPreview}, TimeoutMs={TimeoutMs}",
                    request.Operation,
                    Truncate(request.Expression),
                    timeoutMs);
                return Error(request, "timeout", "Symbolic computation timed out.", started.ElapsedMilliseconds);
            }

            await waitTask;
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                _logger.LogDebug(
                    "Symbolic worker stderr. Operation={Operation}, Message={Message}",
                    request.Operation,
                    Truncate(stderr, 400));
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                return Error(request, "worker_invalid_response", "Symbolic worker returned empty response.", started.ElapsedMilliseconds);
            }

            SymbolicComputeResponseDto? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<SymbolicComputeResponseDto>(stdout, WorkerJsonOptions);
            }
            catch (JsonException)
            {
                _logger.LogWarning(
                    "Symbolic worker returned invalid JSON. Operation={Operation}, OutputPreview={OutputPreview}",
                    request.Operation,
                    Truncate(stdout, 300));
                return Error(request, "worker_invalid_response", "Symbolic worker returned invalid response.", started.ElapsedMilliseconds);
            }

            if (parsed == null)
            {
                return Error(request, "worker_invalid_response", "Symbolic worker returned invalid response.", started.ElapsedMilliseconds);
            }

            if (parsed.ElapsedMs <= 0)
            {
                parsed.ElapsedMs = (int)started.ElapsedMilliseconds;
            }

            parsed.Operation ??= request.Operation;
            parsed.Input ??= request.Expression;
            parsed.Engine ??= "sympy";
            parsed.Warnings ??= new List<string>();

            return parsed;
        }
        catch (OperationCanceledException)
        {
            return Error(request, "timeout", "Symbolic computation timed out.", started.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Symbolic worker unavailable. Operation={Operation}, ExpressionPreview={ExpressionPreview}",
                request.Operation,
                Truncate(request.Expression));
            return Error(request, "worker_unavailable", "Symbolic worker is unavailable.", started.ElapsedMilliseconds);
        }
    }

    private int ResolveTimeoutMs(int? requestTimeoutMs)
    {
        var configured = _configuration.GetValue("Symbolic:TimeoutMs", 3000);
        if (configured <= 0)
        {
            configured = 3000;
        }

        var candidate = requestTimeoutMs.HasValue && requestTimeoutMs.Value > 0
            ? Math.Min(requestTimeoutMs.Value, configured)
            : configured;

        return Math.Min(candidate, HardMaxTimeoutMs);
    }

    private string ResolveWorkerPath(string? configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? "Tools/Symbolic/symbolic_worker.py"
            : configuredPath;

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, path));
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // ignore kill errors
        }
    }

    private static SymbolicComputeResponseDto Error(
        SymbolicComputeRequestDto request,
        string code,
        string message,
        long elapsedMs)
    {
        return new SymbolicComputeResponseDto
        {
            Success = false,
            Operation = request.Operation,
            Input = request.Expression,
            ResultText = null,
            ResultLatex = null,
            ResultJson = null,
            Engine = "sympy",
            EngineVersion = null,
            Warnings = new List<string>(),
            ErrorCode = code,
            ErrorMessage = message,
            ElapsedMs = (int)Math.Max(0, elapsedMs)
        };
    }

    private static string Truncate(string? value, int max = 120)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= max ? normalized : normalized[..max] + "...";
    }
}
