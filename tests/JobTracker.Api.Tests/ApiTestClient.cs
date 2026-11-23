using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobTracker.Api.Tests;

public static class ApiTestClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<HttpClient> AuthenticatedAsync(this ApiFactory factory, string? email = null)
    {
        var client = factory.CreateClient();
        email ??= $"user-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, fullName = "Test User", password = "Passw0rd1" });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json);

    public static Task<HttpResponseMessage> CreateApplicationAsync(this HttpClient client, string company = "Contoso",
        string position = "Senior .NET Engineer", string status = "Wishlist") =>
        client.PostAsJsonAsync("/api/applications", new
        {
            companyName = company,
            position,
            workMode = "Remote",
            location = "Cairo",
            salaryMin = 4000,
            salaryMax = 6000,
            currency = "USD",
            status
        });
}
