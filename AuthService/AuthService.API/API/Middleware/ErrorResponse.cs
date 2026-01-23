namespace AuthService.API.Middleware
{
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
}
