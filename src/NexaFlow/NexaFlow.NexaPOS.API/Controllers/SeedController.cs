using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Application.Records.Create;
using Npgsql;

namespace NexaFlow.NexaPOS.API.Controllers;

/// <summary>
/// Genera datos demo aleatorios con historial de 14 días para que las métricas
/// de Analytics (ticket promedio, forecast, anomalías, top productos, stock crítico,
/// tasa de cancelación) tengan datos suficientes para visualizarse.
///
/// POST /seed  →  x-tenant-id: {uuid}
/// </summary>
[ApiController]
[Route("seed")]
[Produces("application/json")]
public class SeedController(
    IProductService productService,
    ICustomerService customerService,
    IConfiguration configuration) : ControllerBase
{
    // ── Catálogo de datos demo ────────────────────────────────────────────────
    private static readonly (string Name, decimal Price, int Stock, int Threshold)[] Products =
    [
        ("Café Americano",     5_500,  40, 8),
        ("Bandeja Paisa",     28_000,  25, 5),
        ("Jugo de Naranja",    7_000,  35, 6),
        ("Agua Mineral",       3_000,   3, 5),   // stock crítico intencional
        ("Postre del Día",    12_000,  18, 4),
        ("Empanada de Pipián", 4_500,  30, 5),
        ("Limonada de Coco",   8_000,  22, 4),
        ("Arroz con Pollo",   22_000,  20, 5),
        ("Sopa del Día",      15_000,  15, 4),
        ("Brownie con Helado", 9_500,  12, 3),
    ];

    private static readonly (string Name, string Phone, string Email)[] Customers =
    [
        ("Carlos Mendoza",   "3001234567", "carlos.mendoza@demo.com"),
        ("Laura Gómez",      "3109876543", "laura.gomez@demo.com"),
        ("Andrés Ruiz",      "3205551234", "andres.ruiz@demo.com"),
        ("Valentina Pérez",  "3154449876", "valentina.perez@demo.com"),
        ("Miguel Torres",    "3007778899", "miguel.torres@demo.com"),
        ("Sofía Ramírez",    "3112223344", "sofia.ramirez@demo.com"),
    ];

    // Días de historial: -13 a 0. Día -4 tiene cantidad alta → anomalía detectable.
    private static readonly int[] SaleDays = [-13, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0];

    // Reservas: pasadas (cancelled/completed), hoy (confirmed), futuras (pending)
    private static readonly (int DayOffset, string Status)[] ReservationDays =
    [
        (-3, "cancelled"), (-2, "completed"), (-1, "completed"),
        (-1, "cancelled"),
        (0,  "confirmed"), (0,  "confirmed"),
        (1,  "pending"),   (2,  "pending"),
    ];

    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR",
            "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    private string ConnString =>
        configuration["DB_CONNECTION"]
        ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
        ?? "Host=localhost;Database=NexosNexaFlow;Username=post_usr;Password=P3assW0e";

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateSeedData(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();

        var rng = new Random();
        int productCount = 0, customerCount = 0, saleCount = 0, reservationCount = 0;

        try
        {
            // ── 1. Productos ──────────────────────────────────────────────────
            var productIds   = new List<Guid>();
            var productPrices = new List<decimal>();

            var sample = Products.OrderBy(_ => rng.Next()).Take(5).ToList();
            foreach (var (name, price, stock, threshold) in sample)
            {
                try
                {
                    var id = await productService.CreateAsync(tenantId,
                        new CreateProductRequest(name, price, stock, threshold));
                    productIds.Add(id);
                    productPrices.Add(price);
                    productCount++;
                }
                catch { /* ya existe — ignorar */ }
            }

            // Fallback: usar productos existentes
            if (productIds.Count == 0)
            {
                var existing = await productService.GetPagedAsync(tenantId, 1, 20);
                if (existing.Data != null)
                    foreach (var p in existing.Data)
                    {
                        productIds.Add(p.Id);
                        productPrices.Add(p.Price);
                    }
            }

            if (productIds.Count == 0)
                return StatusCode(500, ApiResponse<object>.Fail("SEED_ERROR",
                    "No hay productos disponibles."));

            // ── 2. Clientes ───────────────────────────────────────────────────
            var customerIds = new List<Guid>();

            foreach (var (name, phone, email) in Customers)
            {
                try
                {
                    var id = await customerService.CreateAsync(tenantId,
                        new CreateCustomerRequest(name, phone, email));
                    customerIds.Add(id);
                    customerCount++;
                }
                catch { /* ya existe — ignorar */ }
            }

            // Fallback: usar clientes existentes
            if (customerIds.Count == 0)
            {
                var existing = await customerService.ListCustomersAsync(tenantId, 1, 20);
                if (existing.Data != null)
                    customerIds.AddRange(existing.Data.Select(c => c.Id));
            }

            if (customerIds.Count == 0)
                return StatusCode(500, ApiResponse<object>.Fail("SEED_ERROR",
                    "No hay clientes disponibles."));

            // ── 3. Ventas históricas (SQL directo para poder poner fechas pasadas) ──
            await using var conn = new NpgsqlConnection(ConnString);
            await conn.OpenAsync();
            await SetTenantAsync(conn, tenantId);

            // Obtener tax_rate del tenant
            decimal taxRate = 19m;
            await using (var cmd = new NpgsqlCommand(
                "SELECT tax_rate FROM tenant_config WHERE tenant_id = $1", conn))
            {
                cmd.Parameters.AddWithValue(tenantId);
                var result = await cmd.ExecuteScalarAsync();
                if (result is decimal tr) taxRate = tr;
            }

            foreach (var daysAgo in SaleDays)
            {
                var customerId = customerIds[rng.Next(customerIds.Count)];
                var saleDate   = DateTime.UtcNow.AddDays(daysAgo);
                var saleId     = Guid.NewGuid();

                // 1-3 items por venta; día -4 tiene cantidad alta para anomalía
                var itemCount = rng.Next(1, 4);
                var picked    = productIds.OrderBy(_ => rng.Next()).Take(itemCount).ToList();

                decimal subtotal = 0;
                var saleItems = new List<(Guid ProductId, decimal UnitPrice, int Qty)>();
                foreach (var pid in picked)
                {
                    var idx   = productIds.IndexOf(pid);
                    var price = productPrices[idx];
                    var qty   = rng.Next(1, daysAgo == -4 ? 6 : 3); // anomalía en día -4
                    subtotal += price * qty;
                    saleItems.Add((pid, price, qty));
                }

                var taxAmount = Math.Round(subtotal * taxRate / 100, 2);
                var total     = subtotal + taxAmount;

                await using var tx = await conn.BeginTransactionAsync();
                try
                {
                    await using (var cmd = new NpgsqlCommand(
                        @"INSERT INTO sales
                            (id, tenant_id, customer_id, subtotal, tax_rate, tax_amount, total, status, created_at)
                          VALUES ($1,$2,$3,$4,$5,$6,$7,'completed',$8)
                          ON CONFLICT (id) DO NOTHING", conn, tx))
                    {
                        cmd.Parameters.AddWithValue(saleId);
                        cmd.Parameters.AddWithValue(tenantId);
                        cmd.Parameters.AddWithValue(customerId);
                        cmd.Parameters.AddWithValue(subtotal);
                        cmd.Parameters.AddWithValue(taxRate);
                        cmd.Parameters.AddWithValue(taxAmount);
                        cmd.Parameters.AddWithValue(total);
                        cmd.Parameters.AddWithValue(saleDate);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    foreach (var (pid, price, qty) in saleItems)
                    {
                        await using var cmd = new NpgsqlCommand(
                            @"INSERT INTO sale_items (id, sale_id, product_id, quantity, unit_price)
                              VALUES ($1,$2,$3,$4,$5)
                              ON CONFLICT (id) DO NOTHING", conn, tx);
                        cmd.Parameters.AddWithValue(Guid.NewGuid());
                        cmd.Parameters.AddWithValue(saleId);
                        cmd.Parameters.AddWithValue(pid);
                        cmd.Parameters.AddWithValue(qty);
                        cmd.Parameters.AddWithValue(price);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    saleCount++;
                }
                catch { await tx.RollbackAsync(); }
            }

            // ── 4. Reservas (pasadas, hoy, futuras, con canceladas) ───────────
            foreach (var (dayOffset, status) in ReservationDays)
            {
                var customerId      = customerIds[rng.Next(customerIds.Count)];
                var reservationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(dayOffset));
                var hour            = rng.Next(8, 20);
                var timeSlot        = new TimeOnly(hour, 0);

                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO reservations
                        (id, tenant_id, customer_id, reservation_date, time_slot, status)
                      VALUES ($1,$2,$3,$4,$5,$6)
                      ON CONFLICT (id) DO NOTHING", conn);
                cmd.Parameters.AddWithValue(Guid.NewGuid());
                cmd.Parameters.AddWithValue(tenantId);
                cmd.Parameters.AddWithValue(customerId);
                cmd.Parameters.AddWithValue(reservationDate);
                cmd.Parameters.AddWithValue(timeSlot);
                cmd.Parameters.AddWithValue(status);

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                    reservationCount++;
                }
                catch { /* ignorar conflictos */ }
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                message       = $"Datos demo generados correctamente.",
                products      = productCount,
                customers     = customerCount,
                sales         = saleCount,
                reservations  = reservationCount,
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("SEED_ERROR",
                $"Error al generar datos demo: {ex.Message}"));
        }
    }

    private static async Task SetTenantAsync(NpgsqlConnection conn, Guid tenantId)
    {
        await using var cmd = new NpgsqlCommand(
            $"SET app.tenant_id = '{tenantId}'", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
