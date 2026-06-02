using Microsoft.AspNetCore.SignalR;

namespace CreditScore.Api.Hubs;

public class CreditScoreHub : Hub
{
    private readonly ILogger<CreditScoreHub> _logger;

    public CreditScoreHub(ILogger<CreditScoreHub> logger)
        => _logger = logger;

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        _logger.LogInformation(
            "Client {ConnectionId} joined group user-{UserId}",
            Context.ConnectionId, userId);
    }
}