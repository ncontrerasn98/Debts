using Debts.API.Contracts.Requests;
using Debts.Application.Commands.Webhooks.Deactivate;
using Debts.Application.Commands.Webhooks.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Debts.API.Controllers;

[ApiController]
[Route("webhooks")]
[Authorize(Roles = "Admin")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
        => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Register(RegisterWebhookRequest request)
    {
        var id = await _mediator.Send(new RegisterWebhookCommand
        {
            Url = request.Url,
            EventType = request.EventType
        });

        return Ok(id);
    }

    [HttpDelete("{webhookId}")]
    public async Task<IActionResult> Deactivate(Guid webhookId)
    {
        await _mediator.Send(new DeactivateWebhookCommand
        {
            WebhookId = webhookId
        });

        return NoContent();
    }
}