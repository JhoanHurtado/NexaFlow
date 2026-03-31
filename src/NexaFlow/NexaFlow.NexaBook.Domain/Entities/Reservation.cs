using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.Domain.Entities
{
    /// <summary>
    /// Estados válidos del ciclo de vida de una reserva.
    /// </summary>
    public enum ReservationStatus
    {
        /// <summary>Creada, pendiente de confirmación por el tenant.</summary>
        Pending,
        /// <summary>Confirmada por el tenant.</summary>
        Confirmed,
        /// <summary>Cancelada por el cliente o el tenant.</summary>
        Cancelled,
        /// <summary>El cliente llegó al local.</summary>
        Arrived,
        /// <summary>La reserva fue completada (servicio prestado).</summary>
        Completed
    }

    /// <summary>
    /// Representa una reserva de un cliente para un tenant.
    /// Gestiona el ciclo de vida completo: pending → confirmed → arrived → completed.
    /// Una reserva cancelada no puede ser reactivada.
    /// Una reserva completada no puede cambiar de estado.
    /// </summary>
    public class Reservation
    {
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid CustomerId { get; private set; }
        public DateOnly ReservationDate { get; private set; }
        public TimeOnly TimeSlot { get; private set; }
        public ReservationStatus Status { get; private set; }
        public string? Notes { get; private set; }
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Reconstruction constructor for loading from persistence. Bypasses business rule validations.
        /// </summary>
        public Reservation(Guid id, Guid tenantId, Guid customerId, DateOnly reservationDate, TimeOnly timeSlot, ReservationStatus status, DateTime createdAt)
        {
            Id = id;
            TenantId = tenantId;
            CustomerId = customerId;
            ReservationDate = reservationDate;
            TimeSlot = timeSlot;
            Status = status;
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Crea una nueva reserva en estado <see cref="ReservationStatus.Pending"/>.
        /// </summary>
        /// <param name="tenantId">Tenant propietario.</param>
        /// <param name="customerId">Cliente que realiza la reserva.</param>
        /// <param name="reservationDate">Fecha de la reserva. No puede ser en el pasado.</param>
        /// <param name="timeSlot">Hora del turno.</param>
        /// <param name="notes">Notas opcionales del cliente.</param>
        /// <exception cref="DomainException">Si alguna regla de negocio es violada.</exception>
        public Reservation(Guid tenantId, Guid customerId, DateOnly reservationDate, TimeOnly timeSlot, string? notes = null)
        {
            if (tenantId == Guid.Empty)
                throw new DomainException("El tenant es requerido.");
            if (customerId == Guid.Empty)
                throw new DomainException("El cliente es requerido.");
            if (reservationDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
                throw new DomainException("La fecha de reserva no puede ser en el pasado.");
            if (notes is not null && notes.Length > 500)
                throw new DomainException("Las notas no pueden superar 500 caracteres.");

            Id = Guid.NewGuid();
            TenantId = tenantId;
            CustomerId = customerId;
            ReservationDate = reservationDate;
            TimeSlot = timeSlot;
            Status = ReservationStatus.Pending;
            Notes = notes?.Trim();
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Confirma la reserva. Solo puede confirmarse si está en estado <see cref="ReservationStatus.Pending"/>.
        /// </summary>
        public void Confirm()
        {
            if (Status == ReservationStatus.Cancelled)
                throw new DomainException("No se puede confirmar una reserva cancelada.");
            if (Status == ReservationStatus.Completed)
                throw new DomainException("No se puede confirmar una reserva completada.");
            if (Status == ReservationStatus.Confirmed)
                throw new DomainException("La reserva ya está confirmada.");

            Status = ReservationStatus.Confirmed;
        }

        /// <summary>
        /// Cancela la reserva. No se puede cancelar si ya fue completada.
        /// </summary>
        public void Cancel()
        {
            if (Status == ReservationStatus.Completed)
                throw new DomainException("No se puede cancelar una reserva completada.");
            if (Status == ReservationStatus.Cancelled)
                throw new DomainException("La reserva ya está cancelada.");

            Status = ReservationStatus.Cancelled;
        }

        /// <summary>
        /// Registra la llegada del cliente. Solo puede marcarse si está confirmada.
        /// </summary>
        public void MarkArrived()
        {
            if (Status != ReservationStatus.Confirmed)
                throw new DomainException("Solo se puede registrar la llegada de una reserva confirmada.");

            Status = ReservationStatus.Arrived;
        }

        /// <summary>
        /// Completa la reserva. Solo puede completarse si el cliente llegó.
        /// </summary>
        public void Complete()
        {
            if (Status != ReservationStatus.Arrived)
                throw new DomainException("Solo se puede completar una reserva en estado 'arrived'.");

            Status = ReservationStatus.Completed;
        }

        /// <summary>
        /// Reagenda la reserva a una nueva fecha y hora.
        /// Solo puede reagendarse si está en estado pending o confirmed.
        /// </summary>
        public void Reschedule(DateOnly newDate, TimeOnly newTimeSlot)
        {
            if (Status == ReservationStatus.Cancelled)
                throw new DomainException("No se puede reagendar una reserva cancelada.");
            if (Status == ReservationStatus.Completed)
                throw new DomainException("No se puede reagendar una reserva completada.");
            if (Status == ReservationStatus.Arrived)
                throw new DomainException("No se puede reagendar una reserva en curso.");
            if (newDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
                throw new DomainException("La nueva fecha no puede ser en el pasado.");

            ReservationDate = newDate;
            TimeSlot = newTimeSlot;
            Status = ReservationStatus.Pending;
        }
    }
}
