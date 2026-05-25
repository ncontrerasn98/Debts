namespace Debts.API.Contracts.Requests;

public record RegisterWebhookRequest(string Url, string EventType);