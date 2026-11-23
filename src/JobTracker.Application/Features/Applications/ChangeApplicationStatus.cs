using FluentValidation;
using JobTracker.Application.Common.Abstractions;
using JobTracker.Application.Common.Exceptions;
using JobTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Applications;

public sealed record ChangeApplicationStatusCommand(Guid Id, ApplicationStatus Status, string? Comment)
    : IRequest<ApplicationDetailsDto>;

public sealed class ChangeApplicationStatusCommandValidator : AbstractValidator<ChangeApplicationStatusCommand>
{
    public ChangeApplicationStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}

internal sealed class ChangeApplicationStatusCommandHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<ChangeApplicationStatusCommand, ApplicationDetailsDto>
{
    public async Task<ApplicationDetailsDto> Handle(ChangeApplicationStatusCommand r, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var application = await db.JobApplications
            .Include(a => a.Notes).Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == r.Id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Application", r.Id);

        // Throws DomainException when the transition is not allowed (mapped to 422 by the API).
        application.ChangeStatus(r.Status, r.Comment, clock.GetUtcNow().UtcDateTime);

        await db.SaveChangesAsync(ct);
        return application.ToDetails();
    }
}
