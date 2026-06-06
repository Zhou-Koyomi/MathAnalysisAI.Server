using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.Data.Seed;
using MathAnalysisAI.Server.Services.Analysis;
using MathAnalysisAI.Server.Services.Analysis.Fallback;
using MathAnalysisAI.Server.Services.Analysis.Context;
using MathAnalysisAI.Server.Services.Analysis.Mistakes;
using MathAnalysisAI.Server.Services.Analysis.LLM;
using MathAnalysisAI.Server.Services.Analysis.Parsing;
using MathAnalysisAI.Server.Services.Analysis.Persistence;
using MathAnalysisAI.Server.Services.Analysis.Stats;
using MathAnalysisAI.Server.Services.Auth;
using MathAnalysisAI.Server.Services.Materials;
using MathAnalysisAI.Server.Services.Knowledge;
using MathAnalysisAI.Server.Services.LLM;
using MathAnalysisAI.Server.Services.OCR;
using MathAnalysisAI.Server.Services.Ranking;
using MathAnalysisAI.Server.Services.Security;
using MathAnalysisAI.Server.Services.Symbolic;
using MathAnalysisAI.Server.Services.Visualization;
using MathAnalysisAI.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

ValidateAuthConfiguration(builder.Environment, authOptions);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Controllers
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".MathAnalysisAI.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(12);
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100L * 1024L * 1024L;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 100L * 1024L * 1024L;
});

// Forwarded Headers — trust Nginx/load-balancer proxy headers when behind reverse proxy
// Required for correct HTTPS detection, client IP, and OAuth redirect_uri generation.
// Deployment note: if Nginx runs on Docker host (not in compose), add the Docker bridge
// network (typically 172.16.0.0/12) to ForwardedHeaders__KnownNetworks in server.env.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // Clear ASP.NET Core defaults; only trust explicitly configured sources
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    // Always trust loopback (Nginx on same machine connects via 127.0.0.1)
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("127.0.0.0"), 8));

    // Optional: trust Docker bridge via config (comma-separated CIDRs, e.g. "172.16.0.0/12")
    var dockerNetworks = builder.Configuration["ForwardedHeaders:KnownNetworks"];
    if (!string.IsNullOrWhiteSpace(dockerNetworks))
    {
        foreach (var cidr in dockerNetworks.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = cidr.Trim();
            if (Microsoft.AspNetCore.HttpOverrides.IPNetwork.TryParse(trimmed, out var network))
            {
                options.KnownNetworks.Add(network);
            }
        }
    }

    // Optional: trust specific proxy IPs via config (comma-separated, e.g. "172.17.0.1")
    var proxyIps = builder.Configuration["ForwardedHeaders:KnownProxies"];
    if (!string.IsNullOrWhiteSpace(proxyIps))
    {
        foreach (var ip in proxyIps.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = ip.Trim();
            if (IPAddress.TryParse(trimmed, out var addr))
            {
                options.KnownProxies.Add(addr);
            }
        }
    }
});

// Rate Limiting (memory-based, single-instance; multi-instance needs Redis/Gateway)
builder.Services.AddRateLimiter(options =>
{
    // Global fallback — very permissive, acts as safety net only
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var message = System.Text.Json.JsonSerializer.Serialize(new
        {
            message = "请求过于频繁，请稍后重试。",
            retryAfter = 30
        });
        await context.HttpContext.Response.WriteAsync(message, cancellationToken);
    };

    // Partition key helper: prefer session userId, fall back to client IP
    string GetPartitionKey(HttpContext ctx)
    {
        var userId = ctx.Session.GetInt32("auth_user_id");
        if (userId.HasValue && userId.Value > 0)
            return $"user:{userId.Value}";

        return $"ip:{ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    // login: per-IP, 5 requests/minute
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"login:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3
            }));

    // analyze: per-user (fallback IP), 3 requests/minute
    options.AddPolicy("analyze", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"analyze:{GetPartitionKey(context)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    // ocr: per-user (fallback IP), 2 requests/minute
    options.AddPolicy("ocr", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ocr:{GetPartitionKey(context)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            }));

    // symbolic: per-user (fallback IP), 10 requests/minute
    options.AddPolicy("symbolic", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"symbolic:{GetPartitionKey(context)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HTTP + Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IUserContext, CurrentUserService>();
builder.Services.AddScoped<LLMGateway>();
builder.Services.AddScoped<AnalysisService>();
builder.Services.AddScoped<ILlmResponseParser, LlmResponseParser>();
builder.Services.AddScoped<IAnalysisContextBuilder, AnalysisContextBuilder>();
builder.Services.AddScoped<IAnalysisFallbackService, AnalysisFallbackService>();
builder.Services.AddScoped<ILlmRequestFactory, LlmRequestFactory>();
builder.Services.AddScoped<IAnalysisPersistenceService, AnalysisPersistenceService>();
builder.Services.AddScoped<IMistakeRecordService, MistakeRecordService>();
builder.Services.AddScoped<IUserStatsUpdateService, UserStatsUpdateService>();
builder.Services.AddScoped<VisualizationService>();
builder.Services.AddScoped<LeaderboardService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddSingleton<IGeoGebraCommandValidator, GeoGebraCommandValidator>();
builder.Services.AddScoped<CourseMaterialStorageService>();
builder.Services.AddScoped<PdfTextExtractionService>();
builder.Services.AddScoped<MaterialChunkingService>();
builder.Services.AddScoped<CourseMaterialIngestionService>();
builder.Services.AddScoped<IPhotoSolutionOcrProvider, LiteLLMPhotoSolutionOcrProvider>();
builder.Services.AddScoped<IKnowledgeRetrievalService, KnowledgeRetrievalService>();
builder.Services.AddScoped<ISymbolicMathService, SymPySymbolicMathService>();

var app = builder.Build();

// Runtime PromptProfile seeding (idempotent, safe to skip when DB/tables are unavailable)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inserted = await PromptProfileSeeder.SeedAsync(db);
        logger.LogInformation("PromptProfile seeding completed. Inserted: {InsertedCount}", inserted);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "PromptProfile seeding skipped due to database initialization state.");
    }

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var testUserId = await AppUserSeeder.SeedDevelopmentTestStudentAsync(db);
        logger.LogInformation("Development test user seeding completed. UserId: {UserId}", testUserId);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Development test user seeding skipped due to database initialization state.");
    }
}

app.UseForwardedHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseAuthorization();
app.UseEndpoints(endpoints => endpoints.MapControllers());
app.Run();

static void ValidateAuthConfiguration(IHostEnvironment environment, AuthOptions authOptions)
{
    var mode = authOptions.GetNormalizedMode();
    var validModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        AuthOptions.ModeDevelopmentUsername,
        AuthOptions.ModeLocalPassword,
        AuthOptions.ModeOidc,
        AuthOptions.ModeDisabled
    };

    if (string.IsNullOrWhiteSpace(mode))
    {
        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Unsafe auth configuration for Production: Auth:Mode must be explicitly set to LocalPassword, Oidc, or Disabled.");
        }

        return;
    }

    if (!validModes.Contains(mode))
    {
        throw new InvalidOperationException(
            $"Invalid auth configuration: Auth:Mode '{mode}' is not supported. Allowed values: {AuthOptions.ModeDevelopmentUsername}, {AuthOptions.ModeLocalPassword}, {AuthOptions.ModeOidc}, {AuthOptions.ModeDisabled}.");
    }

    if (!environment.IsProduction())
    {
        return;
    }

    var violations = new List<string>();
    if (string.Equals(mode, AuthOptions.ModeDevelopmentUsername, StringComparison.OrdinalIgnoreCase))
    {
        violations.Add("Auth:Mode=DevelopmentUsername");
    }

    if (authOptions.EnableDevelopmentFallback)
    {
        violations.Add("Auth:EnableDevelopmentFallback=true");
    }

    if (authOptions.EnableDevelopmentMaterialAccessOverride)
    {
        violations.Add("Auth:EnableDevelopmentMaterialAccessOverride=true");
    }

    if (authOptions.EnableDevelopmentSymbolicAccessOverride)
    {
        violations.Add("Auth:EnableDevelopmentSymbolicAccessOverride=true");
    }

    if (violations.Count > 0)
    {
        throw new InvalidOperationException(
            $"Unsafe auth configuration for Production: {string.Join(", ", violations)}. Development-only auth settings must be disabled before startup.");
    }
}
