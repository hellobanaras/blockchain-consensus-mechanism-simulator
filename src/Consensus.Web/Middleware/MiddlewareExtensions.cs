namespace Consensus.Web.Middleware;

/// <summary>
/// Extension methods for configuring middleware in the application pipeline
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Add exception handling middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }

    /// <summary>
    /// Add request logging middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// Add security headers middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Add API validation middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseApiValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiValidationMiddleware>();
    }

    /// <summary>
    /// Add CORS middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseCustomCors(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorsMiddleware>();
    }

    /// <summary>
    /// Configure the complete middleware pipeline for the consensus simulator
    /// </summary>
    public static IApplicationBuilder UseConsensusSimulatorMiddleware(this IApplicationBuilder app, bool isDevelopment)
    {
        // Exception handling (should be first)
        app.UseExceptionHandling();

        // Security headers
        app.UseSecurityHeaders();

        // Request logging (for development)
        if (isDevelopment)
        {
            app.UseRequestLogging();
        }

        // CORS handling
        app.UseCustomCors();

        // API validation
        app.UseApiValidation();

        return app;
    }
}