using JobTracker.Infrastructure.Identity;

namespace JobTracker.Application.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_is_salted_so_equal_passwords_differ()
    {
        Assert.NotEqual(_hasher.Hash("Secret123"), _hasher.Hash("Secret123"));
    }

    [Fact]
    public void Verify_accepts_correct_and_rejects_wrong_password()
    {
        var hash = _hasher.Hash("Secret123");
        Assert.True(_hasher.Verify("Secret123", hash));
        Assert.False(_hasher.Verify("secret123", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("v1.abc.def.ghi")]
    [InlineData("v1.1000.!!!.???")]
    public void Verify_returns_false_for_malformed_hashes(string hash)
    {
        Assert.False(_hasher.Verify("Secret123", hash));
    }
}
