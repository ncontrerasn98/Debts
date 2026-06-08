namespace CreditScore.Api.Entities;

public class CreditHistory
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int TotalDebts { get; private set; }
    public int SettledDebts { get; private set; }
    public int ActiveDebts { get; private set; }
    public decimal TotalOriginalAmount { get; private set; }
    public decimal TotalNegotiatedAmount { get; private set; }
    public int LowNegotiations { get; private set; } // negoció menos del 50% del original
    public DateTime OldestDebtDate { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    private List<CreditHistoryEvent> _events = new();
    public IReadOnlyCollection<CreditHistoryEvent> Events => _events.AsReadOnly();

    public CreditHistory() { }

    public CreditHistory(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        OldestDebtDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyDebtSettled(
        decimal originalAmount,
        decimal? negotiatedAmount,
        DateTime createdAt)
    {
        TotalDebts++;
        SettledDebts++;
        TotalOriginalAmount += originalAmount;

        if (negotiatedAmount.HasValue)
        {
            TotalNegotiatedAmount += negotiatedAmount.Value;

            // penalización si negoció menos del 50%
            if (negotiatedAmount.Value < originalAmount * 0.5m)
                LowNegotiations++;
        }

        if (createdAt < OldestDebtDate)
            OldestDebtDate = createdAt;

        UpdatedAt = DateTime.UtcNow;
    }

    public bool ApplyDebtCreated(Guid debtId, decimal amount)
    {
        // Idempotencia — si ya procesamos este debtId, ignorar
        if (_events.Any(e => e.DebtId == debtId && e.EventType == CreditHistoryEvent.Types.Created))
            return false;

        TotalDebts++;
        ActiveDebts++;
        UpdatedAt = DateTime.UtcNow;

        _events.Add(new CreditHistoryEvent(Id, debtId, amount, CreditHistoryEvent.Types.Created));
        return true;
    }
    
    public void ReverseDebtSettled(
        decimal originalAmount,
        decimal? negotiatedAmount)
    {
        SettledDebts--;
        TotalDebts--;
        TotalOriginalAmount -= originalAmount;

        if (negotiatedAmount.HasValue)
        {
            TotalNegotiatedAmount -= negotiatedAmount.Value;

            if (negotiatedAmount.Value < originalAmount * 0.5m)
                LowNegotiations--;
        }

        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool RevertDebtCreated(Guid debtId)
    {
        var createdEvent = _events
            .FirstOrDefault(e => e.DebtId == debtId
                                 && e.EventType == CreditHistoryEvent.Types.Created);

        // Nunca procesamos este DebtCreatedEvent — ignorar
        if (createdEvent is null)
            return false;

        TotalDebts--;
        ActiveDebts--;
        UpdatedAt = DateTime.UtcNow;

        _events.Add(new CreditHistoryEvent(Id, debtId, createdEvent.Amount, CreditHistoryEvent.Types.Compensated));
        return true;
    }
}