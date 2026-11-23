using JobTracker.Application.Common.Abstractions;
using JobTracker.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Applications;

public sealed record GetApplicationQuery(Guid Id) : IRequest<ApplicationDetailsDto>;

internal sealed class GetApplicationQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetApplicationQuery, ApplicationDetailsDto>
{
    public async Task<ApplicationDetailsDto> Handle(GetApplicationQuery r, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var application = await db.JobApplications.AsNoTracking()
            .Include(a => a.Notes).Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == r.Id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Application", r.Id);

        return application.ToDetails();
    }
}
