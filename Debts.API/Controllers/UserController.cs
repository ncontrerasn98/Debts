using Debts.API.Contracts.Requests;
using Debts.Application.Commands.CreateUser;
using Debts.Application.Commands.CreditScore.GetUserCreditScore;
using Debts.Application.Commands.Users.AssignRole;
using Debts.Application.Commands.Users.RevokeRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Debts.API.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    } 
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var command = new CreateUserCommand
        {
            Name = request.Name,
            Password = request.Password
        };

        var userId = await _mediator.Send(command);

        return Ok(userId);
    }
    
    [HttpPost("{userId}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRoles(Guid userId, AssignRoleRequest request)
    {
        await _mediator.Send(new AssignRoleCommand
        {
            UserId = userId,
            RoleName = request.RoleName
        });

        return NoContent();
    }

    [HttpDelete("{userId}/roles/{roleName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Revoke(Guid userId, string roleName)
    {
        await _mediator.Send(new RevokeRoleCommand
        {
            UserId = userId,
            RoleName = roleName
        });

        return NoContent();
    }
    
    [HttpGet("{userId}/credit-score")]
    [Authorize(Roles = "Admin,UserAdmin")]
    public async Task<IActionResult> GetCreditScore(Guid userId)
    {
        var result = await _mediator.Send(new GetUserCreditScoreQuery { UserId = userId });
        return Ok(result);
    }
    
    // [HttpGet]
    // public async Task<IActionResult> Get()
    // {
    //     var query = new GetDebtsQuery();
    //
    //     var result = await _getDebtsHandler.Handle(query);
    //
    //     return Ok(result);
    // }
    
}