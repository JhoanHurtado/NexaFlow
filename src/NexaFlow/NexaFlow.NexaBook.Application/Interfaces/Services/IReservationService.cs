using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Records.Create;

namespace NexaFlow.NexaBook.Application.Interfaces.Services
{
    public interface IReservationService
    {
        // ── Operaciones compartidas (cliente y admin) ──────────────────────────

        /// <summary>
        /// Crea una reserva en estado pending.
        /// Usado tanto por el cliente (auto-reserva) como por el admin (reserva a nombre de un cliente).
        /// El mismo endpoint sirve para ambos casos — el rol se pasa en el header <c>x-role</c>.
        /// </summary>
        Task<Guid> CreateAsync(Guid tenantId, CreateReservationRequest request);

        /// <summary>
        /// Cancela una reserva.
        /// El cliente solo puede cancelar sus propias reservas.
        /// El admin puede cancelar cualquier reserva del tenant.
        /// </summary>
        Task CancelAsync(Guid tenantId, Guid reservationId, CancelReservationRequest request);

        /// <summary>
        /// Reagenda una reserva a nueva fecha y hora.
        /// Disponible para cliente y admin. Valida conflicto de horario.
        /// </summary>
        Task RescheduleAsync(Guid tenantId, Guid reservationId, RescheduleReservationRequest request);

        /// <summary>
        /// Consulta los horarios disponibles para una fecha.
        /// Usado por el cliente antes de crear una reserva para saber qué slots están libres.
        /// </summary>
        Task<ApiResponse<AvailabilityDTO>> GetAvailabilityAsync(Guid tenantId, GetAvailabilityRequest request);

        /// <summary>Obtiene una reserva por ID con el nombre del cliente.</summary>
        Task<ApiResponse<ReservationDTO?>> GetByIdAsync(Guid tenantId, Guid reservationId);

        /// <summary>
        /// Retorna las reservas de un cliente específico.
        /// El cliente usa este endpoint para ver su historial de reservas.
        /// </summary>
        Task<ApiResponse<IEnumerable<ReservationDTO>>> GetByCustomerAsync(Guid tenantId, Guid customerId, int page, int pageSize);

        // ── Solo admin ─────────────────────────────────────────────────────────

        /// <summary>
        /// Confirma una reserva pendiente.
        /// Solo el admin/staff puede confirmar. Notifica al cliente.
        /// </summary>
        Task ConfirmAsync(Guid tenantId, Guid reservationId);

        /// <summary>
        /// Registra la llegada del cliente al local.
        /// Solo el admin/staff puede marcar este estado.
        /// </summary>
        Task MarkArrivedAsync(Guid tenantId, Guid reservationId);

        /// <summary>
        /// Completa la reserva (servicio prestado).
        /// Solo el admin/staff puede completar. Puede disparar creación de venta en NexaPOS.
        /// </summary>
        Task CompleteAsync(Guid tenantId, Guid reservationId);

        /// <summary>
        /// Retorna la agenda del día para el admin.
        /// Incluye todas las reservas del día con estadísticas de estado.
        /// </summary>
        Task<ApiResponse<AgendaDTO>> GetAgendaAsync(Guid tenantId, DateOnly date);

        /// <summary>
        /// Lista todas las reservas del tenant con paginación y filtro opcional por estado.
        /// Solo para admin. Permite ver la agenda completa.
        /// </summary>
        Task<ApiResponse<IEnumerable<ReservationDTO>>> ListAsync(Guid tenantId, int page, int pageSize, string? status = null);

        /// <summary>
        /// Retorna un resumen de reservas para un rango de fechas.
        /// Solo para admin. Útil para métricas y reportes.
        /// </summary>
        Task<ApiResponse<ReservationSummaryDTO>> GetSummaryAsync(Guid tenantId, DateOnly from, DateOnly to);
    }
}
