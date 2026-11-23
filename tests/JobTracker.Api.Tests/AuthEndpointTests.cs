using System.Net;
using System.Net.Http.Json;

namespace JobTracker.Api.Tests;

public class AuthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Register_then_login_returns_tokens()
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "ahmed@example.com", fullName = "Ahmed", password = "Passw0rd1" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "AHMED@example.com", password = "Passw0rd1" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var body = await login.ReadJsonAsync();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.Equal("ahmed@example.com", body.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Duplicate_registration_returns_409_problem_details()
    {
        var client = factory.CreateClient();
        var payload = new { email = "dup@example.com", fullName = "Dup", password = "Passw0rd1" };
        await client.PostAsJsonAsync("/api/auth/register", payload);

        var second = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
        Assert.Equal(409, (await second.ReadJsonAsync()).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Weak_password_returns_400_with_field_errors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { email = "weak@example.com", fullName = "Weak", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Wrong_password_returns_401()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new { email = "who@example.com", fullName = "Who", password = "Passw0rd1" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "who@example.com", password = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/applications")]
    [InlineData("/api/applications/stats")]
    public async Task Protected_endpoints_require_a_token(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Garbage_token_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not.a.jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/applications")).StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}

public class SwaggerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task OpenApi_document_is_generated_with_bearer_security()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await response.ReadJsonAsync();
        Assert.True(doc.GetProperty("paths").TryGetProperty("/api/applications/{id}/status", out _));
        Assert.True(doc.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }
}
