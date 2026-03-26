using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Records.Create;
using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.Handlers
{
    /// <summary>
    /// Handler Lambda para el ciclo de vida completo de reservas.
    /// Headers requeridos en todos los requests:
    ///   - x-tenant-id : UUID del tenant
    ///   - x-role      : "admin" | "customer"  (controla qué operaciones están permitidas)
    /// </summary>
    public class ReservationHandler
    {
        private readonly IReservationService _reservationService;

        public ReservationHandler(IReservationService reservationService) => _reservationService = reservationService;

        // ─────────────────────────────────────────────────────────────────────
        // COMPARTIDOS — cliente y admin
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Crea una reserva en estado pending.
        ///
        /// Uso cliente  : el cliente envía su propio customerId y elige fecha/hora disponible.
        /// Uso admin    : el admin puede crear una reserva a nombre de cualquier cliente del tenant.
        /// El mismo endpoint sirve para ambos casos — el rol no restringe esta operación.
        ///
        /// POST /reservations
        /// Headers: x-tenant-id, x-role
        /// Body: { customerId, reservationDate (yyyy-MM-dd), timeSlot (HH:mm), notes? }
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations")]
        public async Task<IHttpResult> CreateReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateReservationRequest body,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var id = await _reservationService.CreateAsync(tenantId, body);
                return HttpResults.Created($"/reservations/{id}", new { id });
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Create] {ex.Message}");
                return HttpResults.InternalServerError("Error al crear reserva");
            }
        }

        /// <summary>
        /// Cancela una reserva.
        ///
        /// Uso cliente : solo puede cancelar sus propias reservas (validar customerId en el frontend).
        /// Uso admin   : puede cancelar cualquier reserva del tenant.
        /// Ambos deben enviar CancelledBy para auditoría.
        ///
        /// POST /reservations/{id}/cancel
        /// Headers: x-tenant-id, x-role
        /// Body: { cancelledBy: "nombre o id de quien cancela" }
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/cancel")]
        public async Task<IHttpResult> CancelReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] CancelReservationRequest body,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                await _reservationService.CancelAsync(tenantId, Guid.Parse(id), body);
                return HttpResults.Ok(new { id });
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Cancel] {ex.Message}");
                return HttpResults.InternalServerError("Error al cancelar reserva");
            }
        }

        /// <summary>
        /// Reagenda una reserva a nueva fecha y hora.
        ///
        /// Uso cliente : puede reagendar sus propias reservas (solo pending o confirmed).
        /// Uso admin   : puede reagendar cualquier reserva del tenant.
        /// Valida que no exista conflicto de horario en la nueva fecha.
        ///
        /// POST /reservations/{id}/reschedule
        /// Headers: x-tenant-id, x-role
        /// Body: { newDate (yyyy-MM-dd), newTimeSlot (HH:mm) }
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/reschedule")]
        public async Task<IHttpResult> RescheduleReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] RescheduleReservationRequest body,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                await _reservationService.RescheduleAsync(tenantId, Guid.Parse(id), body);
                return HttpResults.Ok(new { id });
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Reschedule] {ex.Message}");
                return HttpResults.InternalServerError("Error al reagendar reserva");
            }
        }

        /// <summary>
        /// Consulta los horarios disponibles para una fecha.
        ///
        /// Uso cliente : lo usa antes de crear una reserva para saber qué slots están libres.
        /// Uso admin   : lo usa para ver disponibilidad al crear una reserva para un cliente.
        ///
        /// GET /reservations/availability?date=2025-04-01&amp;slotDurationMinutes=60&amp;openTime=08:00&amp;closeTime=20:00
        /// Headers: x-tenant-id
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/reservations/availability")]
        public async Task<IHttpResult> GetAvailability(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromQuery] string date,
            ILambdaContext context,
            [FromQuery] int slotDurationMinutes = 60,
            [FromQuery] string? openTime = null,
            [FromQuery] string? closeTime = null)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var request = new GetAvailabilityRequest(
                    DateOnly.Parse(date),
                    slotDurationMinutes,
                    openTime is not null ? TimeOnly.Parse(openTime) : null,
                    closeTime is not null ? TimeOnly.Parse(closeTime) : null
                );
                var result = await _reservationService.GetAvailabilityAsync(tenantId, request);
                return HttpResults.Ok(result);
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Availability] {ex.Message}");
                return HttpResults.InternalServerError("Error al consultar disponibilidad");
            }
        }

        /// <summary>
        /// Obtiene una reserva por ID.
        ///
        /// Uso cliente : puede ver el detalle de su propia reserva.
        /// Uso admin   : puede ver cualquier reserva del tenant.
        ///
        /// GET /reservations/{id}
        /// Headers: x-tenant-id
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/reservations/{id}")]
        public async Task<IHttpResult> GetReservationById(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            ILambdaContext context)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _reservationService.GetByIdAsync(tenantId, Guid.Parse(id));
                return result.Data is null ? HttpResults.NotFound() : HttpResults.Ok(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.GetById] {ex.Message}");
                return HttpResults.InternalServerError("Error al obtener reserva");
            }
        }

        /// <summary>
        /// Retorna las reservas de un cliente específico.
        ///
        /// Uso cliente : el cliente consulta su propio historial pasando su customerId.
        /// Uso admin   : puede consultar el historial de cualquier cliente del tenant.
        ///
        /// GET /customers/{customerId}/reservations?page=1&amp;pageSize=10
        /// Headers: x-tenant-id
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/customers/{customerId}/reservations")]
        public async Task<IHttpResult> GetReservationsByCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string customerId,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _reservationService.GetByCustomerAsync(tenantId, Guid.Parse(customerId), page, pageSize);
                return HttpResults.Ok(result);
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.ByCustomer] {ex.Message}");
                return HttpResults.InternalServerError("Error al obtener reservas del cliente");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // SOLO ADMIN
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Confirma una reserva pendiente. SOLO ADMIN.
        ///
        /// El admin revisa las reservas pendientes y las confirma.
        /// Esto notifica al cliente que su reserva fue aceptada.
        ///
        /// POST /reservations/{id}/confirm
        /// Headers: x-tenant-id, x-role: admin
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/confirm")]
        public async Task<IHttpResult> ConfirmReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            string id,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                await _reservationService.ConfirmAsync(tenantId, Guid.Parse(id));
                return HttpResults.Ok(new { id });
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Confirm] {ex.Message}");
                return HttpResults.InternalServerError("Error al confirmar reserva");
            }
        }

        /// <summary>
        /// Registra la llegada del cliente al local. SOLO ADMIN.
        ///
        /// El staff marca este estado cuando el cliente se presenta físicamente.
        /// Solo puede marcarse si la reserva está confirmada.
        ///
        /// POST /reservations/{id}/arrived
        /// Headers: x-tenant-id, x-role: admin
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/arrived")]
        public async Task<IHttpResult> MarkArrived(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            string id,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                await _reservationService.MarkArrivedAsync(tenantId, Guid.Parse(id));
                return HttpResults.Ok(new { id });
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Arrived] {ex.Message}");
                return HttpResults.InternalServerError("Error al registrar llegada");
            }
        }

        /// <summary>
        /// Completa la reserva (servicio prestado). SOLO ADMIN.
        ///
        /// El staff marca este estado al finalizar el servicio.
        /// Solo puede completarse si el cliente llegó (estado arrived).
        /// Puede disparar la creación de una venta en NexaPOS.
        ///
        /// POST /reservations/{id}/complete
        /// Headers: x-tenant-id, x-role: admin
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/complete")]
        public async Task<IHttpResult> CompleteReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            string id,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                await _reservationService.CompleteAsync(tenantId, Guid.Parse(id));
                return HttpResults.Ok(new { id });
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Complete] {ex.Message}");
                return HttpResults.InternalServerError("Error al completar reserva");
            }
        }

        /// <summary>
        /// Retorna la agenda del día para el admin. SOLO ADMIN.
        ///
        /// Vista completa del día: todas las reservas ordenadas por hora con
        /// estadísticas de cuántas están en cada estado.
        /// Ideal para la pantalla principal del panel de administración.
        ///
        /// GET /agenda?date=2025-04-01
        /// Headers: x-tenant-id, x-role: admin
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/agenda")]
        public async Task<IHttpResult> GetAgenda(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            [FromQuery] string date,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _reservationService.GetAgendaAsync(tenantId, DateOnly.Parse(date));
                return HttpResults.Ok(result);
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Agenda] {ex.Message}");
                return HttpResults.InternalServerError("Error al obtener agenda");
            }
        }

        /// <summary>
        /// Lista todas las reservas del tenant con paginación. SOLO ADMIN.
        ///
        /// Permite filtrar por estado: pending, confirmed, arrived, completed, cancelled.
        /// Útil para gestionar la agenda completa o buscar reservas por estado.
        ///
        /// GET /reservations?page=1&amp;pageSize=10&amp;status=pending
        /// Headers: x-tenant-id, x-role: admin
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/reservations")]
        public async Task<IHttpResult> ListReservations(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            if (role != "admin") return HttpResults.Forbid();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _reservationService.ListAsync(tenantId, page, pageSize, status);
                return HttpResults.Ok(result);
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.List] {ex.Message}");
                return HttpResults.InternalServerError("Error al listar reservas");
            }
        }

        /// <summary>
        /// Retorna un resumen de reservas para un rango de fechas. SOLO ADMIN.
        ///
        /// Muestra totales por estado para el rango indicado.
        /// Útil para métricas, reportes y vista de semana/mes en el panel.
        ///
        /// GET /reservations/summary?from=2025-04-01&amp;to=2025-04-30
        /// Headers: x-tenant-id, x-role: admin
        /// </summary>
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/reservations/summary")]
        public async Task<IHttpResult> GetSummary(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            [FromQuery] string from,
            [FromQuery] string to,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            try
            {
                var tenantId = Guid.Parse(tenantHeader);
                var result = await _reservationService.GetSummaryAsync(tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
                return HttpResults.Ok(result);
            }
            catch (DomainException ex) { return HttpResults.BadRequest(ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Summary] {ex.Message}");
                return HttpResults.InternalServerError("Error al obtener resumen");
            }
        }
    }
}
