using JobTracker.Application.Common.Exceptions;
using JobTracker.Application.Features.Applications;
using JobTracker.Application.Tests.Support;
using JobTracker.Domain.Common;
using JobTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Tests;

public class ApplicationHandlerTests
{
    private static async Task<ApplicationDetailsDto> Create(
        TestContext c, string company = "Contoso", string position = "Backend Engineer",
        ApplicationStatus status = ApplicationStatus.Wishlist, WorkMode mode = WorkMode.Remote, string? location = "Cairo")
    {
        var handler = new CreateApplicationCommandHandler(c.Db, c.CurrentUser, c.Clock);
        return await handler.Handle(new CreateApplicationCommand(company, position, mode, location, null, null, null, null, status), default);
    }

    [Fact]
    public async Task Create_persists_application_for_current_user_with_initial_history()
    {
        using var c = new TestContext();

        var dto = await Create(c, status: ApplicationStatus.Applied);

        var stored = await c.Db.JobApplications.Include(a => a.History).SingleAsync();
        Assert.Equal(c.CurrentUser.UserId, stored.UserId);
        Assert.Equal(dto.Id, stored.Id);
        Assert.Single(stored.History);
        Assert.NotNull(dto.AppliedAtUtc);
        Assert.Contains(ApplicationStatus.Interview, dto.AllowedTransitions);
    }

    [Fact]
    public async Task ChangeStatus_updates_status_and_appends_history()
    {
        using var c = new TestContext();
        var created = await Create(c);
        var handler = new ChangeApplicationStatusCommandHandler(c.Db, c.CurrentUser, c.Clock);

        c.Clock.Advance(TimeSpan.FromDays(1));
        var dto = await handler.Handle(new ChangeApplicationStatusCommand(created.Id, ApplicationStatus.Applied, "Applied via site"), default);

        Assert.Equal(ApplicationStatus.Applied, dto.Status);
        Assert.Equal(2, dto.History.Count);
        Assert.Equal("Applied via site", dto.History[^1].Comment);
        Assert.Equal(1, await c.Db.StatusChanges.CountAsync(h => h.To == ApplicationStatus.Applied));
    }

    [Fact]
    public async Task ChangeStatus_with_invalid_transition_throws_domain_exception_and_persists_nothing()
    {
        using var c = new TestContext();
        var created = await Create(c);
        var handler = new ChangeApplicationStatusCommandHandler(c.Db, c.CurrentUser, c.Clock);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ChangeApplicationStatusCommand(created.Id, ApplicationStatus.Offer, null), default));

        Assert.Equal(1, await c.Db.StatusChanges.CountAsync());
    }

    [Fact]
    public async Task Users_cannot_read_update_or_delete_each_others_applications()
    {
        using var c = new TestContext();
        var mine = await Create(c);
        var intruder = new FakeCurrentUser(c.AddUser("intruder@example.com").Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetApplicationQueryHandler(c.Db, intruder).Handle(new GetApplicationQuery(mine.Id), default));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteApplicationCommandHandler(c.Db, intruder).Handle(new DeleteApplicationCommand(mine.Id), default));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ChangeApplicationStatusCommandHandler(c.Db, intruder, c.Clock)
                .Handle(new ChangeApplicationStatusCommand(mine.Id, ApplicationStatus.Applied, null), default));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new AddApplicationNoteCommandHandler(c.Db, intruder, c.Clock)
                .Handle(new AddApplicationNoteCommand(mine.Id, "hi"), default));

        Assert.Equal(1, await c.Db.JobApplications.CountAsync());
    }

    [Fact]
    public async Task Update_changes_details_but_not_status()
    {
        using var c = new TestContext();
        var created = await Create(c);
        var handler = new UpdateApplicationCommandHandler(c.Db, c.CurrentUser, c.Clock);

        var dto = await handler.Handle(new UpdateApplicationCommand(created.Id, "Fabrikam", "Tech Lead", WorkMode.Hybrid,
            "Remote", "https://jobs.example.com/1", 5000, 7000, "eur"), default);

        Assert.Equal("Fabrikam", dto.CompanyName);
        Assert.Equal("EUR", dto.Currency);
        Assert.Equal(ApplicationStatus.Wishlist, dto.Status);
    }

    [Fact]
    public async Task AddNote_persists_note_and_it_shows_in_details_newest_first()
    {
        using var c = new TestContext();
        var created = await Create(c);
        var handler = new AddApplicationNoteCommandHandler(c.Db, c.CurrentUser, c.Clock);

        await handler.Handle(new AddApplicationNoteCommand(created.Id, "first"), default);
        c.Clock.Advance(TimeSpan.FromHours(1));
        await handler.Handle(new AddApplicationNoteCommand(created.Id, "second"), default);

        var details = await new GetApplicationQueryHandler(c.Db, c.CurrentUser).Handle(new GetApplicationQuery(created.Id), default);
        Assert.Equal(["second", "first"], details.Notes.Select(n => n.Text));
    }

    [Fact]
    public async Task Delete_removes_application_with_its_children()
    {
        using var c = new TestContext();
        var created = await Create(c);
        await new AddApplicationNoteCommandHandler(c.Db, c.CurrentUser, c.Clock)
            .Handle(new AddApplicationNoteCommand(created.Id, "note"), default);

        await new DeleteApplicationCommandHandler(c.Db, c.CurrentUser).Handle(new DeleteApplicationCommand(created.Id), default);

        Assert.Empty(c.Db.JobApplications);
        Assert.Empty(c.Db.Notes);
        Assert.Empty(c.Db.StatusChanges);
    }
}
