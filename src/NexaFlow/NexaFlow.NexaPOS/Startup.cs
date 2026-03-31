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

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                           ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";

        services.AddScoped<IProductRepository>(_ => new ProductRepository(connectionString));
        services.AddScoped<ICustomerRepository>(_ => new CustomerRepository(connectionString));
        services.AddScoped<ISaleRepository>(_ => new SaleRepository(connectionString));
        services.AddScoped<IStockRepository>(_ => new StockRepository(connectionString));
        services.AddScoped<IEventRepository>(_ => new EventRepository(connectionString));
        services.AddScoped<ITenantConfigRepository>(_ => new TenantConfigRepository(connectionString));
        services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
        services.AddSingleton<IPosLogger, LambdaPosLogger>();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ITenantConfigService, TenantConfigService>();

        services.AddScoped<ProductHandler>();
        services.AddScoped<CustomerHandler>();
        services.AddScoped<SaleHandler>();
        services.AddScoped<ConfigHandler>();
    }
}
