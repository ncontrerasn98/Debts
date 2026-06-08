namespace Notification.Service.Entities;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool Sent { get; private set; }
    public string? Error { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Notification() { }

    public Notification(Guid userId, string type, string channel, string content)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Channel = channel;
        Content = content;
        Sent = false;
        CreatedAt = DateTime.UtcNow;
    }

    public static class Types
    {
        public const string DebtSettled = "DEBT_SETTLED";
        public const string DebtCreated = "DEBT_CREATED";          // ← nuevo
        public const string DebtCompensated = "DEBT_COMPENSATED";  // ← nuevo
        public const string CreditScoreUpdated = "CREDIT_SCORE_UPDATED";
        public const string LowCreditScore = "LOW_CREDIT_SCORE";
    }

    public static class Channels
    {
        public const string Email = "EMAIL";
        public const string SignalR = "SIGNALR";
    }

    public void MarkSent() => Sent = true;
    public void MarkFailed(string error)
    {
        Sent = false;
        Error = error;
    }
}