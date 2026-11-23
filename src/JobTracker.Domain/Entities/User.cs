using JobTracker.Domain.Common;

namespace JobTracker.Domain.Entities;

public class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static User Register(string email, string fullName, string passwordHash, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Full name is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");

        return new User
        {
            Id = Guid.NewGuid(),
            Email = NormalizeEmail(email),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            CreatedAtUtc = nowUtc
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
