using NexaFlow.NexaAuth_Billing.Application.Interfaces.Repositories;
using Npgsql;

namespace NexaFlow.NexaAuth_Billing.Infrastructure.DBRepository;

public class PlanRepository : IPlanRepository
{
    private readonly string _conn;
    public PlanRepository(string conn) => _conn = conn;

    public async Task<IEnumerable<PlanRecord>> GetAllAsync()
    {
        await using var conn = new NpgsqlConnection(_conn);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, price, max_users, stripe_price_id FROM plans ORDER BY price ASC", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        var result = new List<PlanRecord>();
        while (await r.ReadAsync())
        {
            result.Add(new PlanRecord(
                Id:           r.GetString(0),
                Name:         r.GetString(1),
                Price:        r.GetDecimal(2),
                MaxUsers:     r.GetInt32(3),
                StripePriceId: r.IsDBNull(4) ? null : r.GetString(4)
            ));
        }
        return result;
    }
}
