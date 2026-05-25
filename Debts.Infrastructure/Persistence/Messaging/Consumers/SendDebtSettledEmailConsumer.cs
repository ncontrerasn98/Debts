using Debts.Application.Abstractions.Email;
using Debts.Application.Messaging.Commands;
using Debts.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Debts.Infrastructure.Persistence.Messaging.Consumers;

public class SendDebtSettledEmailConsumer : IConsumer<SendDebtSettledEmailCommand>
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SendDebtSettledEmailConsumer> _logger;

    public SendDebtSettledEmailConsumer(ILogger<SendDebtSettledEmailConsumer> logger, AppDbContext dbContext, IEmailSender emailSender)
    {
        _logger = logger;
        _dbContext = dbContext;
        _emailSender = emailSender;
    }

    public async Task Consume(ConsumeContext<SendDebtSettledEmailCommand> context)
    {
        var message = context.Message;

        var consumerName = nameof(SendDebtSettledEmailConsumer);

        var alreadyProcessed =
            await _dbContext.InboxMessages.AnyAsync(
                x =>
                    x.MessageId == message.MessageId &&
                    x.Consumer == consumerName);

        if (alreadyProcessed)
            return;
        
        await _emailSender.SendAsync(
            to: message.Email,
            subject: "Your debt has been settled",
            body: $"""
                   <h2>Debt Settled</h2>
                   <p>Your debt <strong>{message.DebtId}</strong> was settled on {message.SettledAt:dd MMM yyyy HH:mm} UTC.</p>
                   <p>Thank you for your payment.</p>
                   """);

        _logger.LogInformation("Sending settled debt email for debt {DebtId}", message.DebtId);

        _dbContext.InboxMessages.Add(new InboxMessage
            {
                MessageId = message.MessageId,
                Consumer = consumerName,
                ProcessedOnUtc = DateTime.UtcNow
            });

        await _dbContext.SaveChangesAsync();
        
        await Task.CompletedTask;
    }
}