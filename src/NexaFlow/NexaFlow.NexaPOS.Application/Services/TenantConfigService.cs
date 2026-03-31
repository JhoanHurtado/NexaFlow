using NexaFlow.NexaPOS.Application.Dto;
using NexaFlow.NexaPOS.Application.Interfaces.Repositories;
using NexaFlow.NexaPOS.Application.Interfaces.Services;
using NexaFlow.NexaPOS.Domain.Entities;
using NexaFlow.NexaPOS.Domain.Exceptions;

namespace NexaFlow.NexaPOS.Application.Services
{
    public class TenantConfigService : ITenantConfigService
    {
        private readonly ITenantConfigRepository _repo;
        public TenantConfigService(ITenantConfigRepository repo) => _repo = repo;

        public async Task<ApiResponse<TenantConfigDTO>> GetAsync(Guid tenantId)
        {
            var config = await _repo.GetOrDefaultAsync(tenantId);
            return ApiResponse<TenantConfigDTO>.Ok(Map(config));
        }

        public async Task<ApiResponse<TenantConfigDTO>> UpdateAsync(Guid tenantId, UpdateTenantConfigRequest request)
        {
            if (request.TaxRate < 0 || request.TaxRate > 100)
                throw new DomainException("La tasa de IVA debe estar entre 0 y 100.");
            if (string.IsNullOrWhiteSpace(request.Currency))
                throw new DomainException("La moneda es requerida.");
            if (request.SlotDurationMinutes < 15 || request.SlotDurationMinutes > 480)
                throw new DomainException("La duración del slot debe estar entre 15 y 480 minutos.");
            if (!TimeOnly.TryParse(request.OpenTime, out var openTime))
                throw new DomainException("Hora de apertura inválida. Use formato HH:mm.");
            if (!TimeOnly.TryParse(request.CloseTime, out var closeTime))
                throw new DomainException("Hora de cierre inválida. Use formato HH:mm.");
            if (openTime >= closeTime)
                throw new DomainException("La hora de apertura debe ser anterior a la de cierre.");

            var config = new TenantConfig(
                tenantId,
                request.TaxRate,
                request.Currency.Trim().ToUpperInvariant(),
                request.SlotDurationMinutes,
                openTime,
                closeTime);

            await _repo.UpsertAsync(config);
            return ApiResponse<TenantConfigDTO>.Ok(Map(config));
        }

        private static TenantConfigDTO Map(TenantConfig c) => new(
            c.TenantId, c.TaxRate, c.Currency,
            c.SlotDurationMinutes,
            c.OpenTime.ToString("HH:mm"),
            c.CloseTime.ToString("HH:mm"),
            c.UpdatedAt);
    }
}
