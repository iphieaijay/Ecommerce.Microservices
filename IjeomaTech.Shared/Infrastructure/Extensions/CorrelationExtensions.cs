using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Shared.Infrastructure.Middleware;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Extensions
{
    public static class CorrelationExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
    public static class CorrelationExtensions
    {
        public static string GetCorrelationId(this HttpContext context)
            => context.Items["X-Correlation-Id"]?.ToString()
               ?? context.TraceIdentifier;
    }


}
