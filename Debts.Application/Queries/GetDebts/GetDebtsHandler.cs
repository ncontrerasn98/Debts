using Debts.Application.Abstractions.Persistence;
using Debts.Application.DTOs;
using MediatR;

namespace Debts.Application.Queries.GetDebts;

public class GetDebtsHandler  : IRequestHandler<GetDebtsQuery, List<DebtDto>>
{
    private readonly IDebtRepository _repository;

    public GetDebtsHandler(IDebtRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DebtDto>> Handle(GetDebtsQuery query, CancellationToken cancellationToken)
    {
        var debts = await _repository.GetAllAsync();

        return debts.Select(x => new DebtDto
        {
            Id = x.Id,
            Amount = x.OriginalAmount,
            UserId = x.UserId,
            IsSettled = x.IsSettled
        }).ToList();
    }
    
}