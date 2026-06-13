using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using NexaFlow.NexaPOS.Application.Records.Update;
using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Events;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly IStockRepository _stockRepository;
        private readonly IUnitOfWork _uow;
        private readonly IPosLogger _logger;

        public ProductService(IProductRepository repository, IStockRepository stockRepository, IUnitOfWork uow, IPosLogger logger)
        {
            _repository = repository;
            _stockRepository = stockRepository;
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
            catch { await _uow.RollbackAsync(); throw; }

            return product.Id;
        }

        public async Task UpdateAsync(Guid tenantId, Guid productId, UpdateProductRequest request)
        {
            var product = await _repository.GetByIdAsync(tenantId, productId)
                ?? throw new DomainException("Producto no encontrado.");

            if (request.Name is not null) product.UpdateName(request.Name);
            if (request.Price is not null) product.UpdatePrice(request.Price.Value);
            if (request.Active is true) product.Activate();
            else if (request.Active is false) product.Deactivate();

            await _repository.UpdateAsync(product);

            if (request.Stock is not null || request.LowStockThreshold is not null)
            {
                var stock = await _stockRepository.GetByProductIdAsync(tenantId, productId);
                if (stock is not null)
                {
                    if (request.Stock is not null) stock.SetQuantity(request.Stock.Value);
                    if (request.LowStockThreshold is not null) stock.SetThreshold(request.LowStockThreshold.Value);
                    await _stockRepository.UpdateAsync(stock);
                }
            }
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
