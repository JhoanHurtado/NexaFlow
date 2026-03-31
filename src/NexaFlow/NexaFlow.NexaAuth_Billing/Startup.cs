using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Services;
using NexaFlow.NexaAuth_Billing.Handlers;
using NexaFlow.NexaAuth_Billing.Infrastructure.DBRepository;
using NexaFlow.NexaAuth_Billing.Infrastructure.Logging;
using NexaFlow.NexaAuth_Billing.Infrastructure.Security;

namespace NexaFlow.NexaAuth_Billing;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var conn = Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "nexaflow-dev-secret-min32chars!!";
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "nexaflow";

        // Infraestructura
        services.AddScoped<ITenantRepository>(_ => new TenantRepository(conn));
        services.AddScoped<IUserRepository>(_ => new UserRepository(conn));
        services.AddScoped<ISubscriptionRepository>(_ => new SubscriptionRepository(conn));
        services.AddScoped<IWebhookEventRepository>(_ => new WebhookEventRepository(conn));

        // Seguridad
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtService>(_ => new JwtService(jwtSecret, jwtIssuer));
        services.AddSingleton<IAuthLogger, LambdaAuthLogger>();

        // Aplicación
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // Handlers
        services.AddScoped<AuthHandler>();
        services.AddScoped<UserHandler>();
        services.AddScoped<SubscriptionHandler>();
    }
}
