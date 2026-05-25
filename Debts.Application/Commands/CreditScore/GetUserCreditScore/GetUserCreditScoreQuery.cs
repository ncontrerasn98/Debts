using Debts.Application.Abstractions.CreditScore;
using MediatR;

namespace Debts.Application.Commands.CreditScore.GetUserCreditScore;

public class GetUserCreditScoreQuery : IRequest<CreditScoreResult?>
{
    public Guid UserId { get; set; }
}