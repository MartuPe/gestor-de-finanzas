using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;

namespace Finanzas.Application.Abstractions;

// Lectores del lado de consulta: Infrastructure hace las sumas en SQL y devuelve datos planos;
// los casos de uso los combinan con las reglas del dominio.

public interface IMonthReader
{
    Task<MonthMovements> GetMovementsAsync(Guid ledgerId, Period period, CancellationToken cancellationToken);
}

/// <summary>Movimientos de las cuentas líquidas de un mes, por moneda.</summary>
/// <param name="LiquidAtStart">Saldo confirmado al empezar el mes.</param>
/// <param name="ConfirmedFlows">Suma con signo de todo lo confirmado en el mes.</param>
/// <param name="Income">Ingresos y rendimientos confirmados del mes.</param>
/// <param name="Spent">Gastos confirmados del mes, en positivo (las devoluciones restan).</param>
/// <param name="Planned">Previstos hasta fin de mes, incluidos los vencidos sin confirmar.</param>
public sealed record MonthMovements(
    MoneyBag LiquidAtStart,
    MoneyBag ConfirmedFlows,
    MoneyBag Income,
    MoneyBag Spent,
    IReadOnlyList<PlannedMovement> Planned);

public sealed record PlannedMovement(
    Guid TransactionId,
    string AccountName,
    DateOnly Date,
    Money Amount,
    TransactionKind Kind,
    string? Description);

public interface IStatementReader
{
    /// <summary>
    /// Cargos y pagos de los resúmenes del libro que vencen entre <paramref name="from"/> y <paramref name="to"/>.
    /// Sin <paramref name="from"/>, desde el primero.
    /// </summary>
    Task<IReadOnlyList<StatementBalance>> GetBalancesAsync(
        Guid ledgerId,
        Period? from,
        Period to,
        bool openOnly,
        CancellationToken cancellationToken);

    Task<StatementBalance?> GetBalanceAsync(Guid statementId, CancellationToken cancellationToken);
}

/// <param name="Charges">Las cuotas que caen en el resumen, por moneda (incluye los cargos previstos).</param>
/// <param name="Payments">Lo pagado, por moneda: la pata de cada pago que entra en la cuenta de crédito.</param>
public sealed record StatementBalance(
    Guid StatementId,
    Guid CreditAccountId,
    string CreditAccountName,
    Period Period,
    DateOnly DueDate,
    StatementStatus Status,
    MoneyBag Charges,
    MoneyBag Payments)
{
    public MoneyBag Outstanding => Statement.Outstanding(Charges, Payments);
}

public interface ISpendingReader
{
    Task<IReadOnlyList<SpendingRow>> GetAsync(
        Guid ledgerId,
        Period period,
        SpendingGrouping by,
        SpendingMode mode,
        CancellationToken cancellationToken);
}

public enum SpendingGrouping
{
    Category,
    Tag,
}

/// <summary>R10: cuándo cuenta una compra a crédito.</summary>
public enum SpendingMode
{
    /// <summary>Cada cuota, en el mes en que vence su resumen (por defecto: como pensás tu presupuesto).</summary>
    Due,

    /// <summary>La compra entera, en el mes en que se hizo.</summary>
    Purchase,
}

/// <param name="GroupId">La categoría o la etiqueta; null para "sin categoría" o "sin etiqueta".</param>
/// <param name="Amount">Lo gastado, en positivo.</param>
public sealed record SpendingRow(Guid? GroupId, Money Amount);

public interface IAccountReader
{
    /// <summary>R1: el saldo de cada cuenta del libro, por moneda.</summary>
    Task<IReadOnlyDictionary<Guid, AccountBalance>> GetBalancesAsync(Guid ledgerId, CancellationToken cancellationToken);
}

/// <param name="Confirmed">Suma de los movimientos confirmados. En una cuenta de crédito es la deuda, en negativo.</param>
/// <param name="Projected">Lo mismo, sumando además los previstos.</param>
public sealed record AccountBalance(MoneyBag Confirmed, MoneyBag Projected);

public interface ITransactionReader
{
    Task<TransactionPage> ListAsync(Guid ledgerId, TransactionFilter filter, CancellationToken cancellationToken);
}

/// <param name="Page">Empieza en 1.</param>
public sealed record TransactionFilter(
    DateOnly From,
    DateOnly To,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? TagId,
    TransactionStatus? Status,
    int Page,
    int PageSize);

/// <param name="Total">Cuántos movimientos cumplen el filtro, sin paginar.</param>
public sealed record TransactionPage(IReadOnlyList<TransactionListItem> Items, int Total);

/// <param name="Installments">Cantidad de cuotas, si es una compra a crédito.</param>
public sealed record TransactionListItem(
    Guid Id,
    DateOnly Date,
    Guid AccountId,
    string AccountName,
    TransactionKind Kind,
    TransactionStatus Status,
    decimal Amount,
    string Currency,
    Guid? CategoryId,
    string? CategoryName,
    string? Description,
    Guid? TransferGroupId,
    IReadOnlyList<Guid> TagIds,
    int? Installments);
