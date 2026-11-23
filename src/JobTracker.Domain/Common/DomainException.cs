namespace JobTracker.Domain.Common;

/// <summary>
/// Raised when a domain invariant or business rule is violated.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
