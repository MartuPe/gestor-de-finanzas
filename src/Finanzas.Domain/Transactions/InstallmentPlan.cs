using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;

namespace Finanzas.Domain.Transactions;

public sealed record PlannedInstallment(int Number, int Total, Money Amount, Statement Statement);

/// <summary>
/// Cómo se reparte una compra a crédito en cuotas y en qué resumen cae cada una.
/// Lo calcula <c>InstallmentScheduler</c>.
/// </summary>
/// <param name="NewStatements">Resúmenes estimados que hubo que crear y que hay que guardar junto con la compra.</param>
public sealed record InstallmentPlan(
    Guid CreditAccountId,
    IReadOnlyList<PlannedInstallment> Installments,
    IReadOnlyList<Statement> NewStatements)
{
    public Money Total => Installments.Select(installment => installment.Amount).Aggregate((sum, amount) => sum + amount);
}
