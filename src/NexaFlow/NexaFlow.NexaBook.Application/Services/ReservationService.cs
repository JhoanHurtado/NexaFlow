using NexaFlow.NexaBook.Application.Dto;
using NexaFlow.NexaBook.Application.Interfaces.Events;
using NexaFlow.NexaBook.Application.Interfaces.Repositories;
using NexaFlow.NexaBook.Application.Interfaces.Services;
using NexaFlow.NexaBook.Application.Interfaces.UnitOfWork;
using NexaFlow.NexaBook.Application.Records.Create;
using NexaFlow.NexaBook.Domain.Entities;
using NexaFlow.NexaBook.Domain.Events;
using NexaFlow.NexaBook.Domain.Exceptions;

namespace NexaFlow.NexaBook.Application.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _repository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _uow;
        private readonly IPosLogger _logger;

        public ReservationService(IReservationRepository repository, ICustomerRepository customerRepository, IUnitOfWork uow, IPosLogger logger)
        {
            _repository = repository;
            _customerRepository = customerRepository;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Guid> CreateAsync(Guid tenantId, CreateReservationRequest request)
        {
            if (request.CustomerId == Guid.Empty)
                throw new DomainException("El cliente es requerido.");

            _ = await _customerRepository.GetByIdAsync(tenantId, request.CustomerId)
                ?? throw new DomainException($"Cliente {request.CustomerId} no encontrado.");

            var conflict = await _repository.ExistsConflictAsync(tenantId, request.ReservationDate, request.TimeSlot);
            if (conflict)
                throw new DomainException($"Ya existe una reserva para el {request.ReservationDate} a las {request.TimeSlot}.");

            _logger.Info($"[Reservation] Creando reserva para cliente {request.CustomerId} el {request.ReservationDate} {request.TimeSlot}");

            var reservation = new Reservation(tenantId, request.CustomerId, request.ReservationDate, request.TimeSlot, request.Notes);

            await _uow.BeginAsync(tenantId);
            try
            {
                await _uow.SaveReservationAsync(reservation);
                await _uow.EnqueueEventAsync(new ReservationCreatedEvent(tenantId, reservation.Id, reservation.CustomerId, reservation.ReservationDate, reservation.TimeSlot));
                await _uow.CommitAsync();
            }
            catch { await _uow.RollbackAsync(); throw; }

            _logger.Info($"[Reservation] Reserva {reservation.Id} creada.");
            return reservation.Id;
        }

        public async Task ConfirmAsync(Guid tenantId, Guid reservationId)
        {
            var reservation = await GetOrThrowAsync(tenantId, reservationId);
            reservation.Confirm();
            await SaveAndEnqueueAsync(tenantId, reservation, new ReservationConfirmedEvent(tenantId, reservation.Id, reservation.CustomerId, reservation.ReservationDate, reservation.TimeSlot));
            _logger.Info($"[Reservation] Reserva {reservationId} confirmada.");
        }

        public async Task CancelAsync(Guid tenantId, Guid reservationId, CancelReservationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CancelledBy))
                throw new DomainException("Debe indicarse quién cancela la reserva.");

            var reservation = await GetOrThrowAsync(tenantId, reservationId);
            reservation.Cancel();
            await SaveAndEnqueueAsync(tenantId, reservation, new ReservationCancelledEvent(tenantId, reservation.Id, reservation.CustomerId, request.CancelledBy));
            _logger.Info($"[Reservation] Reserva {reservationId} cancelada por {request.CancelledBy}.");
        }

        public async Task MarkArrivedAsync(Guid tenantId, Guid reservationId)
        {
            var reservation = await GetOrThrowAsync(tenantId, reservationId);
            reservation.MarkArrived();
            await SaveAndEnqueueAsync(tenantId, reservation, new ReservationArrivedEvent(tenantId, reservation.Id, reservation.CustomerId));
            _logger.Info($"[Reservation] Cliente llegó para reserva {reservationId}.");
        }

        public async Task CompleteAsync(Guid tenantId, Guid reservationId)
        {
            var reservation = await GetOrThrowAsync(tenantId, reservationId);
            reservation.Complete();
            await SaveAndEnqueueAsync(tenantId, reservation, new ReservationCompletedEvent(tenantId, reservation.Id, reservation.CustomerId, reservation.ReservationDate));
            _logger.Info($"[Reservation] Reserva {reservationId} completada.");
        }

        public async Task RescheduleAsync(Guid tenantId, Guid reservationId, RescheduleReservationRequest request)
        {
            var reservation = await GetOrThrowAsync(tenantId, reservationId);

            var conflict = await _repository.ExistsConflictAsync(tenantId, request.NewDate, request.NewTimeSlot, excludeReservationId: reservationId);
            if (conflict)
                throw new DomainException($"Ya existe una reserva para el {request.NewDate} a las {request.NewTimeSlot}.");

            reservation.Reschedule(request.NewDate, request.NewTimeSlot);
            await SaveAndEnqueueAsync(tenantId, reservation, new ReservationRescheduledEvent(tenantId, reservation.Id, reservation.CustomerId, reservation.ReservationDate, reservation.TimeSlot));
            _logger.Info($"[Reservation] Reserva {reservationId} reagendada a {request.NewDate} {request.NewTimeSlot}.");
        }

        public async Task<ApiResponse<AvailabilityDTO>> GetAvailabilityAsync(Guid tenantId, GetAvailabilityRequest request)
        {
            if (request.Date < DateOnly.FromDateTime(DateTime.UtcNow.Date))
                throw new DomainException("No se puede consultar disponibilidad para fechas pasadas.");

            var open  = request.OpenTime  ?? new TimeOnly(8, 0);
            var close = request.CloseTime ?? new TimeOnly(20, 0);
            var duration = request.SlotDurationMinutes < 15 ? 15 : request.SlotDurationMinutes;

            var takenSlots = (await _repository.GetByDateAsync(tenantId, request.Date))
                .Where(r => r.Status is ReservationStatus.Pending or ReservationStatus.Confirmed)
                .Select(r => r.TimeSlot)
                .ToHashSet();

            var slots = new List<TimeSlotDTO>();
            var current = open;
            while (current < close)
            {
                slots.Add(new TimeSlotDTO(current, !takenSlots.Contains(current)));
                current = current.AddMinutes(duration);
            }

            return ApiResponse<AvailabilityDTO>.Ok(new AvailabilityDTO(request.Date, slots));
        }

        public async Task<ApiResponse<ReservationDTO?>> GetByIdAsync(Guid tenantId, Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                throw new DomainException("El id de la reserva es requerido.");

            var reservation = await _repository.GetByIdAsync(tenantId, reservationId);
            if (reservation is null)
            {
                _logger.Warning($"[Reservation] Reserva {reservationId} no encontrada.");
                return ApiResponse<ReservationDTO?>.Ok(null);
            }

            var customer = await _customerRepository.GetByIdAsync(tenantId, reservation.CustomerId);
            return ApiResponse<ReservationDTO?>.Ok(MapToDto(new ReservationWithCustomer(reservation, customer?.Name ?? "Desconocido")));
        }

        public async Task<ApiResponse<IEnumerable<ReservationDTO>>> ListAsync(Guid tenantId, int page, int pageSize, string? status = null)
        {
            if (page < 1) throw new DomainException("La página debe ser mayor a cero.");
            if (pageSize < 1 || pageSize > 100) throw new DomainException("El tamaño de página debe estar entre 1 y 100.");

            var (items, total) = await _repository.GetPagedAsync(tenantId, page, pageSize, status);
            return ApiResponse<IEnumerable<ReservationDTO>>.Ok(items.Select(MapToDto), new PaginationMetadata(page, pageSize, total));
        }

        public async Task<ApiResponse<IEnumerable<ReservationDTO>>> GetByCustomerAsync(Guid tenantId, Guid customerId, int page, int pageSize)
        {
            if (page < 1) throw new DomainException("La página debe ser mayor a cero.");
            if (pageSize < 1 || pageSize > 100) throw new DomainException("El tamaño de página debe estar entre 1 y 100.");

            var (items, total) = await _repository.GetByCustomerAsync(tenantId, customerId, page, pageSize);
            return ApiResponse<IEnumerable<ReservationDTO>>.Ok(items.Select(MapToDto), new PaginationMetadata(page, pageSize, total));
        }

        public async Task<ApiResponse<AgendaDTO>> GetAgendaAsync(Guid tenantId, DateOnly date)
        {
            var reservations = await _repository.GetByDateAsync(tenantId, date);
            var customers = new Dictionary<Guid, string>();
            var dtos = new List<ReservationDTO>();

            foreach (var r in reservations)
            {
                if (!customers.TryGetValue(r.CustomerId, out var name))
                {
                    var c = await _customerRepository.GetByIdAsync(tenantId, r.CustomerId);
                    name = c?.Name ?? "Desconocido";
                    customers[r.CustomerId] = name;
                }
                dtos.Add(MapToDto(new ReservationWithCustomer(r, name)));
            }

            var agenda = new AgendaDTO(
                date,
                dtos.Count,
                dtos.Count(r => r.Status == "pending"),
                dtos.Count(r => r.Status == "confirmed"),
                dtos.Count(r => r.Status == "arrived"),
                dtos.Count(r => r.Status == "completed"),
                dtos.Count(r => r.Status == "cancelled"),
                dtos.OrderBy(r => r.TimeSlot)
            );

            return ApiResponse<AgendaDTO>.Ok(agenda);
        }

        public async Task<ApiResponse<ReservationSummaryDTO>> GetSummaryAsync(Guid tenantId, DateOnly from, DateOnly to)
        {
            if (from > to)
                throw new DomainException("La fecha de inicio no puede ser mayor a la fecha de fin.");

            var counts = await _repository.GetStatusCountsAsync(tenantId, from, to);

            int Get(string key) => counts.TryGetValue(key, out var v) ? v : 0;
            var total = counts.Values.Sum();

            return ApiResponse<ReservationSummaryDTO>.Ok(new ReservationSummaryDTO(
                from, to, total,
                Get("pending"), Get("confirmed"), Get("arrived"), Get("completed"), Get("cancelled")
            ));
        }

        private async Task SaveAndEnqueueAsync(Guid tenantId, Reservation reservation, Domain.Events.DomainEvent evt)
        {
            await _uow.BeginAsync(tenantId);
            try
            {
                await _uow.UpdateReservationAsync(reservation);
                await _uow.EnqueueEventAsync(evt);
                await _uow.CommitAsync();
            }
            catch { await _uow.RollbackAsync(); throw; }
        }

        private async Task<Reservation> GetOrThrowAsync(Guid tenantId, Guid reservationId)
        {
            if (reservationId == Guid.Empty)
                throw new DomainException("El id de la reserva es requerido.");
            return await _repository.GetByIdAsync(tenantId, reservationId)
                ?? throw new DomainException($"Reserva {reservationId} no encontrada.");
        }

        private static ReservationDTO MapToDto(ReservationWithCustomer r) => new(
            r.Reservation.Id, r.Reservation.TenantId, r.Reservation.CustomerId,
            r.CustomerName, r.Reservation.ReservationDate, r.Reservation.TimeSlot,
            r.Reservation.Status.ToString().ToLowerInvariant(), r.Reservation.Notes, r.Reservation.CreatedAt
        );
    }
}
