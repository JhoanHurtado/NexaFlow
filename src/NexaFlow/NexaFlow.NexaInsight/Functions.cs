using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaGlobalProperties(GenerateMain = true)]
[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<NexaFlow.NexaInsight.LambdaFunctionJsonSerializerContext>))]

namespace NexaFlow.NexaInsight;

public class Functions { }
