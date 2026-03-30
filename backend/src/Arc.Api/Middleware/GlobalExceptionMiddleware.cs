using Serilog;
using System.Net;
using System.Text.Json;

namespace Arc.Api.Middleware
{
    /// <summary>
    /// Global exception handling middleware.
    /// Ensures all unhandled exceptions are returned in a consistent, deterministic format.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = Log.ForContext<GlobalExceptionMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unhandled exception occurred while processing request.");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = new
                {
                    Message = "An unexpected error occurred.",
                    Detail = ex.Message // safe for dev; can hide in production
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
            }
        }
    }

    public static class GlobalExceptionMiddlewareExtensions
    {
        public static WebApplication UseGlobalExceptionMiddleware(this WebApplication app)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
            return app;
        }
    }
}