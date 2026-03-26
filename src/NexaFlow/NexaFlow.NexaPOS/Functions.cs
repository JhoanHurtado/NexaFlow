using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using NexaFlow.NexaPOS;

// Indica al source generator que genere el método Main del ejecutable.
// Requerido para Native AOT y el modo Executable Assembly del Mock Lambda Test Tool.
[assembly: LambdaGlobalProperties(GenerateMain = true)]

// Registra el serializador basado en source generators para evitar reflection en AOT.
[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<LambdaFunctionJsonSerializerContext>))]

namespace NexaFlow.NexaPOS;

/// <summary>
/// Clase contenedora de los assembly attributes requeridos por Lambda Annotations.
/// El source generator de Lambda Annotations genera el método <c>Main</c> del ejecutable
/// y el switch de <c>ANNOTATIONS_HANDLER</c> a partir de los handlers registrados.
/// Los handlers reales están en <c>ProductHandler</c>, <c>SaleHandler</c> y <c>CustomerHandler</c>.
/// </summary>
public class Functions
{
}
