using System.Text.Json;
using NikaFitness.Application.Carts.Models;
using NikaFitness.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NikaFitness.Infrastructure.Carts;

/// <summary>Stores carts as JSON in Redis with a sliding 30-day expiry.</summary>
public sealed class RedisCartStore(IConnectionMultiplexer redis) : ICartStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _db = redis.GetDatabase();

    private static string Key(string cartId) => $"cart:{cartId}";

    public async Task<Cart?> GetAsync(string cartId, CancellationToken cancellationToken)
    {
        var value = await _db.StringGetAsync(Key(cartId));
        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<Cart>(value!, JsonOptions);
    }

    public async Task SaveAsync(Cart cart, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(cart, JsonOptions);
        await _db.StringSetAsync(Key(cart.Id), payload, Ttl);
    }

    public Task RemoveAsync(string cartId, CancellationToken cancellationToken)
        => _db.KeyDeleteAsync(Key(cartId));
}
