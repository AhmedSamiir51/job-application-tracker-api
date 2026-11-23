using System.Security.Claims;
using JobTracker.Application.Common.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JobTracker.Api.Infrastructure;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                        ?? accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("There is no authenticated user.");
        }
    }
}
