using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Debts.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Http;

namespace Debts.Infrastructure.Persistence.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
                .Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Name
        => _httpContextAccessor.HttpContext?.User
            .Claims
            .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
}