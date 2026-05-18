using NexaFlow.NexaInsight.Application.Interfaces.Repositories;
using Prometheus;
using NexaFlow.NexaInsight.Application.Interfaces.Services;
using NexaFlow.NexaInsight.Application.Services;
using NexaFlow.NexaInsight.Infrastructura.DBRepository;
using NexaFlow.NexaInsight.Infrastructura.Logging;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration["DB_CONNECTION"]
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";

// Infraestructura
builder.Services.AddScoped<ISalesInsightRepository>(_ => new SalesInsightRepository(conn));
builder.Services.AddScoped<IReservationInsightRepository>(_ => new ReservationInsightRepository(conn));
builder.Services.AddSingleton<IInsightLogger, LambdaInsightLogger>();

// Aplicación
builder.Services.AddScoped<IInsightService, InsightService>();

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NexaInsight API", Version = "v1" });
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
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexaInsight API v1"));

app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
