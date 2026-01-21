using Microsoft.AspNetCore.Http;
using Shared.Contracts.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Extensions
{
    public static class HttpContextExtensions
    {
        public static string? GetCorrelationId(this HttpContext context)
            => context.Items.TryGetValue(CorrelationConstants.ItemKey, out var value)
                ? value?.ToString()
                : null;
    }
}
