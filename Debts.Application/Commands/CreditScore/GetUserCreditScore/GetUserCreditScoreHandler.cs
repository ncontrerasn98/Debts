using Debts.Application.Abstractions.CreditScore;
using Debts.Domain.Exceptions;
using MediatR;

namespace Debts.Application.Commands.CreditScore.GetUserCreditScore;

public class GetUserCreditScoreHandler : IRequestHandler<GetUserCreditScoreQuery, CreditScoreResult?>
{
    private readonly ICreditScoreService _creditScoreService;

    public GetUserCreditScoreHandler(ICreditScoreService creditScoreService)
        => _creditScoreService = creditScoreService;

    public async Task<CreditScoreResult?> Handle(
        GetUserCreditScoreQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _creditScoreService.GetScoreAsync(query.UserId, cancellationToken);

        if (result is null)
            throw new NotFoundException($"No credit history found for user {query.UserId}");

        return result;
    }
}