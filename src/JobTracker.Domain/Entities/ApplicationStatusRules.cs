using JobTracker.Domain.Enums;

namespace JobTracker.Domain.Entities;

/// <summary>
/// The hiring pipeline as a small state machine. Keeping the rules in one place
/// makes them trivial to test and to reason about.
/// </summary>
public static class ApplicationStatusRules
{
    private static readonly IReadOnlyDictionary<ApplicationStatus, ApplicationStatus[]> Transitions =
        new Dictionary<ApplicationStatus, ApplicationStatus[]>
        {
            [ApplicationStatus.Wishlist] = [ApplicationStatus.Applied, ApplicationStatus.Withdrawn],
            [ApplicationStatus.Applied] =
                [ApplicationStatus.PhoneScreen, ApplicationStatus.Interview, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
            [ApplicationStatus.PhoneScreen] =
                [ApplicationStatus.Interview, ApplicationStatus.Offer, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
            [ApplicationStatus.Interview] =
                [ApplicationStatus.Offer, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
            [ApplicationStatus.Offer] =
                [ApplicationStatus.Accepted, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
            [ApplicationStatus.Accepted] = [],
            [ApplicationStatus.Rejected] = [],
            [ApplicationStatus.Withdrawn] = []
        };

    public static bool CanTransition(ApplicationStatus from, ApplicationStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static IReadOnlyCollection<ApplicationStatus> AllowedFrom(ApplicationStatus from) =>
        Transitions.TryGetValue(from, out var allowed) ? allowed : [];

    public static bool IsTerminal(ApplicationStatus status) => AllowedFrom(status).Count == 0;
}
