using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;

namespace NexaFlow.NexaPOS.API.Controllers;

/// <summary>
/// Genera datos demo aleatorios para el tenant cuando no hay suficientes datos
/// para visualizar las métricas de Analytics.
/// Llamado automáticamente desde el frontend al detectar saleCount == 0.
/// </summary>
[ApiController]
[Route("seed")]
[Produces("application/json")]
public class SeedController(
    IProductService productService,
    ICustomerService customerService,
    ISaleService saleService) : ControllerBase
{
    private static readonly string[] ProductNames =
    [
        "Café Americano", "Bandeja Paisa", "Jugo de Naranja", "Agua Mineral",
        "Postre del Día", "Empanada de Pipián", "Limonada de Coco",
        "Arroz con Pollo", "Sopa del Día", "Brownie con Helado"
    ];

    private static readonly decimal[] ProductPrices =
    [
        5500, 28000, 7000, 3000, 12000, 4500, 8000, 22000, 15000, 9500
    ];

    private static readonly (string Name, string Phone, string Email)[] Customers =
    [
        ("Carlos Mendoza",  "3001234567", "carlos.mendoza@demo.com"),
        ("Laura Gómez",     "3109876543", "laura.gomez@demo.com"),
        ("Andrés Ruiz",     "3205551234", "andres.ruiz@demo.com"),
        ("Valentina Pérez", "3154449876", "valentina.perez@demo.com"),
        ("Miguel Torres",   "3007778899", "miguel.torres@demo.com"),
        ("Sofía Ramírez",   "3112223344", "sofia.ramirez@demo.com"),
    ];

    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR",
            "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateSeedData(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();

        var rng = new Random();
        var summary = new { products = 0, customers = 0, sales = 0 };
        int productCount = 0, customerCount = 0, saleCount = 0;

        try
        {
            // ── 1. Crear productos (5 aleatorios) ──────────────────────────────
            var productIds = new List<Guid>();
            var productPrices = new List<decimal>();
            var indices = Enumerable.Range(0, ProductNames.Length).OrderBy(_ => rng.Next()).Take(5).ToList();

            foreach (var i in indices)
            {
                try
                {
                    var id = await productService.CreateAsync(tenantId, new CreateProductRequest(
                        Name:              ProductNames[i],
                        Price:             ProductPrices[i],
                        InitialStock:      rng.Next(5, 50),
                        LowStockThreshold: rng.Next(3, 8)));
                    productIds.Add(id);
                    productPrices.Add(ProductPrices[i]);
                    productCount++;
                }
                catch { /* producto ya existe — ignorar */ }
            }

            // Si no se crearon productos nuevos, listar los existentes
            if (productIds.Count == 0)
            {
                var existing = await productService.GetPagedAsync(tenantId, 1, 10);
                if (existing.Data != null)
                {
                    foreach (var p in existing.Data)
                    {
                        productIds.Add(p.Id);
                        productPrices.Add(p.Price);
                    }
                }
            }

            if (productIds.Count == 0)
                return StatusCode(500, ApiResponse<object>.Fail("SEED_ERROR",
                    "No hay productos disponibles para generar ventas."));

            // ── 2. Crear clientes (5 aleatorios) ───────────────────────────────
            var customerIds = new List<Guid>();
            var customerSample = Customers.OrderBy(_ => rng.Next()).Take(5).ToList();

            foreach (var (name, phone, email) in customerSample)
            {
                try
                {
                    var id = await customerService.CreateAsync(tenantId,
                        new CreateCustomerRequest(name, phone, email));
                    customerIds.Add(id);
                    customerCount++;
                }
                catch { /* cliente ya existe — ignorar */ }
            }

            // Si no se crearon clientes nuevos, listar los existentes
            if (customerIds.Count == 0)
            {
                var existing = await customerService.ListCustomersAsync(tenantId, 1, 10);
                if (existing.Data != null)
                    customerIds.AddRange(existing.Data.Select(c => c.Id));
            }

            if (customerIds.Count == 0)
                return StatusCode(500, ApiResponse<object>.Fail("SEED_ERROR",
                    "No hay clientes disponibles para generar ventas."));

            // ── 3. Crear ventas — 14 días de historial ─────────────────────────
            // Mínimo 5 ventas, distribuidas en los últimos 14 días
            // Día -4 tiene venta alta para que el detector de anomalías la marque
            var salesDays = new[] { -13, -11, -9, -7, -6, -5, -4, -3, -2, -1, 0, 0 };

            foreach (var daysAgo in salesDays)
            {
                var customerId = customerIds[rng.Next(customerIds.Count)];

                // 1-3 productos por venta
                var itemCount = rng.Next(1, 4);
                var pickedIndices = Enumerable.Range(0, productIds.Count)
                    .OrderBy(_ => rng.Next()).Take(itemCount).ToList();

                var items = pickedIndices.Select(pi => new CreateSaleItemRequest(
                    ProductId: productIds[pi],
                    Quantity:  rng.Next(1, daysAgo == -4 ? 5 : 3) // día -4: cantidad alta = anomalía
                )).ToList();

                try
                {
                    await saleService.CreateAsync(tenantId, new CreateSaleRequest(
                        CustomerId:    customerId,
                        ReservationId: null,
                        Items:         items));
                    saleCount++;
                }
                catch { /* ignorar si falla por stock */ }
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                message  = $"Seed completado: {productCount} productos, {customerCount} clientes, {saleCount} ventas generadas.",
                products = productCount,
                customers = customerCount,
                sales    = saleCount,
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("SEED_ERROR",
                $"Error al generar datos demo: {ex.Message}"));
        }
    }
}
