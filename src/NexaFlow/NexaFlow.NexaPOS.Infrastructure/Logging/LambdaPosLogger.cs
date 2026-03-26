using NexaFlow.NexaPOS.Application.Interfaces.Events;

namespace NexaFlow.NexaPOS.Infrastructure.Logging
{
    /// <summary>
    /// Implementación de <see cref="IPosLogger"/> para el entorno AWS Lambda.
    /// Escribe a <see cref="Console"/> con timestamp ISO 8601 y nivel de log.
    /// Lambda reenvía automáticamente la salida de Console a CloudWatch Logs,
    /// por lo que no se requiere ninguna dependencia adicional de logging.
    /// <para>
    /// Formato de salida: <c>2024-01-15T10:30:00.000Z [INFO] mensaje</c>
    /// </para>
    /// </summary>
    public class LambdaPosLogger : IPosLogger
    {
        /// <inheritdoc/>
        public void Info(string message) =>
            Console.WriteLine($"{DateTime.UtcNow:O} [INFO] {message}");

        /// <inheritdoc/>
        public void Warning(string message) =>
            Console.WriteLine($"{DateTime.UtcNow:O} [WARN] {message}");

        /// <inheritdoc/>
        public void Error(string message, Exception? ex = null)
        {
            Console.Error.WriteLine($"{DateTime.UtcNow:O} [ERROR] {message}");
            if (ex is not null)
                Console.Error.WriteLine($"{DateTime.UtcNow:O} [ERROR] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
