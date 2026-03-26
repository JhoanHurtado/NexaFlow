using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using NexaFlow.NexaBook;

// Indica al source generator que genere el método Main del ejecutable.
[assembly: LambdaGlobalProperties(GenerateMain = true)]

// Serializador basado en source generators para Native AOT.
[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>))]

namespace NexaFlow.NexaBook;

/// <summary>
/// Contenedor de los assembly attributes requeridos por Lambda Annotations.
/// Los handlers reales están en CustomerHandler y ReservationHandler.
/// </summary>
public class Functions
{
}
