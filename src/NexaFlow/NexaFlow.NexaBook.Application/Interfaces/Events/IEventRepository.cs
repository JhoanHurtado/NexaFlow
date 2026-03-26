using NexaFlow.NexaBook.Domain.Events;

namespace NexaFlow.NexaBook.Application.Interfaces.Events
{
    /// <summary>
    /// Persiste eventos de dominio en la tabla <c>pos_events</c> fuera de una transacción atómica.
    /// Para eventos dentro de una transacción usar <c>IUnitOfWork.EnqueueEventAsync</c>.
    /// </summary>
    public interface IEventRepository
    {
        Task PublishAsync(DomainEvent domainEvent);
    }

    /// <summary>
    /// Logging del microservicio NexaBook.
    /// La implementación escribe a Console, que Lambda reenvía a CloudWatch.
    /// </summary>
    public interface IPosLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? ex = null);
    }
}
