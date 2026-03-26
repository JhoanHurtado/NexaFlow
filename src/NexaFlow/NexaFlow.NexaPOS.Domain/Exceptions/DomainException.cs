namespace NexaFlow.NexaPOS.Domain.Exceptions
{
    /// <summary>
    /// Excepción base para todas las violaciones de reglas de negocio en NexaFlow.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }

        public DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
