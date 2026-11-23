namespace JobTracker.Application.Common.Exceptions;

public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");

public sealed class ConflictException(string message) : Exception(message);

public sealed class InvalidCredentialsException() : Exception("Invalid email or password.");
