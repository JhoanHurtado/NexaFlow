namespace NexaFlow.NexaInsight.Application.Dto;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = "";

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
}
