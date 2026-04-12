namespace GymManagementBackend.DTOs
{
    public class ApiErrorResponseDto
    {
        public bool Success { get; init; } = false;
        public string Message { get; init; } = string.Empty;
        public string Code { get; init; } = "ERROR";
        public string? TraceId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public object? Details { get; init; }
    }
}

