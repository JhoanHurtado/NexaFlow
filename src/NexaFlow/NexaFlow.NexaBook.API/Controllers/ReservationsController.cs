using Microsoft.AspNetCore.Mvc;
using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Records.Create;
using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.API.Controllers;

[ApiController]
[Produces("application/json")]
public class ReservationsController(IReservationService reservationService) : ControllerBase
{
    private IActionResult TenantError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El header 'x-tenant-id' es requerido y debe ser un UUID válido."));

    private IActionResult IdError() =>
        BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'id' no tiene un formato válido."));

    private IActionResult AdminOnly() =>
        StatusCode(403, ApiResponse<object>.Fail("FORBIDDEN", "Esta operación requiere rol admin."));

    // ── Compartidos ────────────────────────────────────────────────────────

    [HttpPost("reservations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateReservation(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromBody] CreateReservationRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var id = await reservationService.CreateAsync(tenantId, body);
            return Created($"/reservations/{id}", new { id });
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_CREATE_ERROR", "Error al crear reserva"));
        }
    }

    [HttpPost("reservations/{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelReservation(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id,
        [FromBody] CancelReservationRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var reservationId)) return IdError();
        try
        {
            await reservationService.CancelAsync(tenantId, reservationId, body);
            return Ok(new { id });
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_CANCEL_ERROR", "Error al cancelar reserva"));
        }
    }

    [HttpPost("reservations/{id}/reschedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RescheduleReservation(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id,
        [FromBody] RescheduleReservationRequest body)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var reservationId)) return IdError();
        try
        {
            await reservationService.RescheduleAsync(tenantId, reservationId, body);
            return Ok(new { id });
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_RESCHEDULE_ERROR", "Error al reagendar reserva"));
        }
    }

    [HttpGet("reservations/availability")]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailability(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromQuery] string date,
        [FromQuery] int slotDurationMinutes = 60,
        [FromQuery] string? openTime = null,
        [FromQuery] string? closeTime = null)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var request = new GetAvailabilityRequest(
                DateOnly.Parse(date),
                slotDurationMinutes,
                openTime is not null ? TimeOnly.Parse(openTime) : null,
                closeTime is not null ? TimeOnly.Parse(closeTime) : null
            );
            var result = await reservationService.GetAvailabilityAsync(tenantId, request);
            return Ok(result);
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("AVAILABILITY_ERROR", "Error al consultar disponibilidad"));
        }
    }

    [HttpGet("reservations/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReservationById(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var reservationId)) return IdError();
        try
        {
            var result = await reservationService.GetByIdAsync(tenantId, reservationId);
            return result.Data is null
                ? NotFound(ApiResponse<object>.Fail("RESERVATION_NOT_FOUND", "Reserva no encontrada"))
                : Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_GET_ERROR", "Error al obtener reserva"));
        }
    }

    [HttpGet("customers/{id}/reservations")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReservationDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReservationsByCustomer(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var customerId)) return IdError();
        try
        {
            var result = await reservationService.GetByCustomerAsync(tenantId, customerId, page, pageSize);
            return Ok(result);
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_BY_CUSTOMER_ERROR", "Error al obtener reservas del cliente"));
        }
    }

    // ── Solo admin ─────────────────────────────────────────────────────────

    [HttpPost("reservations/{id}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmReservation(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromHeader(Name = "x-role")] string? role,
        string id)
    {
        if (role != "admin") return AdminOnly();
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var reservationId)) return IdError();
        try
        {
            await reservationService.ConfirmAsync(tenantId, reservationId);
            return Ok(new { id });
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_CONFIRM_ERROR", "Error al confirmar reserva"));
        }
    }

    [HttpPost("reservations/{id}/arrived")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkArrived(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromHeader(Name = "x-role")] string? role,
        string id)
    {
        if (role != "admin") return AdminOnly();
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var reservationId)) return IdError();
        try
        {
            await reservationService.MarkArrivedAsync(tenantId, reservationId);
            return Ok(new { id });
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_ARRIVED_ERROR", "Error al registrar llegada"));
        }
    }

    [HttpPost("reservations/{id}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteReservation(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromHeader(Name = "x-role")] string? role,
        string id)
    {
        if (role != "admin") return AdminOnly();
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (!Guid.TryParse(id, out var reservationId)) return IdError();
        try
        {
            await reservationService.CompleteAsync(tenantId, reservationId);
            return Ok(new { id });
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_COMPLETE_ERROR", "Error al completar reserva"));
        }
    }

    [HttpGet("agenda")]
    [ProducesResponseType(typeof(ApiResponse<AgendaDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAgenda(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromHeader(Name = "x-role")] string? role,
        [FromQuery] string date)
    {
        if (role != "admin") return AdminOnly();
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var result = await reservationService.GetAgendaAsync(tenantId, DateOnly.Parse(date));
            return Ok(result);
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("AGENDA_ERROR", "Error al obtener agenda"));
        }
    }

    [HttpGet("reservations")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReservationDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListReservations(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromHeader(Name = "x-role")] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        if (role != "admin") return AdminOnly();
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        if (page < 1)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'page' debe ser mayor o igual a 1."));
        if (pageSize < 1 || pageSize > 100)
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", "El parámetro 'pageSize' debe estar entre 1 y 100."));
        try
        {
            var result = await reservationService.ListAsync(tenantId, page, pageSize, status);
            return Ok(result);
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_LIST_ERROR", "Error al listar reservas"));
        }
    }

    [HttpGet("reservations/summary")]
    [ProducesResponseType(typeof(ApiResponse<ReservationSummaryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSummary(
        [FromHeader(Name = "x-tenant-id")] string? tenantHeader,
        [FromHeader(Name = "x-role")] string? role,
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (role != "admin") return AdminOnly();
        if (!Guid.TryParse(tenantHeader, out var tenantId)) return TenantError();
        try
        {
            var result = await reservationService.GetSummaryAsync(tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
            return Ok(result);
        }
        catch (DomainException ex) { return BadRequest(ApiResponse<object>.Fail("DOMAIN_ERROR", ex.Message)); }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("RESERVATION_SUMMARY_ERROR", "Error al obtener resumen"));
        }
    }
}
