using MediatR;

namespace ProductService.Application.Common.Behaviors
{

    // Logging Behavior
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogInformation(
                "Handling {RequestName}: {@Request}",
                requestName,
                request);

            var startTime = DateTime.UtcNow;

            try
            {
                var response = await next();

                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMs}ms",
                    requestName,
                    elapsedMs);

                return response;
            }
            catch (Exception ex)
            {
                var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogError(
                    ex,
                    "Error handling {RequestName} after {ElapsedMs}ms: {ErrorMessage}",
                    requestName,
                    elapsedMs,
                    ex.Message);

                throw;
            }
        }
    }

}
