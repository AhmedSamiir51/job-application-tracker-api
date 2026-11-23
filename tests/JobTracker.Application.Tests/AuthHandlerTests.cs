using FluentValidation;
using JobTracker.Application.Common.Exceptions;
using JobTracker.Application.Features.Auth;
using JobTracker.Application.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JobTracker.Application.Tests;

public class AuthHandlerTests
{
    private static RegisterCommandHandler RegisterHandler(TestContext c) => new(c.Db, c.Hasher, c.Tokens, c.Clock);

    [Fact]
    public async Task Register_creates_user_with_hashed_password_and_returns_valid_jwt()
    {
        using var c = new TestContext();

        var result = await RegisterHandler(c).Handle(new RegisterCommand("  New@Example.com ", "New Person", "Str0ngPass"), default);

        var stored = await c.Db.Users.SingleAsync(u => u.Email == "new@example.com");
        Assert.NotEqual("Str0ngPass", stored.PasswordHash);
        Assert.True(c.Hasher.Verify("Str0ngPass", stored.PasswordHash));
        Assert.Equal(stored.Id, result.User.Id);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(result.AccessToken);
        Assert.Equal(stored.Id.ToString(), jwt.Subject);
        Assert.Equal(c.Clock.GetUtcNow().UtcDateTime.AddMinutes(60), result.ExpiresAtUtc);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email_case_insensitively()
    {
        using var c = new TestContext();
        await Assert.ThrowsAsync<ConflictException>(() =>
            RegisterHandler(c).Handle(new RegisterCommand("OWNER@example.com", "Dup", "Str0ngPass"), default));
    }

    [Fact]
    public async Task Login_succeeds_with_correct_credentials()
    {
        using var c = new TestContext();
        var handler = new LoginCommandHandler(c.Db, c.Hasher, c.Tokens);

        var result = await handler.Handle(new LoginCommand("Owner@Example.com", "Passw0rd!"), default);

        Assert.Equal("owner@example.com", result.User.Email);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
    }

    [Theory]
    [InlineData("owner@example.com", "wrong")]
    [InlineData("nobody@example.com", "Passw0rd!")]
    public async Task Login_fails_identically_for_unknown_user_and_wrong_password(string email, string password)
    {
        using var c = new TestContext();
        var handler = new LoginCommandHandler(c.Db, c.Hasher, c.Tokens);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(new LoginCommand(email, password), default));
    }

    [Theory]
    [InlineData("not-an-email", "Str0ngPass", false)]
    [InlineData("a@b.com", "short1A", false)]
    [InlineData("a@b.com", "alllowercase1", false)]
    [InlineData("a@b.com", "NoDigitsHere", false)]
    [InlineData("a@b.com", "Str0ngPass", true)]
    public void Register_validator_enforces_email_and_password_policy(string email, string password, bool valid)
    {
        var result = new RegisterCommandValidator().Validate(new RegisterCommand(email, "Name", password));
        Assert.Equal(valid, result.IsValid);
    }
}
