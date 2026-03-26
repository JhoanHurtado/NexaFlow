using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.NexaBook.Application.Interfaces.Events;
using NexaFlow.NexaBook.Application.Interfaces.Repositories;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaBook.Application.Services;
using NexaFlow.NexaBook.Handlers;
using NexaFlow.NexaBook.Infrastructure.DBRepository;
using NexaFlow.NexaBook.Infrastructure.DBRepository.Events;
using NexaFlow.NexaBook.Infrastructure.Logging;
using NexaFlow.NexaBook.Infrastructure.UnitOfWork;

namespace NexaFlow.NexaBook;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                           ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";

        // Infraestructura
        services.AddScoped<ICustomerRepository>(_ => new CustomerRepository(connectionString));
        services.AddScoped<IReservationRepository>(_ => new ReservationRepository(connectionString));
        services.AddScoped<IEventRepository>(_ => new EventRepository(connectionString));
        services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));

        // Logger
        services.AddSingleton<IPosLogger, LambdaBookLogger>();

        // Aplicación
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IReservationService, ReservationService>();

        // Handlers
        services.AddScoped<CustomerHandler>();
        services.AddScoped<ReservationHandler>();
    }
}
