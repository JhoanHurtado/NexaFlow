namespace NexaFlow.NexaPOS.Application.Dto
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = "";
        public PaginationMetadata? Pagination { get; set; }

        public static ApiResponse<T> Ok(T data, PaginationMetadata? meta = null)
            => new() { Success = true, Data = data, Pagination = meta };
    }
}
