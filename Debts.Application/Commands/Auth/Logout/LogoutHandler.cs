using System.IdentityModel.Tokens.Jwt;
using Debts.Application.Abstractions.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Debts.Application.Commands.Auth.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly ITokenBlacklistService _blacklistService;
    private readonly IConfiguration _configuration;

    public LogoutHandler(
        ITokenBlacklistService blacklistService,
        IConfiguration configuration)
    {
        _blacklistService = blacklistService;
        _configuration = configuration;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        // Calcular el tiempo restante del token
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(command.Token);
        var expiry = jwt.ValidTo - DateTime.UtcNow;

        if (expiry > TimeSpan.Zero)
            await _blacklistService.BlacklistAsync(command.Token, expiry);
    }
}