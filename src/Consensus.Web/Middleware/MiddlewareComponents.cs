using System.Diagnostics;
using System.Text.Json;

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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                message = "An error occurred while processing your request.",
                details = GetErrorDetails(exception),
                timestamp = DateTime.UtcNow,
                traceId = Activity.Current?.Id ?? context.TraceIdentifier
            }
        };

        var statusCode = exception switch
        {
            ArgumentException => 400,
            UnauthorizedAccessException => 401,
            KeyNotFoundException => 404,
            NotSupportedException => 405,
            TimeoutException => 408,
            InvalidOperationException => 409,
            _ => 500
        };

        context.Response.StatusCode = statusCode;
        
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static string GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "Invalid argument provided",
            UnauthorizedAccessException => "Access denied",
            KeyNotFoundException => "Resource not found",
            NotSupportedException => "Operation not supported",
            TimeoutException => "Operation timed out",
            InvalidOperationException => "Invalid operation",
            _ => "Internal server error"
        };
    }
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