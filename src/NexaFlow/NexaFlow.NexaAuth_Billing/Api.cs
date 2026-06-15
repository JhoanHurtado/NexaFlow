using Amazon.Lambda.Annotations.APIGateway;
using NexaFlow.NexaAuth_Billing.Application.Dto;

namespace NexaFlow.NexaAuth_Billing;

public static class Api
{
    private static string Origin => Environment.GetEnvironmentVariable("CORS_ORIGIN") is { Length: > 0 } o ? o : "*";
    private const string AllowHeaders = "Content-Type,Authorization,x-tenant-id,x-role,stripe-signature";
    private const string AllowMethods = "GET,POST,PUT,PATCH,DELETE,OPTIONS";

    public static IHttpResult Ok(object body) => AddCors(HttpResults.Ok(body));
    public static IHttpResult Created(string location, object body) => AddCors(HttpResults.Created(location, body));
    public static IHttpResult BadRequest(string errorCode, string message)
        => AddCors(HttpResults.BadRequest(ApiResponse<object>.Fail(errorCode, message)));
    public static IHttpResult BadRequest(ApiResponse<object> response)
        => AddCors(HttpResults.BadRequest(response));
    public static IHttpResult NotFound(string errorCode = "NOT_FOUND", string message = "Recurso no encontrado")
        => AddCors(HttpResults.NotFound(ApiResponse<object>.Fail(errorCode, message)));
    public static IHttpResult InternalServerError(string errorCode, string message)
        => AddCors(HttpResults.InternalServerError(ApiResponse<object>.Fail(errorCode, message)));

    private static IHttpResult AddCors(IHttpResult result) =>
        result
            .AddHeader("Access-Control-Allow-Origin",  Origin)
            .AddHeader("Access-Control-Allow-Headers", AllowHeaders)
            .AddHeader("Access-Control-Allow-Methods", AllowMethods);
}
