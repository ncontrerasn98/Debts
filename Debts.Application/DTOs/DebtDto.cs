namespace Debts.Application.DTOs;

public class DebtDto
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public Guid UserId { get; set; }

    public bool IsSettled { get; set; }
}