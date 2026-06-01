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

    public void ApplyDebtCreated()
    {
        TotalDebts++;
        ActiveDebts++;
        UpdatedAt = DateTime.UtcNow;
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
}