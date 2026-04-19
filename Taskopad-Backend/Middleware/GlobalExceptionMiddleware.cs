using System.Net;
using System.Text.Json;

namespace Taskopad_Backend.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var body = JsonSerializer.Serialize(new
                {
                    message = "An unexpected error occurred.",
                    detail = ex.Message
                });
                await ctx.Response.WriteAsync(body);
            }
        }
    }
}
