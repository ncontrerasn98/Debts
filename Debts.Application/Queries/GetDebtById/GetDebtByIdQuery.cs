using Debts.Application.DTOs;
using MediatR;

namespace Debts.Application.Queries.GetDebtById;

public class GetDebtByIdQuery  : IRequest<DebtDto>
{
    public Guid Id { get; set; }
}