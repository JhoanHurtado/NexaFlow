using Amazon.Lambda.Annotations.APIGateway;
using NexaFlow.NexaPOS.Application.Dto;

namespace NexaFlow.NexaPOS;

/// <summary>
/// Clase de validación para realizar verificaciones de datos.
/// </summary>
public static class Validate
{
    /// <summary>
    /// Valida que el valor no sea nulo, vacío o solo espacios en blanco, y que pueda ser convertido a Guid. Si la validación falla, se devuelve un error HTTP 400 con un mensaje detallado.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="paramName"></param>
    /// <param name="result"></param>
    /// <param name="error"></param>
    /// <returns></returns> <summary>
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
            error = HttpResults.BadRequest(response);
            return false;
        }
        if (!Guid.TryParse(value, out result))
        {
            ApiResponse<string> response = new ApiResponse<string>
            {
                Success = false,
                Message = $"El parámetro '{paramName}' no tiene un formato válido. Valor recibido: '{value}'."
            };  
            error = HttpResults.BadRequest(response);
            return false;
        }
        error = null;
        return true;
    }
}
