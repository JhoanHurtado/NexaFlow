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
            error = Api.BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", $"El parámetro '{paramName}' es requerido."));
            return false;
        }
        if (!Guid.TryParse(value, out result))
        {
            error = Api.BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", $"El parámetro '{paramName}' no tiene un formato válido. Valor recibido: '{value}'."));
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
            error = Api.BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", $"El parámetro '{paramName}' es requerido."));
            return false;
        }
        if (!DateOnly.TryParse(value, out result))
        {
            error = Api.BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", $"El parámetro '{paramName}' no tiene un formato de fecha válido (yyyy-MM-dd). Valor recibido: '{value}'."));
            return false;
        }
        error = null;
        return true;
    }
}


