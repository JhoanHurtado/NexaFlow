using NexaFlow.NexaPOS.Domain.Events;

namespace NexaFlow.NexaPOS.Application.Interfaces.Events
{
    /// <summary>
    /// Contrato para persistir eventos de dominio en la tabla <c>pos_events</c>.
    /// Usado para eventos que no requieren transacción atómica con otras operaciones.
    /// Para operaciones atómicas usar <see cref="UnitOfWork.IUnitOfWork.EnqueueEventAsync"/>.
    /// </summary>
    public interface IEventRepository
    {
        /// <summary>
        /// Persiste un evento de dominio en <c>pos_events</c> con <c>published = FALSE</c>.
        /// </summary>
        Task PublishAsync(DomainEvent domainEvent);
    }

    /// <summary>
    /// Contrato para el sistema de logging del microservicio NexaPOS.
    /// La implementación <c>LambdaPosLogger</c> escribe a <c>Console</c>,
    /// que Lambda reenvía automáticamente a CloudWatch Logs.
    /// </summary>
    public interface IPosLogger
    {
        /// <summary>Registra un mensaje informativo.</summary>
        void Info(string message);

        /// <summary>Registra una advertencia (ej. stock bajo, entidad no encontrada).</summary>
        void Warning(string message);

        /// <summary>Registra un error con mensaje y opcionalmente la excepción completa.</summary>
        void Error(string message, Exception? ex = null);
    }
}
