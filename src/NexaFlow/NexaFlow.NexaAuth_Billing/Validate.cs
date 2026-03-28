using Amazon.Lambda.Annotations.APIGateway;

namespace NexaFlow.NexaAuth_Billing;

public static class Validate
{
    public static bool TryParseGuid(string? value, string paramName, out Guid result, out IHttpResult? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = Guid.Empty;
            error = HttpResults.BadRequest($"El parámetro '{paramName}' es requerido.");
            return false;
        }
        if (!Guid.TryParse(value, out result))
        {
            error = HttpResults.BadRequest($"El parámetro '{paramName}' no tiene un formato UUID válido. Valor recibido: '{value}'.");
            return false;
        }
        error = null;
        return true;
    }
}
