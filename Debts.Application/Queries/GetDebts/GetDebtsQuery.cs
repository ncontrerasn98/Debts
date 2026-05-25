using Debts.Application.DTOs;
using MediatR;

namespace Debts.Application.Queries.GetDebts;

public class GetDebtsQuery : IRequest<List<DebtDto>>
{
    
}