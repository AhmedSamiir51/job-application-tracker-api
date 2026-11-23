using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JobTracker.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await next();
            logger.LogInformation("Handled {Request} in {ElapsedMs} ms", name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Request {Request} failed after {ElapsedMs} ms: {Error}", name,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, ex.GetType().Name);
            throw;
        }
    }
}
