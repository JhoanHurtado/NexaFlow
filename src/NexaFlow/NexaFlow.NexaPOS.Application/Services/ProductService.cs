using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Events;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IUnitOfWork _uow;
        private readonly IPosLogger _logger;

        public ProductService(IProductRepository repository, IUnitOfWork uow, IPosLogger logger)
        {
            _repository = repository;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Guid> CreateAsync(Guid tenantId, CreateProductRequest request)
        {
            if (request.Price <= 0)
                throw new DomainException("El precio debe ser mayor a cero.");
            if (request.InitialStock < 0)
                throw new DomainException("El stock inicial no puede ser negativo.");
            if (request.LowStockThreshold < 1)
                throw new DomainException("El umbral de stock bajo debe ser al menos 1.");

            if (await _repository.ExistsByNameAsync(tenantId, request.Name))
                throw new DomainException($"Ya existe un producto activo con el nombre '{request.Name}'.");

            _logger.Info($"[Product] Creando '{request.Name}' para tenant {tenantId}");

            var product = new Product(tenantId, request.Name, request.Price);
            var stock = new ProductStock(product.Id, tenantId, request.InitialStock, request.LowStockThreshold);

            await _uow.BeginAsync(tenantId);
            try
            {
                await _uow.SaveProductAsync(product);
                await _uow.SaveStockAsync(stock);
                await _uow.EnqueueEventAsync(new ProductCreatedEvent(tenantId, product.Id, product.Name, product.Price));
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.Info($"[Product] Producto {product.Id} creado con stock {request.InitialStock}");
            return product.Id;
        }

        public async Task<ApiResponse<IEnumerable<ProductDTO>>> GetPagedAsync(Guid tenantId, int page, int pageSize)
        {
            if (page < 1) throw new DomainException("La página debe ser mayor a cero.");
            if (pageSize < 1 || pageSize > 100) throw new DomainException("El tamaño de página debe estar entre 1 y 100.");

            var (products, totalCount) = await _repository.GetPagedAsync(tenantId, page, pageSize);
            var dtos = products.Select(p => new ProductDTO(p.Product.Id, p.Product.Name, p.Product.Price, p.Stock, p.LowStockThreshold, p.Product.IsActive));
            return ApiResponse<IEnumerable<ProductDTO>>.Ok(dtos, new PaginationMetadata(page, pageSize, totalCount));
        }
    }
}
