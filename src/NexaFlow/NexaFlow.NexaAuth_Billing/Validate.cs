using Amazon.Lambda.Annotations.APIGateway;
using NexaFlow.NexaAuth_Billing.Application.Dto;

namespace NexaFlow.NexaAuth_Billing;

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
}
