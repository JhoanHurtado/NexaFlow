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
    public class SaleService : ISaleService
    {
        private readonly ISaleRepository _saleRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IUnitOfWork _uow;
        private readonly IPosLogger _logger;

        public SaleService(
            ISaleRepository saleRepository,
            IProductRepository productRepository,
            IStockRepository stockRepository,
            IUnitOfWork uow,
            IPosLogger logger)
        {
            _saleRepository = saleRepository;
            _productRepository = productRepository;
            _stockRepository = stockRepository;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Guid> CreateAsync(Guid tenantId, CreateSaleRequest request)
        {
            // --- Validaciones de entrada ---
            if (!request.Items.Any())
                throw new DomainException("La venta debe tener al menos un producto.");
            if (request.Items.Any(i => i.Quantity <= 0))
                throw new DomainException("Todos los ítems deben tener cantidad mayor a cero.");
            if (request.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
                throw new DomainException("No se puede repetir el mismo producto. Ajusta la cantidad.");

            _logger.Info($"[Sale] Iniciando venta para tenant {tenantId}, items: {request.Items.Count()}");

            // --- Construir la venta y validar stock ANTES de abrir la transacción ---
            var sale = new Sale(tenantId, request.CustomerId, request.ReservationId);
            var stockUpdates = new List<(ProductStock Stock, string ProductName, int Delta)>();
            var pendingEvents = new List<DomainEvent>();

            foreach (var item in request.Items)
            {
                var product = await _productRepository.GetByIdAsync(tenantId, item.ProductId)
                    ?? throw new DomainException($"Producto {item.ProductId} no encontrado.");

                var stock = await _stockRepository.GetByProductIdAsync(tenantId, item.ProductId)
                    ?? throw new DomainException($"No se encontró stock para el producto '{product.Name}'.");

                // Valida disponibilidad (lanza DomainException si no hay suficiente)
                stock.Deduct(item.Quantity);
                sale.AddItem(product, item.Quantity);
                stockUpdates.Add((stock, product.Name, item.Quantity));
            }

            sale.ValidateForCheckout();

            // --- Preparar eventos según estado del stock ---
            foreach (var (stock, productName, _) in stockUpdates)
            {
                if (stock.IsDepleted)
                {
                    _logger.Warning($"[Stock] '{productName}' agotado tras la venta.");
                    pendingEvents.Add(new StockDepletedEvent(tenantId, stock.ProductId, productName));
                }
                else if (stock.IsLow)
                {
                    _logger.Warning($"[Stock] '{productName}' con stock bajo: {stock.Quantity}");
                    pendingEvents.Add(new StockLowEvent(tenantId, stock.ProductId, productName, stock.Quantity, stock.LowStockThreshold));
                }
            }

            pendingEvents.Add(new SaleCreatedEvent(tenantId, sale.Id, sale.Total, sale.Items.Count, sale.CustomerId, sale.ReservationId));

            // --- Transacción atómica: sale + stock + eventos en una sola operación DB ---
            await _uow.BeginAsync(tenantId);
            try
            {
                await _uow.SaveSaleAsync(sale);
                await _uow.SaveSaleItemsAsync(sale.Items);

                foreach (var (stock, _, _) in stockUpdates)
                    await _uow.UpdateStockAsync(stock);

                foreach (var evt in pendingEvents)
                    await _uow.EnqueueEventAsync(evt);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.Info($"[Sale] Venta {sale.Id} creada. Total: {sale.Total}. Eventos encolados: {pendingEvents.Count}");
            return sale.Id;
        }

        public async Task<ApiResponse<SaleDTO?>> GetSaleByIdAsync(Guid tenantId, Guid saleId)
        {
            if (saleId == Guid.Empty)
                throw new DomainException("El id de la venta es requerido.");

            var result = await _saleRepository.GetByIdAsync(tenantId, saleId);
            if (result is null)
            {
                _logger.Warning($"[Sale] Venta {saleId} no encontrada para tenant {tenantId}");
                return ApiResponse<SaleDTO?>.Ok(null);
            }
            return ApiResponse<SaleDTO?>.Ok(MapToDto(result));
        }

        public async Task<ApiResponse<IEnumerable<SaleDTO>>> ListSalesAsync(Guid tenantId, int page, int pageSize)
        {
            if (page < 1) throw new DomainException("La página debe ser mayor a cero.");
            if (pageSize < 1 || pageSize > 100) throw new DomainException("El tamaño de página debe estar entre 1 y 100.");

            var (sales, total) = await _saleRepository.GetPagedAsync(tenantId, page, pageSize);
            return ApiResponse<IEnumerable<SaleDTO>>.Ok(sales.Select(MapToDto), new PaginationMetadata(page, pageSize, total));
        }

        private static SaleDTO MapToDto(SaleWithItems s) => new(
            s.Sale.Id, s.Sale.TenantId, s.Sale.CustomerId, s.Sale.ReservationId, s.Sale.Total, s.Sale.CreatedAt,
            s.Items.Select(i => new SaleItemDTO(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity))
        );
    }
}
