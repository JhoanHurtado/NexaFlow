using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.NexaInsight.Application.Interfaces.Repositories;
using NexaFlow.NexaInsight.Application.Interfaces.Services;
using NexaFlow.NexaInsight.Application.Services;
using NexaFlow.NexaInsight.Handlers;
using NexaFlow.NexaInsight.Infrastructura.DBRepository;
using NexaFlow.NexaInsight.Infrastructura.Logging;

namespace NexaFlow.NexaInsight;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var conn = Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";

        services.AddScoped<ISalesInsightRepository>(_ => new SalesInsightRepository(conn));
        services.AddScoped<IReservationInsightRepository>(_ => new ReservationInsightRepository(conn));
        services.AddScoped<IStockInsightRepository>(_ => new StockInsightRepository(conn));
        services.AddSingleton<IInsightLogger, LambdaInsightLogger>();
        services.AddScoped<IInsightService, InsightService>();
        services.AddScoped<InsightHandler>();
    }
}
