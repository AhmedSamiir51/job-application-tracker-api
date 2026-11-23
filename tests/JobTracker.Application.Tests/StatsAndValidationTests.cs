using JobTracker.Application.Features.Applications;
using JobTracker.Application.Tests.Support;
using JobTracker.Domain.Enums;

namespace JobTracker.Application.Tests;

public class StatsAndValidationTests
{
    [Fact]
    public void Calculate_computes_totals_active_offers_and_response_rate()
    {
        var counts = Enum.GetValues<ApplicationStatus>().ToDictionary(s => s, _ => 0);
        counts[ApplicationStatus.Wishlist] = 4;
        counts[ApplicationStatus.Applied] = 4;
        counts[ApplicationStatus.Interview] = 2;
        counts[ApplicationStatus.Offer] = 1;
        counts[ApplicationStatus.Rejected] = 1;

        var stats = GetApplicationStatsQueryHandler.Calculate(counts);

        Assert.Equal(12, stats.Total);
        Assert.Equal(7, stats.Active);
        Assert.Equal(1, stats.Offers);
        // 4 of the 8 submitted applications received a response
        Assert.Equal(0.5, stats.ResponseRate);
    }

    [Fact]
    public void Calculate_handles_no_submitted_applications_without_dividing_by_zero()
    {
        var counts = Enum.GetValues<ApplicationStatus>().ToDictionary(s => s, _ => 0);
        counts[ApplicationStatus.Wishlist] = 3;

        Assert.Equal(0, GetApplicationStatsQueryHandler.Calculate(counts).ResponseRate);
    }

    [Fact]
    public async Task Handler_reads_counts_from_the_database_for_current_user_only()
    {
        using var c = new TestContext();
        var create = new CreateApplicationCommandHandler(c.Db, c.CurrentUser, c.Clock);
        await create.Handle(new CreateApplicationCommand("A", "B", WorkMode.Remote, null, null, null, null, null, ApplicationStatus.Applied), default);
        await create.Handle(new CreateApplicationCommand("C", "D", WorkMode.Remote, null, null, null, null, null), default);
        var other = new FakeCurrentUser(c.AddUser("o@example.com").Id);
        await new CreateApplicationCommandHandler(c.Db, other, c.Clock)
            .Handle(new CreateApplicationCommand("X", "Y", WorkMode.Remote, null, null, null, null, null, ApplicationStatus.Applied), default);

        var stats = await new GetApplicationStatsQueryHandler(c.Db, c.CurrentUser).Handle(new GetApplicationStatsQuery(), default);

        Assert.Equal(2, stats.Total);
        Assert.Equal(1, stats.ByStatus[ApplicationStatus.Applied]);
        Assert.Equal(1, stats.ByStatus[ApplicationStatus.Wishlist]);
    }

    private static CreateApplicationCommand Valid() =>
        new("Contoso", "Engineer", WorkMode.Remote, "Cairo", "https://example.com/job", 1000, 2000, "USD");

    [Fact]
    public void Create_validator_accepts_a_valid_command()
    {
        Assert.True(new CreateApplicationCommandValidator().Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("", "Engineer")]
    [InlineData("Contoso", "")]
    public void Create_validator_requires_company_and_position(string company, string position)
    {
        var result = new CreateApplicationCommandValidator().Validate(Valid() with { CompanyName = company, Position = position });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_validator_rejects_bad_url_salary_range_currency_and_status()
    {
        var validator = new CreateApplicationCommandValidator();

        Assert.False(validator.Validate(Valid() with { JobUrl = "javascript:alert(1)" }).IsValid);
        Assert.False(validator.Validate(Valid() with { SalaryMin = 5000, SalaryMax = 1000 }).IsValid);
        Assert.False(validator.Validate(Valid() with { Currency = "DOLLARS" }).IsValid);
        Assert.False(validator.Validate(Valid() with { Status = ApplicationStatus.Offer }).IsValid);
        Assert.False(validator.Validate(Valid() with { WorkMode = (WorkMode)42 }).IsValid);
    }
}
