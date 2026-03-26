using NexaFlow.NexaBook.Application.Interfaces.Events;

namespace NexaFlow.NexaBook.Infrastructure.Logging
{
    public class LambdaBookLogger : IPosLogger
    {
        public void Info(string message) =>
            Console.WriteLine($"{DateTime.UtcNow:O} [INFO] {message}");

        public void Warning(string message) =>
            Console.WriteLine($"{DateTime.UtcNow:O} [WARN] {message}");

        public void Error(string message, Exception? ex = null)
        {
            Console.Error.WriteLine($"{DateTime.UtcNow:O} [ERROR] {message}");
            if (ex is not null)
                Console.Error.WriteLine($"{DateTime.UtcNow:O} [ERROR] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
