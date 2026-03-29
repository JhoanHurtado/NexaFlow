using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Records.Create;
using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.Handlers
{
    public class ReservationHandler(IReservationService reservationService)
    {
        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations")]
        public async Task<IHttpResult> CreateReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromBody] CreateReservationRequest body,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            try
            {
                var id = await reservationService.CreateAsync(tenantId, body);
                return Api.Created($"/reservations/{id}", new IdResponse(id));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Create] {ex.Message}");
                return Api.InternalServerError("RESERVATION_CREATE_ERROR", "Error al crear reserva");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/cancel")]
        public async Task<IHttpResult> CancelReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] CancelReservationRequest body,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var reservationId, out var ie)) return ie!;
            try
            {
                await reservationService.CancelAsync(tenantId, reservationId, body);
                return Api.Ok(new IdResponse(reservationId));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Cancel] {ex.Message}");
                return Api.InternalServerError("RESERVATION_CANCEL_ERROR", "Error al cancelar reserva");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/reschedule")]
        public async Task<IHttpResult> RescheduleReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            [FromBody] RescheduleReservationRequest body,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var reservationId, out var ie)) return ie!;
            try
            {
                await reservationService.RescheduleAsync(tenantId, reservationId, body);
                return Api.Ok(new IdResponse(reservationId));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Reschedule] {ex.Message}");
                return Api.InternalServerError("RESERVATION_RESCHEDULE_ERROR", "Error al reagendar reserva");
            }
        }

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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            try
            {
                var request = new GetAvailabilityRequest(
                    DateOnly.Parse(date),
                    slotDurationMinutes,
                    openTime is not null ? TimeOnly.Parse(openTime) : null,
                    closeTime is not null ? TimeOnly.Parse(closeTime) : null
                );
                var result = await reservationService.GetAvailabilityAsync(tenantId, request);
                return Api.Ok(result);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Availability] {ex.Message}");
                return Api.InternalServerError("AVAILABILITY_ERROR", "Error al consultar disponibilidad");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/reservations/{id}")]
        public async Task<IHttpResult> GetReservationById(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            ILambdaContext context)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var reservationId, out var ie)) return ie!;
            try
            {
                var result = await reservationService.GetByIdAsync(tenantId, reservationId);
                return result.Data is null
                    ? Api.NotFound("RESERVATION_NOT_FOUND", "Reserva no encontrada")
                    : Api.Ok(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.GetById] {ex.Message}");
                return Api.InternalServerError("RESERVATION_GET_ERROR", "Error al obtener reserva");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/customers/{id}/reservations")]
        public async Task<IHttpResult> GetReservationsByCustomer(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            string id,
            ILambdaContext context,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var customerId, out var ie)) return ie!;
            try
            {
                var result = await reservationService.GetByCustomerAsync(tenantId, customerId, page, pageSize);
                return Api.Ok(result);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.ByCustomer] {ex.Message}");
                return Api.InternalServerError("RESERVATION_BY_CUSTOMER_ERROR", "Error al obtener reservas del cliente");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/confirm")]
        public async Task<IHttpResult> ConfirmReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            string id,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var reservationId, out var ie)) return ie!;
            try
            {
                await reservationService.ConfirmAsync(tenantId, reservationId);
                return Api.Ok(new IdResponse(reservationId));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Confirm] {ex.Message}");
                return Api.InternalServerError("RESERVATION_CONFIRM_ERROR", "Error al confirmar reserva");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/arrived")]
        public async Task<IHttpResult> MarkArrived(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            string id,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var reservationId, out var ie)) return ie!;
            try
            {
                await reservationService.MarkArrivedAsync(tenantId, reservationId);
                return Api.Ok(new IdResponse(reservationId));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Arrived] {ex.Message}");
                return Api.InternalServerError("RESERVATION_ARRIVED_ERROR", "Error al registrar llegada");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Post, "/reservations/{id}/complete")]
        public async Task<IHttpResult> CompleteReservation(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            string id,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (!Validate.TryParseGuid(id, "id", out var reservationId, out var ie)) return ie!;
            try
            {
                await reservationService.CompleteAsync(tenantId, reservationId);
                return Api.Ok(new IdResponse(reservationId));
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Complete] {ex.Message}");
                return Api.InternalServerError("RESERVATION_COMPLETE_ERROR", "Error al completar reserva");
            }
        }

        [LambdaFunction]
        [RestApi(LambdaHttpMethod.Get, "/agenda")]
        public async Task<IHttpResult> GetAgenda(
            [FromHeader(Name = "x-tenant-id")] string tenantHeader,
            [FromHeader(Name = "x-role")] string role,
            [FromQuery] string date,
            ILambdaContext context)
        {
            if (role != "admin") return HttpResults.Forbid();
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            try
            {
                var result = await reservationService.GetAgendaAsync(tenantId, DateOnly.Parse(date));
                return Api.Ok(result);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Agenda] {ex.Message}");
                return Api.InternalServerError("AGENDA_ERROR", "Error al obtener agenda");
            }
        }

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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            if (page < 1) return Api.BadRequest("VALIDATION_ERROR", "El parámetro 'page' debe ser mayor o igual a 1.");
            if (pageSize < 1 || pageSize > 100) return Api.BadRequest("VALIDATION_ERROR", "El parámetro 'pageSize' debe estar entre 1 y 100.");
            try
            {
                var result = await reservationService.ListAsync(tenantId, page, pageSize, status);
                return Api.Ok(result);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.List] {ex.Message}");
                return Api.InternalServerError("RESERVATION_LIST_ERROR", "Error al listar reservas");
            }
        }

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
            if (!Validate.TryParseGuid(tenantHeader, "x-tenant-id", out var tenantId, out var ve)) return ve!;
            try
            {
                var result = await reservationService.GetSummaryAsync(tenantId, DateOnly.Parse(from), DateOnly.Parse(to));
                return Api.Ok(result);
            }
            catch (DomainException ex) { return Api.BadRequest("DOMAIN_ERROR", ex.Message); }
            catch (Exception ex)
            {
                context.Logger.LogError($"[ReservationHandler.Summary] {ex.Message}");
                return Api.InternalServerError("RESERVATION_SUMMARY_ERROR", "Error al obtener resumen");
            }
        }
    }
}
