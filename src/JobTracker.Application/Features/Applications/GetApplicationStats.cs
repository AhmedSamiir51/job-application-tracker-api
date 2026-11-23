using JobTracker.Application.Common.Abstractions;
using JobTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Applications;

public sealed record ApplicationStatsDto(
    int Total,
    int Active,
    int Offers,
    double ResponseRate,
    IReadOnlyDictionary<ApplicationStatus, int> ByStatus);

public sealed record GetApplicationStatsQuery : IRequest<ApplicationStatsDto>;

internal sealed class GetApplicationStatsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetApplicationStatsQuery, ApplicationStatsDto>
{
    private static readonly ApplicationStatus[] ActiveStatuses =
        [ApplicationStatus.Applied, ApplicationStatus.PhoneScreen, ApplicationStatus.Interview, ApplicationStatus.Offer];

    private static readonly ApplicationStatus[] ResponseStatuses =
        [ApplicationStatus.PhoneScreen, ApplicationStatus.Interview, ApplicationStatus.Offer, ApplicationStatus.Accepted, ApplicationStatus.Rejected];

    public async Task<ApplicationStatsDto> Handle(GetApplicationStatsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var counts = await db.JobApplications.AsNoTracking()
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byStatus = Enum.GetValues<ApplicationStatus>()
            .ToDictionary(s => s, s => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);

        return Calculate(byStatus);
    }

    internal static ApplicationStatsDto Calculate(IReadOnlyDictionary<ApplicationStatus, int> byStatus)
    {
        var total = byStatus.Values.Sum();
        var submitted = total - byStatus.GetValueOrDefault(ApplicationStatus.Wishlist);
        var responded = ResponseStatuses.Sum(s => byStatus.GetValueOrDefault(s));
        var rate = submitted == 0 ? 0 : Math.Round(responded / (double)submitted, 4);

        return new ApplicationStatsDto(
            total,
            ActiveStatuses.Sum(s => byStatus.GetValueOrDefault(s)),
            byStatus.GetValueOrDefault(ApplicationStatus.Offer) + byStatus.GetValueOrDefault(ApplicationStatus.Accepted),
            rate,
            byStatus);
    }
}
