namespace NexaFlow.NexaBook.Domain.Events
{
    /// <summary>
    /// Clase base para todos los eventos de dominio del sistema NexaBook.
    /// Los eventos se persisten en la tabla <c>pos_events</c> con <c>published = FALSE</c>
    /// dentro de la misma transacción que los origina (Outbox Pattern).
    /// Un proceso separado los lee y los envía a SQS/EventBridge.
    /// </summary>
    public abstract record DomainEvent(Guid TenantId, Guid AggregateId, string AggregateType, DateTime OccurredAt)
    {
        /// <summary>Tipo del evento. Discriminador en la tabla <c>pos_events</c>.</summary>
        public abstract string EventType { get; }
    }

    // ─────────────────────────────────────────
    // CUSTOMER EVENTS
    // ─────────────────────────────────────────

    /// <summary>
    /// Emitido cuando un cliente es registrado en NexaBook.
    /// Puede ser auto-registro del cliente o registro por el staff.
    /// </summary>
    public record CustomerRegisteredEvent(Guid TenantId, Guid CustomerId, string Name, string? Email, string? Phone, bool SelfRegistered)
        : DomainEvent(TenantId, CustomerId, "Customer", DateTime.UtcNow)
    {
        public override string EventType => "customer.registered";
    }

    /// <summary>
    /// Emitido cuando los datos de contacto de un cliente son actualizados.
    /// </summary>
    public record CustomerUpdatedEvent(Guid TenantId, Guid CustomerId, string Name, string? Email, string? Phone)
        : DomainEvent(TenantId, CustomerId, "Customer", DateTime.UtcNow)
    {
        public override string EventType => "customer.updated";
    }

    // ─────────────────────────────────────────
    // RESERVATION EVENTS
    // ─────────────────────────────────────────

    /// <summary>
    /// Emitido cuando una reserva es creada en estado pending.
    /// Puede disparar una notificación al tenant para que la confirme.
    /// </summary>
    public record ReservationCreatedEvent(Guid TenantId, Guid ReservationId, Guid CustomerId, DateOnly ReservationDate, TimeOnly TimeSlot)
        : DomainEvent(TenantId, ReservationId, "Reservation", DateTime.UtcNow)
    {
        public override string EventType => "reservation.created";
    }

    /// <summary>
    /// Emitido cuando el tenant confirma una reserva.
    /// Puede disparar una notificación de confirmación al cliente.
    /// </summary>
    public record ReservationConfirmedEvent(Guid TenantId, Guid ReservationId, Guid CustomerId, DateOnly ReservationDate, TimeOnly TimeSlot)
        : DomainEvent(TenantId, ReservationId, "Reservation", DateTime.UtcNow)
    {
        public override string EventType => "reservation.confirmed";
    }

    /// <summary>
    /// Emitido cuando una reserva es cancelada.
    /// Incluye quién canceló para auditoría.
    /// </summary>
    public record ReservationCancelledEvent(Guid TenantId, Guid ReservationId, Guid CustomerId, string CancelledBy)
        : DomainEvent(TenantId, ReservationId, "Reservation", DateTime.UtcNow)
    {
        public override string EventType => "reservation.cancelled";
    }

    /// <summary>
    /// Emitido cuando el cliente llega al local.
    /// Puede disparar una alerta al staff.
    /// </summary>
    public record ReservationArrivedEvent(Guid TenantId, Guid ReservationId, Guid CustomerId)
        : DomainEvent(TenantId, ReservationId, "Reservation", DateTime.UtcNow)
    {
        public override string EventType => "reservation.arrived";
    }

    /// <summary>
    /// Emitido cuando una reserva es completada.
    /// Puede disparar la creación de una venta en NexaPOS si aplica.
    /// </summary>
    public record ReservationCompletedEvent(Guid TenantId, Guid ReservationId, Guid CustomerId, DateOnly ReservationDate)
        : DomainEvent(TenantId, ReservationId, "Reservation", DateTime.UtcNow)
    {
        public override string EventType => "reservation.completed";
    }

    /// <summary>
    /// Emitido cuando una reserva es reagendada a una nueva fecha/hora.
    /// Puede disparar notificación al cliente con los nuevos datos.
    /// </summary>
    public record ReservationRescheduledEvent(Guid TenantId, Guid ReservationId, Guid CustomerId, DateOnly NewDate, TimeOnly NewTimeSlot)
        : DomainEvent(TenantId, ReservationId, "Reservation", DateTime.UtcNow)
    {
        public override string EventType => "reservation.rescheduled";
    }
}
