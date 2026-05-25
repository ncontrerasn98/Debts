using MediatR;

namespace Debts.Application.Commands.SettleDebt;

public class SettleDebtCommand : IRequest<Unit>
{
    public Guid DebtId { get; set; }
}