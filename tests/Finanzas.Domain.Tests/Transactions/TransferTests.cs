using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Transactions;

public class TransferTests
{
    [Fact]
    public void Comprar_dolares_deja_registrada_la_cotizacion_real()
    {
        var legs = TransferFactory.CreateTransfer(
            Guid.NewGuid(), Guid.NewGuid(), Banco(), Dolares(), Fecha(2026, 4, 20), Money.Ars(155_500), Money.Usd(100)).Value;

        Assert.Equal(-155_500m, legs.Out.Amount);
        Assert.Equal(100m, legs.In.Amount);
        Assert.Equal(legs.Out.TransferGroupId, legs.In.TransferGroupId);
        Assert.Equal(1_555m, legs.ImpliedRate(CurrencyCodes.Ars));
    }

    [Fact]
    public void En_la_misma_moneda_sale_lo_mismo_que_entra()
    {
        var result = TransferFactory.CreateTransfer(
            Guid.NewGuid(), Guid.NewGuid(), Banco(), MercadoPago(), Fecha(2026, 4, 20), Money.Ars(10_000), Money.Ars(9_000));

        Assert.Equal(TransferErrors.SameCurrencyDifferentAmounts, result.Error);
    }

    [Fact]
    public void A_una_tarjeta_no_se_transfiere_se_paga_su_resumen()
    {
        var result = TransferFactory.CreateTransfer(
            Guid.NewGuid(), Guid.NewGuid(), Banco(), Visa(), Fecha(2026, 4, 20), Money.Ars(10_000), Money.Ars(10_000));

        Assert.Equal(TransferErrors.CreditAccountsUseCardPayment, result.Error);
    }

    [Fact]
    public void El_resumen_de_Patagonia_queda_pagado_cuando_se_cubren_pesos_y_dolares()
    {
        // Julio de 2026: en el mismo resumen, "botines papá" en pesos y Claude en dólares.
        var patagonia = Patagonia();
        var botinesPlan = InstallmentScheduler.Plan(patagonia, Fecha(2026, 6, 10), Money.Ars(50_000), 1, []).Value;
        var resumen = botinesPlan.Installments[0].Statement;
        var claudePlan = InstallmentScheduler.Plan(patagonia, Fecha(2026, 6, 12), Money.Usd(40), 1, [resumen]).Value;
        var botines = Transaction.CreateExpense(
            Guid.NewGuid(), patagonia, Fecha(2026, 6, 10), Money.Ars(50_000), Gasto("Regalos"), "botines papá", botinesPlan).Value;
        var claude = Transaction.CreateExpense(
            Guid.NewGuid(), patagonia, Fecha(2026, 6, 12), Money.Usd(40), Gasto("Suscripciones"), "Claude", claudePlan).Value;

        Assert.Equal(Mes(2026, 7), resumen.Period);
        Assert.Same(resumen, claudePlan.Installments[0].Statement);
        var cargos = resumen.ChargesFrom(botines.Installments.Concat(claude.Installments));
        Assert.Equal(50_000m, cargos[CurrencyCodes.Ars]);
        Assert.Equal(40m, cargos[CurrencyCodes.Usd]);

        var pesos = TransferFactory.CreateCardPayment(
            Guid.NewGuid(), Guid.NewGuid(), Banco(), patagonia, resumen, Fecha(2026, 7, 9), Money.Ars(50_000), Money.Ars(50_000)).Value;
        var pagos = resumen.PaymentsFrom([pesos.Out, pesos.In]);
        resumen.Settle(cargos, pagos);

        Assert.Equal(StatementStatus.Open, resumen.Status);
        Assert.Equal(40m, Statement.Outstanding(cargos, pagos)[CurrencyCodes.Usd]);

        var dolares = TransferFactory.CreateCardPayment(
            Guid.NewGuid(), Guid.NewGuid(), Dolares(), patagonia, resumen, Fecha(2026, 7, 9), Money.Usd(40), Money.Usd(40)).Value;
        resumen.Settle(cargos, resumen.PaymentsFrom([pesos.Out, pesos.In, dolares.Out, dolares.In]));

        Assert.Equal(StatementStatus.Paid, resumen.Status);
    }

    [Fact]
    public void No_se_paga_el_resumen_de_otra_tarjeta()
    {
        var visa = Visa();
        var resumenDeMastercard = InstallmentScheduler.Plan(Mastercard(), Fecha(2025, 9, 6), Money.Ars(9_000), 1, []).Value
            .Installments[0].Statement;

        var result = TransferFactory.CreateCardPayment(
            Guid.NewGuid(), Guid.NewGuid(), Banco(), visa, resumenDeMastercard, Fecha(2025, 10, 5), Money.Ars(9_000), Money.Ars(9_000));

        Assert.Equal(TransferErrors.StatementOfAnotherAccount, result.Error);
    }

    [Fact]
    public void El_ahorro_previsto_se_confirma_con_sus_dos_patas()
    {
        var legs = TransferFactory.CreateTransfer(
            Guid.NewGuid(), Guid.NewGuid(), Banco(), Ahorro(), Fecha(2026, 10, 5), Money.Ars(800_000), Money.Ars(800_000),
            "Ahorro mensual", TransactionStatus.Planned).Value;

        Assert.Equal(TransactionErrors.ConfirmTransferTogether, legs.Out.Confirm().Error);
        Assert.True(TransferFactory.Confirm(legs, sent: Money.Ars(700_000), received: Money.Ars(700_000)).IsSuccess);

        Assert.Equal(-700_000m, legs.Out.Amount);
        Assert.Equal(700_000m, legs.In.Amount);
        Assert.False(legs.Out.IsPlanned || legs.In.IsPlanned);
    }
}
