
    using System.Net;
    using System.Text.Json;
    using FluentValidation;

    namespace ProductService.API.Middleware;


/// <summary>
/// Middleware for handling exceptions and formatting error responses in the API.
/// </summary>
public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next,ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

    /// <summary>
    /// Invokes the middleware to handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
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
                InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("already exists") => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = invalidOpEx.Message
                },
                InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("modified by another user") => new ErrorResponse
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
                UnauthorizedAccessException => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Message = "Unauthorized access"
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

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public List<ErrorDetail>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ErrorDetail
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

