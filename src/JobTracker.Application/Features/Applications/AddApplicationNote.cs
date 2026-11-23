using FluentValidation;
using JobTracker.Application.Common.Abstractions;
using JobTracker.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Applications;

public sealed record AddApplicationNoteCommand(Guid ApplicationId, string Text) : IRequest<NoteDto>;

public sealed class AddApplicationNoteCommandValidator : AbstractValidator<AddApplicationNoteCommand>
{
    public AddApplicationNoteCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
    }
}

internal sealed class AddApplicationNoteCommandHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<AddApplicationNoteCommand, NoteDto>
{
    public async Task<NoteDto> Handle(AddApplicationNoteCommand r, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var application = await db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == r.ApplicationId && a.UserId == userId, ct)
            ?? throw new NotFoundException("Application", r.ApplicationId);

        var note = application.AddNote(r.Text, clock.GetUtcNow().UtcDateTime);
        // Added through the DbSet so EF inserts it explicitly instead of guessing from a backing field.
        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);

        return new NoteDto(note.Id, note.Text, note.CreatedAtUtc);
    }
}
