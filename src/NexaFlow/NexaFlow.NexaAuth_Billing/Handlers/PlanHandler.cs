using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;

namespace NexaFlow.NexaAuth_Billing.Handlers;

public class PlanHandler
{
    private readonly IPlanRepository _planRepository;
    public PlanHandler(IPlanRepository planRepository) => _planRepository = planRepository;

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/plans")]
    public async Task<IHttpResult> GetPlans(ILambdaContext context)
    {
        var sw = Log.StartTimer();
        try
        {
            var plans = await _planRepository.GetAllAsync();
            var data = plans.Select(p => new
            {
                Id           = p.Id,
                Name         = p.Name,
                Price        = p.Price,
                MaxUsers     = p.MaxUsers,
                StripePriceId = p.StripePriceId,
            });
            Log.Info(context, "plans-list", "Plans retrieved",
                method: "GET", path: "/plans", durationMs: sw.ElapsedMilliseconds);
            return Api.Ok(data);
        }
        catch (Exception ex)
        {
            Log.Error(context, "plans-list", "Unhandled error retrieving plans",
                ex: ex, method: "GET", path: "/plans", durationMs: sw.ElapsedMilliseconds);
            return Api.InternalServerError("PLANS_ERROR", "Error al obtener planes");
        }
    }
}
