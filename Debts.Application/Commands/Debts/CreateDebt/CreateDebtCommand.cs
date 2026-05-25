using MediatR;

namespace Debts.Application.Commands.Debts.CreateDebt;

public class CreateDebtCommand : IRequest<Guid>
{
    public decimal Amount { get; set; }
    public Guid UserId { get; set; }
}