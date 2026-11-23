using JobTracker.Domain.Common;
using JobTracker.Domain.Entities;
using JobTracker.Domain.Enums;

namespace JobTracker.Domain.Tests;

public class JobApplicationTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    private static JobApplication NewApplication(ApplicationStatus status = ApplicationStatus.Wishlist) =>
        JobApplication.Create(UserId, " Contoso ", " Senior .NET Engineer ", WorkMode.Remote,
            "  Cairo ", null, 4000, 6000, "usd", status, Now);

    [Fact]
    public void Create_trims_and_normalizes_values_and_records_initial_history()
    {
        var app = NewApplication();

        Assert.Equal("Contoso", app.CompanyName);
        Assert.Equal("Senior .NET Engineer", app.Position);
        Assert.Equal("Cairo", app.Location);
        Assert.Equal("USD", app.Currency);
        Assert.Equal(ApplicationStatus.Wishlist, app.Status);
        Assert.Null(app.AppliedAtUtc);

        var entry = Assert.Single(app.History);
        Assert.Null(entry.From);
        Assert.Equal(ApplicationStatus.Wishlist, entry.To);
    }

    [Fact]
    public void Create_as_applied_sets_applied_date()
    {
        var app = NewApplication(ApplicationStatus.Applied);
        Assert.Equal(Now, app.AppliedAtUtc);
    }

    [Theory]
    [InlineData(ApplicationStatus.Interview)]
    [InlineData(ApplicationStatus.Offer)]
    [InlineData(ApplicationStatus.Rejected)]
    public void Create_rejects_invalid_initial_status(ApplicationStatus status)
    {
        Assert.Throws<DomainException>(() => NewApplication(status));
    }

    [Fact]
    public void Create_rejects_empty_user()
    {
        Assert.Throws<DomainException>(() => JobApplication.Create(Guid.Empty, "A", "B", WorkMode.Remote,
            null, null, null, null, null, ApplicationStatus.Wishlist, Now));
    }

    [Theory]
    [InlineData("", "Dev")]
    [InlineData("Acme", "  ")]
    public void Create_requires_company_and_position(string company, string position)
    {
        Assert.Throws<DomainException>(() => JobApplication.Create(UserId, company, position, WorkMode.Onsite,
            null, null, null, null, null, ApplicationStatus.Wishlist, Now));
    }

    [Fact]
    public void Create_rejects_inverted_salary_range()
    {
        var ex = Assert.Throws<DomainException>(() => JobApplication.Create(UserId, "A", "B", WorkMode.Onsite,
            null, null, 9000, 1000, "USD", ApplicationStatus.Wishlist, Now));
        Assert.Contains("Minimum salary", ex.Message);
    }

    [Fact]
    public void ChangeStatus_follows_pipeline_and_records_history()
    {
        var app = NewApplication();
        var later = Now.AddDays(2);

        app.ChangeStatus(ApplicationStatus.Applied, "Sent CV", later);
        app.ChangeStatus(ApplicationStatus.Interview, null, later.AddDays(5));

        Assert.Equal(ApplicationStatus.Interview, app.Status);
        Assert.Equal(later, app.AppliedAtUtc);
        Assert.Equal(3, app.History.Count);
        Assert.Equal(later.AddDays(5), app.UpdatedAtUtc);

        var last = app.History.Last();
        Assert.Equal(ApplicationStatus.Applied, last.From);
        Assert.Equal(ApplicationStatus.Interview, last.To);
    }

    [Fact]
    public void ChangeStatus_rejects_skipping_stages()
    {
        var app = NewApplication();
        var ex = Assert.Throws<DomainException>(() => app.ChangeStatus(ApplicationStatus.Offer, null, Now));
        Assert.Contains("Wishlist", ex.Message);
        Assert.Single(app.History);
    }

    [Theory]
    [InlineData(ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void Terminal_states_cannot_be_left(ApplicationStatus terminal)
    {
        var app = NewApplication(ApplicationStatus.Applied);
        app.ChangeStatus(ApplicationStatus.Interview, null, Now);
        app.ChangeStatus(ApplicationStatus.Offer, null, Now);
        app.ChangeStatus(terminal, null, Now);

        Assert.True(ApplicationStatusRules.IsTerminal(terminal));
        Assert.Throws<DomainException>(() => app.ChangeStatus(ApplicationStatus.Applied, null, Now));
    }

    [Fact]
    public void AddNote_trims_and_touches_updated_timestamp()
    {
        var app = NewApplication();
        var later = Now.AddHours(3);

        var note = app.AddNote("  Spoke to recruiter  ", later);

        Assert.Equal("Spoke to recruiter", note.Text);
        Assert.Single(app.Notes);
        Assert.Equal(later, app.UpdatedAtUtc);
    }

    [Fact]
    public void AddNote_rejects_blank_text()
    {
        Assert.Throws<DomainException>(() => NewApplication().AddNote("   ", Now));
    }

    [Fact]
    public void UpdateDetails_applies_the_same_invariants()
    {
        var app = NewApplication();
        app.UpdateDetails("Fabrikam", "Tech Lead", WorkMode.Hybrid, null, "https://example.com/job", null, null, null, Now.AddDays(1));

        Assert.Equal("Fabrikam", app.CompanyName);
        Assert.Equal(WorkMode.Hybrid, app.WorkMode);
        Assert.Null(app.Currency);
        Assert.Throws<DomainException>(() =>
            app.UpdateDetails("", "Tech Lead", WorkMode.Hybrid, null, null, null, null, null, Now));
    }
}
