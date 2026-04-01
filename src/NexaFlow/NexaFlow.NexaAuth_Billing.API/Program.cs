using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;
using NexaFlow.NexaAuth_Billing.Application.Services;
using NexaFlow.NexaAuth_Billing.Infrastructure.DBRepository;
using NexaFlow.NexaAuth_Billing.Infrastructure.Logging;
using NexaFlow.NexaAuth_Billing.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration["DB_CONNECTION"]
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";
var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? "nexaflow-dev-secret-min32chars!!";
var jwtIssuer = builder.Configuration["JWT_ISSUER"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "nexaflow";

// Infraestructura
builder.Services.AddScoped<ITenantRepository>(_ => new TenantRepository(conn));
builder.Services.AddScoped<IUserRepository>(_ => new UserRepository(conn));
builder.Services.AddScoped<ISubscriptionRepository>(_ => new SubscriptionRepository(conn));
builder.Services.AddScoped<IWebhookEventRepository>(_ => new WebhookEventRepository(conn));
builder.Services.AddScoped<IPlanRepository>(_ => new PlanRepository(conn));
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtService>(_ => new JwtService(jwtSecret, jwtIssuer));
builder.Services.AddSingleton<IAuthLogger, LambdaAuthLogger>();

// Aplicación
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NexaAuth & Billing API", Version = "v1" });
    c.AddSecurityDefinition("TenantId", new()
    {
        Name = "x-tenant-id",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "UUID del tenant"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "TenantId" } },
            []
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexaAuth & Billing API v1"));

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
