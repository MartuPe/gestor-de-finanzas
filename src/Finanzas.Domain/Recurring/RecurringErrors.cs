using Finanzas.Domain.Common;

namespace Finanzas.Domain.Recurring;

public static class RecurringErrors
{
    public static readonly Error InvalidDescription =
        Error.Validation("recurring.invalid_description", $"El fijo necesita una descripción de hasta {RecurringTemplate.DescriptionMaxLength} caracteres.");

    public static readonly Error AmountMustBePositive =
        Error.Validation("recurring.amount_not_positive", "El monto estimado tiene que ser mayor a cero.");

    public static readonly Error InvalidInterval =
        Error.Validation("recurring.invalid_interval", $"El intervalo tiene que estar entre 1 y {RecurringTemplate.MaxInterval}.");

    public static readonly Error InvalidDay =
        Error.Validation("recurring.invalid_day", "El día del mes tiene que estar entre 1 y 31.");

    public static readonly Error EndsBeforeStart =
        Error.Validation("recurring.ends_before_start", "La fecha de fin no puede ser anterior a la de inicio.");

    public static readonly Error ArchivedAccount =
        Error.Rule("recurring.account_archived", "La cuenta está archivada.");

    public static readonly Error DestinationOnlyForTransfer =
        Error.Validation("recurring.destination_only_transfer", "Solo una transferencia tiene cuenta de destino.");

    public static readonly Error TransferNeedsDestination =
        Error.Validation("recurring.transfer_needs_destination", "Una transferencia necesita una cuenta de destino.");

    public static readonly Error InvalidDestination =
        Error.Validation("recurring.invalid_destination", "El destino tiene que ser otra cuenta del mismo libro.");

    public static readonly Error TransferWithCredit =
        Error.Rule("recurring.transfer_with_credit", "Una transferencia programada no puede involucrar una tarjeta o crédito.");

    public static readonly Error IncomeOnCredit =
        Error.Rule("recurring.income_on_credit", "Un ingreso no puede ir a una cuenta de crédito.");

    public static readonly Error CategoryRequired =
        Error.Validation("recurring.category_required", "Un gasto o ingreso fijo necesita una categoría.");

    public static readonly Error CategoryFromAnotherLedger =
        Error.Rule("recurring.category_other_ledger", "La categoría es de otro libro.");

    public static readonly Error CategoryKindMismatch =
        Error.Rule("recurring.category_kind", "La categoría no corresponde al tipo de fijo.");

    public static readonly Error AccountNotFound =
        Error.NotFound("recurring.account_not_found", "No se encontró la cuenta del fijo.");

    public static readonly Error CategoryNotFound =
        Error.NotFound("recurring.category_not_found", "No se encontró la categoría del fijo.");
}
