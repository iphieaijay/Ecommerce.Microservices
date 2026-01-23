using AuthService.Infrastructure.Services;
using System.Net;

namespace AuthService.API.Middleware
{
    /// <summary>
    /// Middleware for token validation and blacklist checking.
    /// </summary>
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        public TokenValidationMiddleware(
            RequestDelegate next,
            ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITokenBlacklistService tokenBlacklistService)
        {
            // Skip validation for auth endpoints and swagger
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/api/auth") || path.Contains("/swagger") || path.Contains("/health"))
            {
                await _next(context);
                return;
            }

            // Check if request has Authorization header
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var token = authHeader.ToString().Replace("Bearer ", "");

                if (!string.IsNullOrEmpty(token))
                {
                    // Check if token is revoked
                    if (await tokenBlacklistService.IsTokenRevokedAsync(token))
                    {
                        _logger.LogWarning("Blocked request with revoked token");

                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsJsonAsync(new
                        {
                            statusCode = 401,
                            message = "Token has been revoked. Please log in again.",
                            timestamp = DateTime.UtcNow
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

}
