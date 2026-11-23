using JobTracker.Domain.Enums;

namespace JobTracker.Api.Contracts;

public sealed record ChangeStatusRequest(ApplicationStatus Status, string? Comment);

public sealed record AddNoteRequest(string Text);

public sealed record SaveApplicationRequest(
    string CompanyName,
    string Position,
    WorkMode WorkMode,
    string? Location,
    string? JobUrl,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? Currency,
    ApplicationStatus Status = ApplicationStatus.Wishlist);
