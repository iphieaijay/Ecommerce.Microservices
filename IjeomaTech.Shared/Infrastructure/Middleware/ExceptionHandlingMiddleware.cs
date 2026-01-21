using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Middleware
{
    public sealed class ExceptionHandlingMiddleware(
     RequestDelegate next,
     ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception");

                var response = ApiResponse.Fail(
                    message: "An unexpected error occurred",
                    statusCode: StatusCodes.Status500InternalServerError,
                    errors: new[]
                    {
                    new ApiError("UNHANDLED", ex.Message)
                    }
                );

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }


}
