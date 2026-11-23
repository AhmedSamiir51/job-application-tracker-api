using FluentValidation;
using JobTracker.Application.Common.Exceptions;
using JobTracker.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Infrastructure;

/// <summary>
/// Translates exceptions into RFC 9457 problem details so clients always get one consistent error shape.
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
            NotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
            ConflictException => (StatusCodes.Status409Conflict, "The request conflicts with the current state."),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Authentication failed."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication is required."),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "The request violates a business rule."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = status;

        ProblemDetails body;
        if (exception is ValidationException validation)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
            body = new ValidationProblemDetails(errors) { Status = status, Title = title };
        }
        else
        {
            body = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Never leak internal details for unexpected failures.
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message
            };
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = body,
            Exception = exception
        });
    }
}
