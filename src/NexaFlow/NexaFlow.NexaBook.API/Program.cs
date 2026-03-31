using NexaFlow.NexaBook.Application.Interfaces.Events;
using NexaFlow.NexaBook.Application.Interfaces.Repositories;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaBook.Application.Services;
using NexaFlow.NexaBook.Infrastructure.DBRepository;
using NexaFlow.NexaBook.Infrastructure.DBRepository.Events;
using NexaFlow.NexaBook.Infrastructure.Logging;
using NexaFlow.NexaBook.Infrastructure.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration["DB_CONNECTION"]
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";

// Infraestructura
builder.Services.AddScoped<ICustomerRepository>(_ => new CustomerRepository(conn));
builder.Services.AddScoped<IReservationRepository>(_ => new ReservationRepository(conn));
builder.Services.AddScoped<IEventRepository>(_ => new EventRepository(conn));
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(conn));
builder.Services.AddSingleton<IPosLogger, LambdaBookLogger>();

// Aplicación
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NexaBook API", Version = "v1" });
    c.AddSecurityDefinition("TenantId", new()
    {
        Name = "x-tenant-id",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "UUID del tenant"
    });
    c.AddSecurityDefinition("Role", new()
    {
        Name = "x-role",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "Rol del usuario: admin | customer"
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
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexaBook API v1"));

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
