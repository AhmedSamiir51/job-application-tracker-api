using FluentValidation;
using JobTracker.Domain.Enums;

namespace JobTracker.Application.Features.Applications;

internal static class ApplicationRules
{
    public static void AddDetailRules<T>(
        this AbstractValidator<T> v,
        Func<T, string> company, Func<T, string> position, Func<T, string?> location, Func<T, string?> url,
        Func<T, decimal?> min, Func<T, decimal?> max, Func<T, string?> currency, Func<T, WorkMode> mode)
    {
        v.RuleFor(x => company(x)).NotEmpty().MaximumLength(150).WithName("CompanyName");
        v.RuleFor(x => position(x)).NotEmpty().MaximumLength(150).WithName("Position");
        v.RuleFor(x => location(x)).MaximumLength(150).WithName("Location");
        v.RuleFor(x => url(x))
            .MaximumLength(2048)
            .Must(u => string.IsNullOrWhiteSpace(u) || (Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
            .WithMessage("'JobUrl' must be a valid http(s) URL.")
            .WithName("JobUrl");
        v.RuleFor(x => min(x)).GreaterThanOrEqualTo(0).When(x => min(x) is not null).WithName("SalaryMin");
        v.RuleFor(x => max(x)).GreaterThanOrEqualTo(0).When(x => max(x) is not null).WithName("SalaryMax");
        v.RuleFor(x => max(x))
            .GreaterThanOrEqualTo(x => min(x))
            .When(x => min(x) is not null && max(x) is not null)
            .WithMessage("'SalaryMax' must be greater than or equal to 'SalaryMin'.")
            .WithName("SalaryMax");
        v.RuleFor(x => currency(x)).Length(3).When(x => !string.IsNullOrWhiteSpace(currency(x)))
            .WithMessage("'Currency' must be a 3-letter ISO code.").WithName("Currency");
        v.RuleFor(x => mode(x)).IsInEnum().WithName("WorkMode");
    }
}
