using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaPOS.Application.Services;
using NexaFlow.NexaPOS.Handlers;
using NexaFlow.NexaPOS.Infrastructure.DBRepository;
using NexaFlow.NexaPOS.Infrastructure.DBRepository.Events;
using NexaFlow.NexaPOS.Infrastructure.Logging;
using NexaFlow.NexaPOS.Infrastructure.UnitOfWork;

namespace NexaFlow.NexaPOS;

/// <summary>
/// Punto de configuración del contenedor de inyección de dependencias para las funciones Lambda.
/// Marcado con <see cref="LambdaStartupAttribute"/> para que Lambda Annotations lo detecte automáticamente.
/// La cadena de conexión se lee de la variable de entorno <c>DB_CONNECTION</c>.
/// </summary>
[LambdaStartup]
public class Startup
{
    /// <summary>
    /// Registra todos los servicios, repositorios y handlers en el contenedor DI.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                           ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";

        // Infraestructura — repositorios de solo lectura / consulta
        services.AddScoped<IProductRepository>(_ => new ProductRepository(connectionString));
        services.AddScoped<ICustomerRepository>(_ => new CustomerRepository(connectionString));
        services.AddScoped<ISaleRepository>(_ => new SaleRepository(connectionString));
        services.AddScoped<IStockRepository>(_ => new StockRepository(connectionString));
        services.AddScoped<IEventRepository>(_ => new EventRepository(connectionString));

        // Unidad de trabajo — maneja transacciones atómicas (producto/venta + stock + eventos)
        services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));

        // Logger — escribe a Console, Lambda reenvía a CloudWatch
        services.AddSingleton<IPosLogger, LambdaPosLogger>();

        // Aplicación
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISaleService, SaleService>();

        // Handlers Lambda
        services.AddScoped<ProductHandler>();
        services.AddScoped<CustomerHandler>();
        services.AddScoped<SaleHandler>();
    }
}
