using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Extensions
{
    public static class TelemetryExtensions
    {
        public static IServiceCollection AddTelemetry(this IServiceCollection services, string serviceName)
        {
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSource(serviceName)
                        .AddConsoleExporter();
                });

            return services;
        }
    }
}
