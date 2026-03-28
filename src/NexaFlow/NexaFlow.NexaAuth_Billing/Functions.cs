using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaGlobalProperties(GenerateMain = true)]
[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<NexaFlow.NexaAuth_Billing.LambdaFunctionJsonSerializerContext>))]

namespace NexaFlow.NexaAuth_Billing;

public class Functions { }
