using Debts.API.Contracts.Requests;
using Debts.Application.Commands.Debts.CreateDebt;
using Debts.Application.Commands.SettleDebt;
using Debts.Application.Queries.GetDebtById;
using Debts.Application.Queries.GetDebts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Debts.API.Controllers;

[ApiController]
[Route("debts")]
[EnableRateLimiting("mixed-per-user")]
public class DebtsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DebtsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateDebtRequest request)
    {
        var command = new CreateDebtCommand
        {
            UserId = request.UserId,
            Amount = request.Amount
        };

        var debtId = await _mediator.Send(command);

        return Ok(debtId);
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var query = new GetDebtsQuery();

        var debt = await _mediator.Send(query);

        return Ok(debt);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetDebtByIdQuery
            {
                Id = id
            };

        var debt = await _mediator.Send(query);
        
        return Ok(debt);
    }
    
    [DisableRateLimiting]//quitar el rate limit 
    [HttpPatch("{id:guid}/settle")]
    public async Task<IActionResult> Settle(Guid id)
    {
        var command = new SettleDebtCommand
        {
            DebtId = id
        };

        await _mediator.Send(command);

        return NoContent();
    }
}
