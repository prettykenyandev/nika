using NikaFitness.Application.Carts.Models;

namespace NikaFitness.Application.Common.Interfaces;

/// <summary>Persistence for transient shopping carts (backed by Redis).</summary>
public interface ICartStore
{
    Task<Cart?> GetAsync(string cartId, CancellationToken cancellationToken);
    Task SaveAsync(Cart cart, CancellationToken cancellationToken);
    Task RemoveAsync(string cartId, CancellationToken cancellationToken);
}
