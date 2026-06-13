using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Events;
using NexaFlow.NexaPOS.Application.Interfaces.UnitOfWork;
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
        private readonly ITenantConfigRepository _configRepository;
        private readonly IUnitOfWork _uow;
        private readonly IPosLogger _logger;

        public SaleService(
            ISaleRepository saleRepository,
            IProductRepository productRepository,
            IStockRepository stockRepository,
            ITenantConfigRepository configRepository,
            IUnitOfWork uow,
            IPosLogger logger)
        {
            _saleRepository = saleRepository;
            _productRepository = productRepository;
            _stockRepository = stockRepository;
            _configRepository = configRepository;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Guid> CreateAsync(Guid tenantId, CreateSaleRequest request)
        {
            if (!request.Items.Any())
                throw new DomainException("La venta debe tener al menos un producto.");
            if (request.Items.Any(i => i.Quantity <= 0))
                throw new DomainException("Todos los ítems deben tener cantidad mayor a cero.");
            if (request.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
                throw new DomainException("No se puede repetir el mismo producto. Ajusta la cantidad.");

            // Leer configuración del tenant para obtener tasa de IVA
            var config = await _configRepository.GetOrDefaultAsync(tenantId);

            // Auto-link reserva: si el cliente tiene una reserva para hoy, asociarla
            var reservationId = request.ReservationId;
            if (reservationId is null && request.CustomerId.HasValue)
                reservationId = await FindTodayReservationAsync(tenantId, request.CustomerId.Value);

            _logger.Info($"[Sale] Iniciando venta tenant={tenantId} IVA={config.TaxRate}% reserva={reservationId}");

            var sale = new Sale(tenantId, request.CustomerId, reservationId, config.TaxRate);
            var stockUpdates = new List<(ProductStock Stock, string ProductName)>();
            var pendingEvents = new List<DomainEvent>();

            foreach (var item in request.Items)
            {
                var product = await _productRepository.GetByIdAsync(tenantId, item.ProductId)
                    ?? throw new DomainException($"Producto {item.ProductId} no encontrado.");
                var stock = await _stockRepository.GetByProductIdAsync(tenantId, item.ProductId)
                    ?? throw new DomainException($"No se encontró stock para '{product.Name}'.");
                stock.Deduct(item.Quantity);
                sale.AddItem(product, item.Quantity);
                stockUpdates.Add((stock, product.Name));
            }

            sale.ValidateForCheckout();
            //sale.Complete(); // la venta se completa al confirmar

            foreach (var (stock, productName) in stockUpdates)
            {
                if (stock.IsDepleted)
                    pendingEvents.Add(new StockDepletedEvent(tenantId, stock.ProductId, productName));
                else if (stock.IsLow)
                    pendingEvents.Add(new StockLowEvent(tenantId, stock.ProductId, productName, stock.Quantity, stock.LowStockThreshold));
            }
            pendingEvents.Add(new SaleCreatedEvent(tenantId, sale.Id, sale.Total, sale.Items.Count, sale.CustomerId, sale.ReservationId));

            await _uow.BeginAsync(tenantId);
            try
            {
                await _uow.SaveSaleAsync(sale);
                await _uow.SaveSaleItemsAsync(sale.Items);
                foreach (var (stock, _) in stockUpdates) await _uow.UpdateStockAsync(stock);
                foreach (var evt in pendingEvents) await _uow.EnqueueEventAsync(evt);
                await _uow.CommitAsync();
            }
            catch { await _uow.RollbackAsync(); throw; }

            _logger.Info($"[Sale] Venta {sale.Id} completada. Subtotal={sale.Subtotal} IVA={sale.TaxAmount} Total={sale.Total}");
            return sale.Id;
        }

        public async Task<ApiResponse<SaleDTO?>> GetSaleByIdAsync(Guid tenantId, Guid saleId)
        {
            if (saleId == Guid.Empty) throw new DomainException("El id de la venta es requerido.");
            var result = await _saleRepository.GetByIdAsync(tenantId, saleId);
            return result is null ? ApiResponse<SaleDTO?>.Ok(null) : ApiResponse<SaleDTO?>.Ok(MapToDto(result));
        }

        public async Task<ApiResponse<IEnumerable<SaleDTO>>> ListSalesAsync(Guid tenantId, int page, int pageSize)
        {
            if (page < 1) throw new DomainException("La página debe ser mayor a cero.");
            if (pageSize < 1 || pageSize > 200) throw new DomainException("El tamaño de página debe estar entre 1 y 200.");
            var (sales, total) = await _saleRepository.GetPagedAsync(tenantId, page, pageSize);
            return ApiResponse<IEnumerable<SaleDTO>>.Ok(sales.Select(MapToDto), new PaginationMetadata(page, pageSize, total));
        }

        public async Task UpdateStatusAsync(Guid tenantId, Guid saleId, string status)
        {
            var allowed = new[] { "pending", "completed", "cancelled" };
            if (!allowed.Contains(status))
                throw new DomainException($"Estado inválido. Valores permitidos: {string.Join(", ", allowed)}.");
            await _saleRepository.UpdateStatusAsync(tenantId, saleId, status);
        }

        // Busca reserva de hoy para el cliente (para auto-link)
        private async Task<Guid?> FindTodayReservationAsync(Guid tenantId, Guid customerId)        {
            try
            {
                return await _saleRepository.FindTodayReservationAsync(tenantId, customerId);
            }
            catch { return null; }
        }

        private static SaleDTO MapToDto(SaleWithItems s) => new(
            s.Sale.Id, s.Sale.TenantId, s.Sale.CustomerId, s.Sale.ReservationId,
            s.Sale.Subtotal, s.Sale.TaxRate, s.Sale.TaxAmount, s.Sale.Total,
            s.Sale.Status, s.Sale.CreatedAt,
            s.Items.Select(i => new SaleItemDTO(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity))
        );
    }
}
