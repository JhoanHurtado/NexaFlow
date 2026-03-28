using Amazon.Lambda.Annotations.APIGateway;

namespace NexaFlow.NexaPOS;

public static class Api
{
    private const string Origin  = "*";
    private const string Headers = "Content-Type,Authorization,x-tenant-id,stripe-signature";
    private const string Methods = "GET,POST,PUT,DELETE,OPTIONS";

    public static IHttpResult Ok(object body) =>
        AddCors(HttpResults.Ok(body));

    public static IHttpResult Created(string location, object body) =>
        AddCors(HttpResults.Created(location, body));

    public static IHttpResult BadRequest(object body) =>
        AddCors(HttpResults.BadRequest(body));

    public static IHttpResult NotFound() =>
        AddCors(HttpResults.NotFound());

    public static IHttpResult InternalServerError(object body) =>
        AddCors(HttpResults.InternalServerError(body));

    private static IHttpResult AddCors(IHttpResult result) =>
        result
            .AddHeader("Access-Control-Allow-Origin",  Origin)
            .AddHeader("Access-Control-Allow-Headers", Headers)
            .AddHeader("Access-Control-Allow-Methods", Methods);
}
