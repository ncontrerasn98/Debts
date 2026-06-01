using Debts.API.Contracts.Requests;
using Debts.API.Contracts.Responses;
using Debts.Application.Commands.Auth.Login;
using Debts.Application.Commands.Auth.Logout;
using Debts.Application.Commands.Auth.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Debts.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var command = new LoginCommand
        {
            Name = request.Name,
            Password = request.Password
        };

        var token =
            await _mediator.Send(command);

        return Ok(new AuthResponse
        { 
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken
        });
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);

        return Ok(result);
    }
    
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Headers.Authorization
            .FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
            return BadRequest("No token provided");

        await _mediator.Send(new LogoutCommand { Token = token });

        return NoContent();
    }
}