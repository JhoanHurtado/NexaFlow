namespace NexaFlow.NexaBook.Application.Records.Create
{
    /// <summary>
    /// Request para cancelar una reserva.
    /// <para><c>CancelledBy</c>: identificador o nombre de quien cancela (cliente o staff).</para>
    /// </summary>
    public record CancelReservationRequest(string CancelledBy);
}
