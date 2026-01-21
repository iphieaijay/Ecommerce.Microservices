using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Extensions
{
    public static class SerilogExtensions
    {
        public static WebApplicationBuilder AddSerilogLogging(
            this WebApplicationBuilder builder)
        {
            var environment = builder.Environment.EnvironmentName;
            var serviceName = builder.Environment.ApplicationName;

            builder.Host.UseSerilog((context, services, loggerConfig) =>
            {
                loggerConfig
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .MinimumLevel.Information()

                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Service", serviceName)
                    .Enrich.WithProperty("Environment", environment)

                    .ReadFrom.Configuration(context.Configuration)

                    .WriteTo.Console(outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                        "[{Service}] " +
                        "[{CorrelationId}] " +
                        "{Message:lj}{NewLine}{Exception}");
            });

            return builder;
        }
    }
}
