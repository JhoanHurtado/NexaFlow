namespace NexaFlow.NexaPOS.Application.Dto
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorCode { get; set; }
        public string Message { get; set; } = "Accion completada correctamente.";
        public PaginationMetadata? Pagination { get; set; }

        public static ApiResponse<T> Ok(T data, PaginationMetadata? meta = null)
            => new() { Success = true, Data = data, Pagination = meta };

        public static ApiResponse<T> Fail(string errorCode, string message)
            => new() { Success = false, ErrorCode = errorCode, Message = message };
    }
}
