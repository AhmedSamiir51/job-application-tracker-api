using FluentValidation;
using JobTracker.Application.Common.Abstractions;
using JobTracker.Application.Common.Exceptions;
using JobTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

internal sealed class LoginCommandHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService tokens)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same error for "unknown user" and "wrong password" so the API does not leak which emails exist.
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var token = tokens.Create(user);
        return new AuthResponse(token.Value, token.ExpiresAtUtc, new UserDto(user.Id, user.Email, user.FullName));
    }
}
