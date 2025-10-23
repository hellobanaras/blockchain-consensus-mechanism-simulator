using System.Diagnostics;
using System.Text.Json;
using Consensus.Web.Exceptions;

namespace Consensus.Web.Middleware;

/// <summary>
/// Middleware for handling global exceptions and providing consistent error responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred. RequestPath: {RequestPath}, UserId: {UserId}",
                context.Request.Path,
                context.User?.Identity?.Name ?? "Anonymous");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, problemDetails) = exception switch
        {
            SimulationException simEx => (400, new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Simulation Error",
                Status = 400,
                Detail = simEx.Message,
                Instance = context.Request.Path,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Timestamp = DateTime.UtcNow,
                Properties = new Dictionary<string, object>
                {
                    ["simulationId"] = simEx.SimulationId ?? "unknown",
                    ["operationType"] = simEx.OperationType ?? "unknown",
                    ["context"] = simEx.Context ?? new Dictionary<string, object>()
                }
            }),
            ConsensusException consEx => (422, new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Title = "Consensus Error",
                Status = 422,
                Detail = consEx.Message,
                Instance = context.Request.Path,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Timestamp = DateTime.UtcNow,
                Properties = new Dictionary<string, object>
                {
                    ["protocol"] = consEx.Protocol ?? "unknown",
                    ["roundNumber"] = consEx.RoundNumber.ToString(),
                    ["nodeId"] = consEx.NodeId ?? "unknown"
                }
            }),
            ValidationException valEx => (400, new ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Error",
                Status = 400,
                Detail = valEx.Message,
                Instance = context.Request.Path,
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
                Timestamp = DateTime.UtcNow,
                Properties = new Dictionary<string, object>
                {
                    ["validationErrors"] = valEx.ValidationErrors ?? new List<string>()
                }
            }),
            ArgumentException => (400, CreateStandardErrorResponse(context, exception, 400, "Bad Request")),
            UnauthorizedAccessException => (401, CreateStandardErrorResponse(context, exception, 401, "Unauthorized")),
            KeyNotFoundException => (404, CreateStandardErrorResponse(context, exception, 404, "Not Found")),
            NotSupportedException => (405, CreateStandardErrorResponse(context, exception, 405, "Method Not Allowed")),
            TimeoutException => (408, CreateStandardErrorResponse(context, exception, 408, "Request Timeout")),
            InvalidOperationException => (409, CreateStandardErrorResponse(context, exception, 409, "Conflict")),
            _ => (500, CreateStandardErrorResponse(context, exception, 500, "Internal Server Error"))
        };

        context.Response.StatusCode = statusCode;

        var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static ErrorResponse CreateStandardErrorResponse(HttpContext context, Exception exception, int statusCode, string title)
    {
        return new ErrorResponse
        {
            Type = GetProblemTypeUri(statusCode),
            Title = title,
            Status = statusCode,
            Detail = exception.Message,
            Instance = context.Request.Path,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };
    }

    private static string GetProblemTypeUri(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            405 => "https://tools.ietf.org/html/rfc7231#section-6.5.5",
            408 => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}

/// <summary>
/// RFC 7807 Problem Details response model
/// </summary>
public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

/// <summary>
/// Middleware for logging HTTP requests and responses
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Log request
        _logger.LogInformation("HTTP {Method} {Path} started",
            context.Request.Method,
            context.Request.Path);

        // Enable buffering to read request body
        context.Request.EnableBuffering();

        await _next(context);

        stopwatch.Stop();

        // Log response
        _logger.LogInformation("HTTP {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds,
            context.Response.StatusCode);
    }
}

/// <summary>
/// Middleware for adding security headers
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Add CSP header for development (adjust for production)
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; connect-src 'self' ws: wss:";

        await _next(context);
    }
}

/// <summary>
/// Middleware for validating API keys and rate limiting
/// </summary>
public class ApiValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiValidationMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public ApiValidationMiddleware(RequestDelegate next, ILogger<ApiValidationMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for certain paths
        if (ShouldSkipValidation(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Validate API key for API endpoints
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
            var validApiKey = _configuration["ApiSettings:ApiKey"];

            if (string.IsNullOrEmpty(validApiKey))
            {
                // API key validation disabled in development
                _logger.LogWarning("API key validation is disabled - no API key configured");
            }
            else if (string.IsNullOrEmpty(apiKey) || apiKey != validApiKey)
            {
                _logger.LogWarning("Invalid or missing API key for {Path}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid or missing API key");
                return;
            }
        }

        await _next(context);
    }

    private static bool ShouldSkipValidation(PathString path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/metrics",
            "/favicon.ico",
            "/_blazor",
            "/_framework",
            "/simulationHub"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
    }
}

/// <summary>
/// Middleware for handling CORS preflight requests
/// </summary>
public class CorsMiddleware
{
    private readonly RequestDelegate _next;

    public CorsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Handle preflight requests
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-API-Key";
            context.Response.StatusCode = 200;
            return;
        }

        await _next(context);
    }
}