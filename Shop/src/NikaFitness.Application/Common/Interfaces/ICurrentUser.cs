namespace NikaFitness.Application.Common.Interfaces;

/// <summary>Information about the caller, populated from the JWT on each request.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
