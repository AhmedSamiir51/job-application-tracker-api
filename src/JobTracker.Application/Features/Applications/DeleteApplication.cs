using JobTracker.Application.Common.Abstractions;
using JobTracker.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Applications;

public sealed record DeleteApplicationCommand(Guid Id) : IRequest;

internal sealed class DeleteApplicationCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<DeleteApplicationCommand>
{
    public async Task Handle(DeleteApplicationCommand r, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var application = await db.JobApplications.FirstOrDefaultAsync(a => a.Id == r.Id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Application", r.Id);

        db.JobApplications.Remove(application);
        await db.SaveChangesAsync(ct);
    }
}
