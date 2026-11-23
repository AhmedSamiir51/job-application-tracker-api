using FluentValidation;
using JobTracker.Application.Common.Abstractions;
using JobTracker.Application.Common.Models;
using JobTracker.Domain.Entities;
using JobTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Applications;

public enum ApplicationSortBy
{
    CreatedAt,
    UpdatedAt,
    AppliedAt,
    Company,
    Status
}

public sealed record ListApplicationsQuery(
    ApplicationStatus? Status = null,
    WorkMode? WorkMode = null,
    string? Company = null,
    string? Search = null,
    DateTime? AppliedFrom = null,
    DateTime? AppliedTo = null,
    ApplicationSortBy SortBy = ApplicationSortBy.CreatedAt,
    bool Descending = true,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ApplicationSummaryDto>>;

public sealed class ListApplicationsQueryValidator : AbstractValidator<ListApplicationsQuery>
{
    public const int MaxPageSize = 100;

    public ListApplicationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);
        RuleFor(x => x.WorkMode).IsInEnum().When(x => x.WorkMode is not null);
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.AppliedTo).GreaterThanOrEqualTo(x => x.AppliedFrom)
            .When(x => x.AppliedFrom is not null && x.AppliedTo is not null)
            .WithMessage("'AppliedTo' must not be before 'AppliedFrom'.");
        RuleFor(x => x.Company).MaximumLength(150);
        RuleFor(x => x.Search).MaximumLength(150);
    }
}

internal sealed class ListApplicationsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<ListApplicationsQuery, PagedResult<ApplicationSummaryDto>>
{
    public async Task<PagedResult<ApplicationSummaryDto>> Handle(ListApplicationsQuery r, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var query = db.JobApplications.AsNoTracking().Where(a => a.UserId == userId);

        if (r.Status is { } status) query = query.Where(a => a.Status == status);
        if (r.WorkMode is { } mode) query = query.Where(a => a.WorkMode == mode);
        if (r.AppliedFrom is { } from) { var f = AsUtc(from); query = query.Where(a => a.AppliedAtUtc >= f); }
        if (r.AppliedTo is { } to) { var t = AsUtc(to); query = query.Where(a => a.AppliedAtUtc <= t); }

        if (!string.IsNullOrWhiteSpace(r.Company))
        {
            var company = r.Company.Trim().ToLower();
            query = query.Where(a => a.CompanyName.ToLower().Contains(company));
        }

        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var term = r.Search.Trim().ToLower();
            query = query.Where(a => a.CompanyName.ToLower().Contains(term)
                                     || a.Position.ToLower().Contains(term)
                                     || (a.Location != null && a.Location.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, r.SortBy, r.Descending)
            .Skip((r.Page - 1) * r.PageSize)
            .Take(r.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ApplicationSummaryDto>(items.Select(a => a.ToSummary()).ToList(), r.Page, r.PageSize, total);
    }

    // PostgreSQL timestamptz parameters must be UTC; query-string dates arrive as Unspecified or Local.
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static IOrderedQueryable<JobApplication> Sort(IQueryable<JobApplication> q, ApplicationSortBy sortBy, bool desc) =>
        (sortBy, desc) switch
        {
            (ApplicationSortBy.Company, false) => q.OrderBy(a => a.CompanyName).ThenBy(a => a.Id),
            (ApplicationSortBy.Company, true) => q.OrderByDescending(a => a.CompanyName).ThenBy(a => a.Id),
            (ApplicationSortBy.Status, false) => q.OrderBy(a => a.Status).ThenBy(a => a.Id),
            (ApplicationSortBy.Status, true) => q.OrderByDescending(a => a.Status).ThenBy(a => a.Id),
            (ApplicationSortBy.AppliedAt, false) => q.OrderBy(a => a.AppliedAtUtc).ThenBy(a => a.Id),
            (ApplicationSortBy.AppliedAt, true) => q.OrderByDescending(a => a.AppliedAtUtc).ThenBy(a => a.Id),
            (ApplicationSortBy.UpdatedAt, false) => q.OrderBy(a => a.UpdatedAtUtc).ThenBy(a => a.Id),
            (ApplicationSortBy.UpdatedAt, true) => q.OrderByDescending(a => a.UpdatedAtUtc).ThenBy(a => a.Id),
            (_, false) => q.OrderBy(a => a.CreatedAtUtc).ThenBy(a => a.Id),
            _ => q.OrderByDescending(a => a.CreatedAtUtc).ThenBy(a => a.Id)
        };
}
