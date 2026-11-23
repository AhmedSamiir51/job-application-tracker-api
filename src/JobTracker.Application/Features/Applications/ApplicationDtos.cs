using JobTracker.Domain.Entities;
using JobTracker.Domain.Enums;

namespace JobTracker.Application.Features.Applications;

public sealed record ApplicationSummaryDto(
    Guid Id,
    string CompanyName,
    string Position,
    string? Location,
    WorkMode WorkMode,
    ApplicationStatus Status,
    DateTime? AppliedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record NoteDto(Guid Id, string Text, DateTime CreatedAtUtc);

public sealed record StatusChangeDto(ApplicationStatus? From, ApplicationStatus To, string? Comment, DateTime ChangedAtUtc);

public sealed record ApplicationDetailsDto(
    Guid Id,
    string CompanyName,
    string Position,
    string? Location,
    WorkMode WorkMode,
    string? JobUrl,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? Currency,
    ApplicationStatus Status,
    IReadOnlyCollection<ApplicationStatus> AllowedTransitions,
    DateTime? AppliedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<NoteDto> Notes,
    IReadOnlyList<StatusChangeDto> History);

internal static class ApplicationMapping
{
    public static ApplicationSummaryDto ToSummary(this JobApplication a) => new(
        a.Id, a.CompanyName, a.Position, a.Location, a.WorkMode, a.Status, a.AppliedAtUtc, a.CreatedAtUtc, a.UpdatedAtUtc);

    public static ApplicationDetailsDto ToDetails(this JobApplication a) => new(
        a.Id, a.CompanyName, a.Position, a.Location, a.WorkMode, a.JobUrl, a.SalaryMin, a.SalaryMax, a.Currency,
        a.Status, ApplicationStatusRules.AllowedFrom(a.Status), a.AppliedAtUtc, a.CreatedAtUtc, a.UpdatedAtUtc,
        a.Notes.OrderByDescending(n => n.CreatedAtUtc).Select(n => new NoteDto(n.Id, n.Text, n.CreatedAtUtc)).ToList(),
        a.History.OrderBy(h => h.ChangedAtUtc).Select(h => new StatusChangeDto(h.From, h.To, h.Comment, h.ChangedAtUtc)).ToList());
}
