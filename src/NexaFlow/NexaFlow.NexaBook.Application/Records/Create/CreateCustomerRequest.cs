namespace NexaFlow.NexaBook.Application.Records.Create
{
    /// <summary>
    /// Request para registrar un cliente.
    /// <para><c>SelfRegistered = true</c> cuando el cliente se registra por sí mismo.</para>
    /// <para><c>SelfRegistered = false</c> cuando lo registra el staff del tenant.</para>
    /// </summary>
    public record CreateCustomerRequest(
        string Name,
        string? Phone,
        string? Email,
        bool SelfRegistered = false
    );
}
