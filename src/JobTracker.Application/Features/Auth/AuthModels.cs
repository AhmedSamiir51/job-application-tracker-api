namespace JobTracker.Application.Features.Auth;

public sealed record UserDto(Guid Id, string Email, string FullName);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserDto User);
