using FluentValidation;
using JobTracker.Application.Common.Abstractions;
using JobTracker.Application.Common.Exceptions;
using JobTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Applications;

public sealed record UpdateApplicationCommand(
    Guid Id,
    string CompanyName,
    string Position,
    WorkMode WorkMode,
    string? Location,
    string? JobUrl,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? Currency) : IRequest<ApplicationDetailsDto>;

public sealed class UpdateApplicationCommandValidator : AbstractValidator<UpdateApplicationCommand>
{
    public UpdateApplicationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        this.AddDetailRules(x => x.CompanyName, x => x.Position, x => x.Location, x => x.JobUrl,
            x => x.SalaryMin, x => x.SalaryMax, x => x.Currency, x => x.WorkMode);
    }
}

internal sealed class UpdateApplicationCommandHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<UpdateApplicationCommand, ApplicationDetailsDto>
{
    public async Task<ApplicationDetailsDto> Handle(UpdateApplicationCommand r, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var application = await db.JobApplications
            .Include(a => a.Notes).Include(a => a.History)
            .FirstOrDefaultAsync(a => a.Id == r.Id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Application", r.Id);

        application.UpdateDetails(r.CompanyName, r.Position, r.WorkMode, r.Location, r.JobUrl,
            r.SalaryMin, r.SalaryMax, r.Currency, clock.GetUtcNow().UtcDateTime);

        await db.SaveChangesAsync(ct);
        return application.ToDetails();
    }
}
