namespace Debts.Infrastructure.Messaging.RabbitMq.Constants;

public static class RabbitMqConstants
{
    public static class Exchanges
    {
        public const string Topic  = "debts.topic";
        public const string Fanout = "debts.fanout";
    }

    public static class RoutingKeys
    {
        public const string DebtCreatedCo = "debt.created.co";
        public const string DebtCreatedUs = "debt.created.us";
        public const string DebtCreated   = "debt.created";
    }

    public static class Queues
    {
        // Topic
        public const string CreditScoreDebtEvents   = "creditscore.debt.events";
        public const string NotificationDebtCreated = "notification.debt.created";
        public const string CreditScoreDebtCompensated = "creditscore.debt.compensated";
        public const string NotificationDebtCompensated = "notification.debt.compensated";

        // Fanout
        public const string CreditScoreFanout   = "creditscore.fanout";
        public const string NotificationFanout  = "notification.fanout";
    }

    public static class Patterns
    {
        public const string AllDebtEvents    = "debt.#";
        public const string DebtCreatedOnly  = "debt.created.#";
        public const string DebtCompensated  = "debt.compensated.#";
    }
}