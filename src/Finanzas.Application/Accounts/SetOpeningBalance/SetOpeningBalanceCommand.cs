namespace Finanzas.Application.Accounts.SetOpeningBalance;

/// <summary>
/// El saldo con el que arranca una cuenta (un movimiento <c>opening</c>). Hay uno por cuenta y moneda:
/// después, las diferencias se corrigen conciliando el saldo (R12).
/// </summary>
/// <param name="Amount">Con signo: una tarjeta que ya debe algo arranca en negativo.</param>
public sealed record SetOpeningBalanceCommand(
    Guid TransactionId,
    Guid AccountId,
    DateOnly Date,
    decimal Amount,
    string Currency);
