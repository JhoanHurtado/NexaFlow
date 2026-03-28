using Amazon.Lambda.Annotations.APIGateway;

namespace NexaFlow.NexaAuth_Billing;

public static class Api
{
    private static readonly string Origin  = Environment.GetEnvironmentVariable("CORS_ORIGIN") ?? "*";
    private const string AllowHeaders = "Content-Type,Authorization,x-tenant-id,x-role,stripe-signature";
    private const string AllowMethods = "GET,POST,PUT,DELETE,OPTIONS";

    public static IHttpResult Ok(object body) => AddCors(HttpResults.Ok(body));
    public static IHttpResult Created(string location, object body) => AddCors(HttpResults.Created(location, body));
    public static IHttpResult BadRequest(object body) => AddCors(HttpResults.BadRequest(body));
    public static IHttpResult NotFound() => AddCors(HttpResults.NotFound());
    public static IHttpResult InternalServerError(object body) => AddCors(HttpResults.InternalServerError(body));

    private static IHttpResult AddCors(IHttpResult result) =>
        result
            .AddHeader("Access-Control-Allow-Origin",  Origin)
            .AddHeader("Access-Control-Allow-Headers", AllowHeaders)
            .AddHeader("Access-Control-Allow-Methods", AllowMethods);
}
