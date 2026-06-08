using Debts.API.Attributes;
using Debts.API.Contracts.Requests;
using Debts.Application.Abstractions.Auth;
using Debts.Application.Commands.Debts.CreateDebt;
using Debts.Application.Commands.SettleDebt;
using Debts.Application.Queries.GetDebtById;
using Debts.Application.Queries.GetDebts;
using Debts.Application.Sagas.CreateDebt.Messages;
using Debts.Domain.Exceptions;
using MassTransit;
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
    private readonly ICurrentUserService  _currentUserService;
    private readonly IRequestClient<CreateDebtRequested> _requestClient;

    public DebtsController(IMediator mediator,  ICurrentUserService currentUserService, IRequestClient<CreateDebtRequested> requestClient)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _requestClient = requestClient;
    }

    [HttpPost]
    [AllowAnonymous]
    [Idempotent]
    public async Task<IActionResult> Create(CreateDebtRequest request)
    {
        var correlationId = Guid.NewGuid();

        var response = await _requestClient.GetResponse<CreateDebtResponse>(
            new CreateDebtRequested
            {
                CorrelationId = correlationId,
                UserId = request.UserId,
                Amount = request.Amount,
                RequestedBy = _currentUserService.UserId ?? Guid.Empty
            });

        if (!response.Message.Success)
            throw new DomainException(response.Message.FailureReason!);

        return Ok(response.Message.DebtId);
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
    
    [Idempotent]
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
