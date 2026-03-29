using Amazon.Lambda.Annotations.APIGateway;
using NexaFlow.NexaBook.Application.Dto;

namespace NexaFlow.NexaBook;

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
}
