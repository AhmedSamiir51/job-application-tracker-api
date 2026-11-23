using JobTracker.Domain.Entities;

namespace JobTracker.Application.Common.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessToken Create(User user);
}

public interface ICurrentUser
{
    /// <summary>The authenticated user's id. Throws when there is no authenticated user.</summary>
    Guid UserId { get; }
}
