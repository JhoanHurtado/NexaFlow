using Amazon.Lambda.Annotations.APIGateway;
using NexaFlow.NexaInsight.Application.Dto;

namespace NexaFlow.NexaInsight;

public static class Validate
{
    public static bool TryParseGuid(string? value, string paramName, out Guid result, out IHttpResult? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = Guid.Empty;
            ApiResponse<string> response = new ApiResponse<string>
            {
                Success = false,
                Message = $"El parámetro '{paramName}' es requerido."
            };
            error = Api.BadRequest(response);
            return false;
        }
        if (!Guid.TryParse(value, out result))
        {
            ApiResponse<string> response = new ApiResponse<string>
            {
                Success = false,
                Message = $"El parámetro '{paramName}' no tiene un formato válido. Valor recibido: '{value}'."
            };  
            error = Api.BadRequest(response);
            return false;
        }
        error = null;
        return true;
    }

    public static bool TryParseDateOnly(string? value, string paramName, out DateOnly result, out IHttpResult? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            ApiResponse<string> response = new ApiResponse<string>
            {
                Success = false,
                Message = $"El parámetro '{paramName}' es requerido."
            };
            error = Api.BadRequest(response);
            return false;
        }
        if (!DateOnly.TryParse(value, out result))
        {
            ApiResponse<string> response = new ApiResponse<string>
            {
                Success = false,
                Message = $"El parámetro '{paramName}' no tiene un formato de fecha válido (yyyy-MM-dd). Valor recibido: '{value}'."
            };
            error = Api.BadRequest(response);
            return false;
        }
        error = null;
        return true;
    }
}


