using FluentValidation;
using JobTracker.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace JobTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.TryAddSingletonTimeProvider();

        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services) =>
        services.AddSingleton(TimeProvider.System);
}
