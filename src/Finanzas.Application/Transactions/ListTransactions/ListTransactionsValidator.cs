using FluentValidation;

namespace Finanzas.Application.Transactions.ListTransactions;

public sealed class ListTransactionsValidator : AbstractValidator<ListTransactionsQuery>
{
    public const int MaxPageSize = 200;

    public ListTransactionsValidator()
    {
        RuleFor(query => query.To).GreaterThanOrEqualTo(query => query.From)
            .WithMessage("La fecha final no puede ser anterior a la inicial.");
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(query => query.Status).IsInEnum();
    }
}
