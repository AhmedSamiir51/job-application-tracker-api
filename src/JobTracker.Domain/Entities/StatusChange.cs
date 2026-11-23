using JobTracker.Domain.Enums;

namespace JobTracker.Domain.Entities;

/// <summary>Immutable audit record of a pipeline transition.</summary>
public class StatusChange
{
    private StatusChange() { }

    public Guid Id { get; private set; }
    public Guid JobApplicationId { get; private set; }
    public ApplicationStatus? From { get; private set; }
    public ApplicationStatus To { get; private set; }
    public string? Comment { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    internal static StatusChange Create(Guid applicationId, ApplicationStatus? from, ApplicationStatus to, string? comment, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        JobApplicationId = applicationId,
        From = from,
        To = to,
        Comment = comment,
        ChangedAtUtc = nowUtc
    };
}
