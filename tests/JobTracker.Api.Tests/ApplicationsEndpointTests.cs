using System.Net;
using System.Net.Http.Json;

namespace JobTracker.Api.Tests;

public class ApplicationsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Full_lifecycle_create_progress_note_and_delete()
    {
        var client = await factory.AuthenticatedAsync();

        var created = await client.CreateApplicationAsync();
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var id = (await created.ReadJsonAsync()).GetProperty("id").GetGuid();

        var applied = await client.PostAsJsonAsync($"/api/applications/{id}/status", new { status = "Applied", comment = "Sent CV" });
        Assert.Equal(HttpStatusCode.OK, applied.StatusCode);

        var note = await client.PostAsJsonAsync($"/api/applications/{id}/notes", new { text = "Recruiter replied" });
        Assert.Equal(HttpStatusCode.Created, note.StatusCode);

        var details = await (await client.GetAsync($"/api/applications/{id}")).ReadJsonAsync();
        Assert.Equal("Applied", details.GetProperty("status").GetString());
        Assert.Equal(2, details.GetProperty("history").GetArrayLength());
        Assert.Equal("Recruiter replied", details.GetProperty("notes")[0].GetProperty("text").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/applications/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/applications/{id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_status_transition_returns_422_problem_details()
    {
        var client = await factory.AuthenticatedAsync();
        var id = (await (await client.CreateApplicationAsync()).ReadJsonAsync()).GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync($"/api/applications/{id}/status", new { status = "Offer" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.ReadJsonAsync();
        Assert.Equal(422, problem.GetProperty("status").GetInt32());
        Assert.Contains("Wishlist", problem.GetProperty("detail").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Invalid_payload_returns_400_with_validation_errors()
    {
        var client = await factory.AuthenticatedAsync();

        var response = await client.PostAsJsonAsync("/api/applications", new
        {
            companyName = "",
            position = "Dev",
            workMode = "Remote",
            jobUrl = "ftp://nope",
            salaryMin = 10,
            salaryMax = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("CompanyName", out _));
        Assert.True(errors.TryGetProperty("JobUrl", out _));
        Assert.True(errors.TryGetProperty("SalaryMax", out _));
    }

    [Fact]
    public async Task Unknown_application_returns_404()
    {
        var client = await factory.AuthenticatedAsync();
        var response = await client.GetAsync($"/api/applications/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Another_users_application_is_invisible()
    {
        var owner = await factory.AuthenticatedAsync();
        var intruder = await factory.AuthenticatedAsync();
        var id = (await (await owner.CreateApplicationAsync()).ReadJsonAsync()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/applications/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"/api/applications/{id}")).StatusCode);
        Assert.Equal(0, (await (await intruder.GetAsync("/api/applications")).ReadJsonAsync()).GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task List_supports_filtering_sorting_and_paging_through_query_string()
    {
        var client = await factory.AuthenticatedAsync();
        foreach (var company in new[] { "Delta", "Alpha", "Charlie", "Bravo" })
        {
            await client.CreateApplicationAsync(company, status: company == "Alpha" ? "Applied" : "Wishlist");
        }

        var page = await (await client.GetAsync("/api/applications?sortBy=Company&descending=false&page=2&pageSize=2")).ReadJsonAsync();
        Assert.Equal(4, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
        Assert.Equal(["Charlie", "Delta"], page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("companyName").GetString()));

        var applied = await (await client.GetAsync("/api/applications?status=Applied")).ReadJsonAsync();
        Assert.Equal(1, applied.GetProperty("totalCount").GetInt32());

        var search = await (await client.GetAsync("/api/applications?search=brav")).ReadJsonAsync();
        Assert.Equal("Bravo", search.GetProperty("items")[0].GetProperty("companyName").GetString());
    }

    [Fact]
    public async Task Page_size_over_the_limit_is_a_400()
    {
        var client = await factory.AuthenticatedAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/applications?pageSize=1000")).StatusCode);
    }

    [Fact]
    public async Task Stats_summarize_the_pipeline()
    {
        var client = await factory.AuthenticatedAsync();
        await client.CreateApplicationAsync("A", status: "Applied");
        await client.CreateApplicationAsync("B", status: "Applied");
        var id = (await (await client.CreateApplicationAsync("C", status: "Applied")).ReadJsonAsync()).GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/api/applications/{id}/status", new { status = "Interview" });
        await client.CreateApplicationAsync("D");

        var stats = await (await client.GetAsync("/api/applications/stats")).ReadJsonAsync();

        Assert.Equal(4, stats.GetProperty("total").GetInt32());
        Assert.Equal(3, stats.GetProperty("active").GetInt32());
        Assert.Equal(1, stats.GetProperty("byStatus").GetProperty("Interview").GetInt32());
        Assert.Equal(0.3333, stats.GetProperty("responseRate").GetDouble(), 4);
    }

    [Fact]
    public async Task Update_changes_details()
    {
        var client = await factory.AuthenticatedAsync();
        var id = (await (await client.CreateApplicationAsync()).ReadJsonAsync()).GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync($"/api/applications/{id}", new
        {
            companyName = "Fabrikam",
            position = "Principal Engineer",
            workMode = "Hybrid"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal("Fabrikam", body.GetProperty("companyName").GetString());
        Assert.Equal("Hybrid", body.GetProperty("workMode").GetString());
    }
}
