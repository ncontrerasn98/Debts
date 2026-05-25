namespace Debts.API.Contracts.Requests;

public class CreateDebtRequest
{
    public decimal Amount { get; set; }
    public Guid UserId { get; set; }
}