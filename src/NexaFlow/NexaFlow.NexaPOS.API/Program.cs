using NexaFlow.NexaPOS.Application.Interfaces.Events;
using Prometheus;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaPOS.Application.Services;
using NexaFlow.NexaPOS.Infrastructure.DBRepository;
using NexaFlow.NexaPOS.Infrastructure.DBRepository.Events;
using NexaFlow.NexaPOS.Infrastructure.Logging;
using NexaFlow.NexaPOS.Infrastructure.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration["DB_CONNECTION"]
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";

// Infraestructura
builder.Services.AddScoped<IProductRepository>(_ => new ProductRepository(conn));
builder.Services.AddScoped<ICustomerRepository>(_ => new CustomerRepository(conn));
builder.Services.AddScoped<ISaleRepository>(_ => new SaleRepository(conn));
builder.Services.AddScoped<IStockRepository>(_ => new StockRepository(conn));
builder.Services.AddScoped<IEventRepository>(_ => new EventRepository(conn));
builder.Services.AddScoped<ITenantConfigRepository>(_ => new TenantConfigRepository(conn));
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(conn));
builder.Services.AddSingleton<IPosLogger, LambdaPosLogger>();

// Aplicación
builder.Services.AddScoped<IProductService>(sp => new ProductService(
    sp.GetRequiredService<IProductRepository>(),
    sp.GetRequiredService<IStockRepository>(),
    sp.GetRequiredService<IUnitOfWork>(),
    sp.GetRequiredService<IPosLogger>()));
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<ITenantConfigService, TenantConfigService>();

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NexaPOS API", Version = "v1" });
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
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexaPOS API v1"));

app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
