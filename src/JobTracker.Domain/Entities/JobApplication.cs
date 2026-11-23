using JobTracker.Domain.Common;
using JobTracker.Domain.Enums;

namespace JobTracker.Domain.Entities;

/// <summary>
/// Aggregate root: a single job application and everything that happens to it.
/// </summary>
public class JobApplication
{
    private readonly List<ApplicationNote> _notes = [];
    private readonly List<StatusChange> _history = [];

    private JobApplication() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string Position { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public WorkMode WorkMode { get; private set; }
    public string? JobUrl { get; private set; }
    public decimal? SalaryMin { get; private set; }
    public decimal? SalaryMax { get; private set; }
    public string? Currency { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public DateTime? AppliedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ApplicationNote> Notes => _notes;
    public IReadOnlyCollection<StatusChange> History => _history;

    public static JobApplication Create(
        Guid userId,
        string companyName,
        string position,
        WorkMode workMode,
        string? location,
        string? jobUrl,
        decimal? salaryMin,
        decimal? salaryMax,
        string? currency,
        ApplicationStatus initialStatus,
        DateTime nowUtc)
    {
        if (userId == Guid.Empty) throw new DomainException("An application must belong to a user.");
        if (initialStatus is not (ApplicationStatus.Wishlist or ApplicationStatus.Applied))
            throw new DomainException("A new application can only start as Wishlist or Applied.");

        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = initialStatus,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            AppliedAtUtc = initialStatus == ApplicationStatus.Applied ? nowUtc : null
        };

        application.ApplyDetails(companyName, position, workMode, location, jobUrl, salaryMin, salaryMax, currency);
        application._history.Add(StatusChange.Create(application.Id, null, initialStatus, "Application created", nowUtc));
        return application;
    }

    public void UpdateDetails(
        string companyName,
        string position,
        WorkMode workMode,
        string? location,
        string? jobUrl,
        decimal? salaryMin,
        decimal? salaryMax,
        string? currency,
        DateTime nowUtc)
    {
        ApplyDetails(companyName, position, workMode, location, jobUrl, salaryMin, salaryMax, currency);
        UpdatedAtUtc = nowUtc;
    }

    public void ChangeStatus(ApplicationStatus newStatus, string? comment, DateTime nowUtc)
    {
        if (!ApplicationStatusRules.CanTransition(Status, newStatus))
        {
            throw new DomainException($"Cannot move an application from {Status} to {newStatus}.");
        }

        _history.Add(StatusChange.Create(Id, Status, newStatus, comment, nowUtc));
        Status = newStatus;
        UpdatedAtUtc = nowUtc;

        if (newStatus == ApplicationStatus.Applied)
        {
            AppliedAtUtc = nowUtc;
        }
    }

    public ApplicationNote AddNote(string text, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("A note cannot be empty.");

        var note = ApplicationNote.Create(Id, text.Trim(), nowUtc);
        _notes.Add(note);
        UpdatedAtUtc = nowUtc;
        return note;
    }

    private void ApplyDetails(
        string companyName,
        string position,
        WorkMode workMode,
        string? location,
        string? jobUrl,
        decimal? salaryMin,
        decimal? salaryMax,
        string? currency)
    {
        if (string.IsNullOrWhiteSpace(companyName)) throw new DomainException("Company name is required.");
        if (string.IsNullOrWhiteSpace(position)) throw new DomainException("Position is required.");
        if (salaryMin is < 0 || salaryMax is < 0) throw new DomainException("Salary cannot be negative.");
        if (salaryMin is not null && salaryMax is not null && salaryMin > salaryMax)
            throw new DomainException("Minimum salary cannot exceed maximum salary.");

        CompanyName = companyName.Trim();
        Position = position.Trim();
        WorkMode = workMode;
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        JobUrl = string.IsNullOrWhiteSpace(jobUrl) ? null : jobUrl.Trim();
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        Currency = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant();
    }
}
