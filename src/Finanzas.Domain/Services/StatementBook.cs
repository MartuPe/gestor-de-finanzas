using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;

namespace Finanzas.Domain.Services;

/// <summary>
/// Los resúmenes de una línea de crédito, por período. Si falta uno, lo estima con los días habituales
/// y lo anota en <see cref="Created"/> para que se guarde junto con la operación.
/// </summary>
internal sealed class StatementBook
{
    private readonly Account _account;
    private readonly Dictionary<Period, Statement> _byPeriod = [];
    private readonly List<Statement> _created = [];

    public StatementBook(Account creditAccount, IEnumerable<Statement> statements)
    {
        if (!creditAccount.IsCredit)
        {
            throw new ArgumentException("Solo las cuentas de crédito tienen resúmenes.", nameof(creditAccount));
        }

        _account = creditAccount;
        foreach (var statement in statements.Where(statement => statement.CreditAccountId == creditAccount.Id))
        {
            if (!_byPeriod.TryAdd(statement.Period, statement))
            {
                throw new ArgumentException($"Hay dos resúmenes para {statement.Period}.", nameof(statements));
            }
        }
    }

    public IReadOnlyList<Statement> Created => _created;

    /// <summary>El resumen de ese período: el existente o uno estimado nuevo.</summary>
    public Statement For(Period period)
    {
        if (_byPeriod.TryGetValue(period, out var statement))
        {
            return statement;
        }

        statement = Statement.Estimate(_account, period);
        _byPeriod.Add(period, statement);
        _created.Add(statement);
        return statement;
    }

    /// <summary>
    /// R4: el primer resumen cuya fecha de corte es igual o posterior a la compra.
    /// Solo crea el resumen elegido, no los que se descartan en el camino.
    /// </summary>
    public Statement FirstAccepting(DateOnly purchaseDate)
    {
        // Con fechas válidas alcanza con mirar dos o tres meses; el tope evita un bucle infinito.
        const int MaxLookahead = 24;

        var period = Period.FromDate(purchaseDate);
        for (var i = 0; i < MaxLookahead; i++, period = period.Next())
        {
            var cutoff = _byPeriod.TryGetValue(period, out var existing)
                ? existing.Cutoff
                : EstimatedCutoff(period);

            if (purchaseDate <= cutoff)
            {
                return For(period);
            }
        }

        throw new InvalidOperationException($"Ningún resumen de los próximos {MaxLookahead} meses acepta una compra del {purchaseDate}.");
    }

    private DateOnly EstimatedCutoff(Period period)
    {
        var (closing, due) = _account.CreditLine!.EstimateDates(period);
        return closing ?? due.AddDays(-1);
    }
}
