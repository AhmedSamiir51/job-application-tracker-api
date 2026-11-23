using FluentValidation;
using JobTracker.Application.Common.Abstractions;
using JobTracker.Domain.Entities;
using JobTracker.Domain.Enums;
using MediatR;

namespace JobTracker.Application.Features.Applications;

public sealed record CreateApplicationCommand(
    string CompanyName,
    string Position,
    WorkMode WorkMode,
    string? Location,
    string? JobUrl,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? Currency,
    ApplicationStatus Status = ApplicationStatus.Wishlist) : IRequest<ApplicationDetailsDto>;

public sealed class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        this.AddDetailRules(x => x.CompanyName, x => x.Position, x => x.Location, x => x.JobUrl,
            x => x.SalaryMin, x => x.SalaryMax, x => x.Currency, x => x.WorkMode);
        RuleFor(x => x.Status)
            .Must(s => s is ApplicationStatus.Wishlist or ApplicationStatus.Applied)
            .WithMessage("A new application can only start as Wishlist or Applied.");
    }
}

internal sealed class CreateApplicationCommandHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<CreateApplicationCommand, ApplicationDetailsDto>
{
    public async Task<ApplicationDetailsDto> Handle(CreateApplicationCommand r, CancellationToken ct)
    {
        var application = JobApplication.Create(currentUser.UserId, r.CompanyName, r.Position, r.WorkMode, r.Location,
            r.JobUrl, r.SalaryMin, r.SalaryMax, r.Currency, r.Status, clock.GetUtcNow().UtcDateTime);

        db.JobApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return application.ToDetails();
    }
}
