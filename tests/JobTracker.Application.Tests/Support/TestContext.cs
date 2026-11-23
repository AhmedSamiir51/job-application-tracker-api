using JobTracker.Application.Common.Abstractions;
using JobTracker.Domain.Entities;
using JobTracker.Infrastructure.Identity;
using JobTracker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobTracker.Application.Tests.Support;

public sealed class FakeCurrentUser(Guid id) : ICurrentUser
{
    public Guid UserId { get; set; } = id;
}

public sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan by) => _now += by;
}

/// <summary>A real EF Core context on an in-memory SQLite database, one per test.</summary>
public sealed class TestContext : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestContext()
    {
        _connection.Open();
        Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();

        Hasher = new PasswordHasher();
        Clock = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
        Tokens = new JwtTokenService(Options.Create(new JwtOptions
        {
            SigningKey = "unit-test-signing-key-with-more-than-32-chars"
        }), Clock);

        var user = User.Register("owner@example.com", "Owner", Hasher.Hash("Passw0rd!"), Clock.GetUtcNow().UtcDateTime);
        Db.Users.Add(user);
        Db.SaveChanges();
        CurrentUser = new FakeCurrentUser(user.Id);
    }

    public AppDbContext Db { get; }
    public PasswordHasher Hasher { get; }
    public JwtTokenService Tokens { get; }
    public FakeTimeProvider Clock { get; }
    public FakeCurrentUser CurrentUser { get; }

    public User AddUser(string email)
    {
        var user = User.Register(email, "Other", Hasher.Hash("Passw0rd!"), Clock.GetUtcNow().UtcDateTime);
        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
