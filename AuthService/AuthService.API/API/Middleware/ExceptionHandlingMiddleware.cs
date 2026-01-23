  using FluentValidation;
    using System.Net;
    using System.Text.Json;

    namespace AuthService.API.Middleware;

    /// <summary>
    /// Middleware for global exception handling.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next,ILogger<ExceptionHandlingMiddleware> logger)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = exception switch
            {
                ValidationException validationEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Validation failed",
                    Errors = validationEx.Errors.Select(e => new ErrorDetail
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    }).ToList()
                },
                KeyNotFoundException keyNotFoundEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = keyNotFoundEx.Message
                },
                UnauthorizedAccessException unauthorizedEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Message = unauthorizedEx.Message
                },
                InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("already") => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = invalidOpEx.Message
                },
                InvalidOperationException invalidOpEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = invalidOpEx.Message
                },
                ArgumentException argEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = argEx.Message
                },
                _ => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "An internal server error occurred. Please try again later.",
                    Details = context.Request.Host.Host.Contains("localhost")
                        ? exception.ToString()
                        : null
                }
            };

            response.StatusCode = errorResponse.StatusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            await response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
        }
    }

    
    

