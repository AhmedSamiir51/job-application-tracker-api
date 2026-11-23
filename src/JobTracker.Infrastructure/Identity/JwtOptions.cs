using System.ComponentModel.DataAnnotations;

namespace JobTracker.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = "JobTracker";
    [Required] public string Audience { get; init; } = "JobTracker.Clients";

    /// <summary>HMAC signing key. Must be at least 32 characters; supply via environment or user-secrets.</summary>
    [Required, MinLength(32)] public string SigningKey { get; init; } = string.Empty;

    [Range(1, 1440)] public int ExpiryMinutes { get; init; } = 60;
}
