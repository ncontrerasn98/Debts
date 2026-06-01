using Debts.Domain.Exceptions;

namespace Debts.Domain.Entities;

public class Debt
{
    public Guid Id { get; private set; }
    public decimal OriginalAmount { get; private set; }
    public decimal? NegotiatedAmount { get; private set; }
    public bool IsSettled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public Guid UserId { get; private set; }

    public User User { get; set; }

    private Debt() { }

    public Debt(decimal originalAmount, Guid userId)
    {
        if (originalAmount <= 0)
            throw new Exception("Original amount must be greater than 0");

        if (userId == Guid.Empty)
            throw new Exception("UserId is required");

        Id = Guid.NewGuid();
        OriginalAmount = originalAmount;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        IsSettled = false;
    }

    public void Negotiate(decimal percentage)
    {
        if (percentage <= 0 || percentage > 1)
            throw new DomainException("Invalid negotiation percentage");

        if (NegotiatedAmount.HasValue)
            throw new DomainException("Debt already negotiated");

        NegotiatedAmount = OriginalAmount * percentage;
    }

    public void MarkAsSettled()
    {
        if (IsSettled)
            throw new DomainException("Debt already settled");

        IsSettled = true;
    }
    
    public void MarkAsUnsettled()
    {
        IsSettled = false;
    }
}
