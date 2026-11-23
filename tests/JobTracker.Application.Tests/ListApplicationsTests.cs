using JobTracker.Application.Features.Applications;
using JobTracker.Application.Tests.Support;
using JobTracker.Domain.Enums;

namespace JobTracker.Application.Tests;

public class ListApplicationsTests
{
    private static async Task Seed(TestContext c)
    {
        var create = new CreateApplicationCommandHandler(c.Db, c.CurrentUser, c.Clock);
        async Task Add(string company, string position, ApplicationStatus status, WorkMode mode, string location)
        {
            await create.Handle(new CreateApplicationCommand(company, position, mode, location, null, null, null, null, status), default);
            c.Clock.Advance(TimeSpan.FromHours(1));
        }

        await Add("Alpha", "Backend Engineer", ApplicationStatus.Applied, WorkMode.Remote, "Cairo");
        await Add("Beta", "Full Stack Developer", ApplicationStatus.Wishlist, WorkMode.Hybrid, "Dubai");
        await Add("Gamma", "Backend Engineer", ApplicationStatus.Applied, WorkMode.Onsite, "Berlin");
        await Add("Delta", "Tech Lead", ApplicationStatus.Wishlist, WorkMode.Remote, "Remote");
        await Add("Alphabet", "SRE", ApplicationStatus.Applied, WorkMode.Remote, "London");
    }

    private static Task<JobTracker.Application.Common.Models.PagedResult<ApplicationSummaryDto>> Run(TestContext c, ListApplicationsQuery q) =>
        new ListApplicationsQueryHandler(c.Db, c.CurrentUser).Handle(q, default);

    [Fact]
    public async Task Default_query_returns_newest_first_with_total_count()
    {
        using var c = new TestContext();
        await Seed(c);

        var page = await Run(c, new ListApplicationsQuery());

        Assert.Equal(5, page.TotalCount);
        Assert.Equal("Alphabet", page.Items[0].CompanyName);
        Assert.Equal("Alpha", page.Items[^1].CompanyName);
    }

    [Fact]
    public async Task Paging_returns_expected_slices_and_flags()
    {
        using var c = new TestContext();
        await Seed(c);

        var p1 = await Run(c, new ListApplicationsQuery(SortBy: ApplicationSortBy.Company, Descending: false, Page: 1, PageSize: 2));
        var p3 = await Run(c, new ListApplicationsQuery(SortBy: ApplicationSortBy.Company, Descending: false, Page: 3, PageSize: 2));

        Assert.Equal(3, p1.TotalPages);
        Assert.True(p1.HasNext);
        Assert.False(p1.HasPrevious);
        Assert.Equal(2, p1.Items.Count);
        Assert.Single(p3.Items);
        Assert.False(p3.HasNext);
        Assert.True(p3.HasPrevious);
    }

    [Fact]
    public async Task Filters_by_status_and_work_mode_combine()
    {
        using var c = new TestContext();
        await Seed(c);

        var applied = await Run(c, new ListApplicationsQuery(Status: ApplicationStatus.Applied));
        var appliedRemote = await Run(c, new ListApplicationsQuery(Status: ApplicationStatus.Applied, WorkMode: WorkMode.Remote));

        Assert.Equal(3, applied.TotalCount);
        Assert.Equal(2, appliedRemote.TotalCount);
    }

    [Fact]
    public async Task Company_filter_is_case_insensitive_contains()
    {
        using var c = new TestContext();
        await Seed(c);

        var result = await Run(c, new ListApplicationsQuery(Company: "ALPHA"));

        Assert.Equal(["Alpha", "Alphabet"], result.Items.Select(i => i.CompanyName).Order());
    }

    [Fact]
    public async Task Search_matches_company_position_or_location()
    {
        using var c = new TestContext();
        await Seed(c);

        Assert.Equal(2, (await Run(c, new ListApplicationsQuery(Search: "backend"))).TotalCount);
        Assert.Equal(1, (await Run(c, new ListApplicationsQuery(Search: "dubai"))).TotalCount);
        Assert.Equal(0, (await Run(c, new ListApplicationsQuery(Search: "nothing-matches"))).TotalCount);
    }

    [Fact]
    public async Task Sorting_by_company_descending_works()
    {
        using var c = new TestContext();
        await Seed(c);

        var result = await Run(c, new ListApplicationsQuery(SortBy: ApplicationSortBy.Company, Descending: true));

        Assert.Equal("Alpha", result.Items[^1].CompanyName);
        Assert.Equal("Gamma", result.Items[0].CompanyName);
    }

    [Fact]
    public async Task Applied_date_range_excludes_wishlist_items()
    {
        using var c = new TestContext();
        await Seed(c);

        var result = await Run(c, new ListApplicationsQuery(
            AppliedFrom: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            AppliedTo: new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal(ApplicationStatus.Applied, i.Status));
    }

    [Fact]
    public async Task Results_are_scoped_to_the_current_user()
    {
        using var c = new TestContext();
        await Seed(c);
        var other = new FakeCurrentUser(c.AddUser("other@example.com").Id);

        var result = await new ListApplicationsQueryHandler(c.Db, other).Handle(new ListApplicationsQuery(), default);

        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    [InlineData(1, 100, true)]
    [InlineData(2, 10, true)]
    public void Validator_bounds_paging(int page, int size, bool valid)
    {
        var result = new ListApplicationsQueryValidator().Validate(new ListApplicationsQuery(Page: page, PageSize: size));
        Assert.Equal(valid, result.IsValid);
    }
}
