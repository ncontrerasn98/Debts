using Debts.Application.Commands.Debts.CreateDebt;
using FluentValidation;

namespace Debts.Application.Validators;

public class CreateDebtCommandValidator : AbstractValidator<CreateDebtCommand>
{
    public CreateDebtCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0);
    }
}