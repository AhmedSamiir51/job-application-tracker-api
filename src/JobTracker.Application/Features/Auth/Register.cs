using FluentValidation;
using JobTracker.Application.Common.Abstractions;
using JobTracker.Application.Common.Exceptions;
using JobTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Features.Auth;

public sealed record RegisterCommand(string Email, string FullName, string Password) : IRequest<AuthResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

internal sealed class RegisterCommandHandler(
    IAppDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    TimeProvider clock) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = User.NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = User.Register(email, request.FullName, hasher.Hash(request.Password), clock.GetUtcNow().UtcDateTime);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = tokens.Create(user);
        return new AuthResponse(token.Value, token.ExpiresAtUtc, new UserDto(user.Id, user.Email, user.FullName));
    }
}
