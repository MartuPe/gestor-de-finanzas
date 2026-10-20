using Finanzas.Api.Http;
using Finanzas.Application.Abstractions;
using Finanzas.Application.CreditLines.GetDebtSchedule;
using Finanzas.Application.ExchangeRates.GetLatestRates;
using Finanzas.Application.Months.GetMonthSummary;
using Finanzas.Application.Recurring.GenerateRecurring;
using Finanzas.Application.Reports.GetSpending;

namespace Finanzas.Api.Endpoints;

/// <summary>Pantalla del mes, reportes, deuda de tarjetas y cotizaciones.</summary>
internal static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder api)
    {
        // Abrir un mes genera antes sus fijos (R7, a demanda): así entran como previstos en el disponible.
        api.MapGet("/months/{period}", async (
                string period,
                GenerateRecurringHandler generate,
                GetMonthSummaryHandler summary,
                CancellationToken cancellationToken) =>
            {
                if (QueryValues.Period(period) is not { } month)
                {
                    return QueryValues.Invalid(nameof(period), period);
                }

                var generated = await generate.HandleAsync(new GenerateRecurringCommand(month), cancellationToken);
                if (generated.IsFailure)
                {
                    return generated.Error.ToProblem();
                }

                return (await summary.HandleAsync(new GetMonthSummaryQuery(month), cancellationToken)).ToHttp();
            })
            .WithTags("Mes")
            .Returns<MonthSummaryResponse>();

        api.MapGet("/reports/spending", async (
                string period,
                string? by,
                string? mode,
                GetSpendingHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (QueryValues.Period(period) is not { } month)
                {
                    return QueryValues.Invalid(nameof(period), period);
                }

                if (QueryValues.Enum<SpendingGrouping>(by, SpendingGrouping.Category) is not { } grouping)
                {
                    return QueryValues.Invalid(nameof(by), by);
                }

                if (QueryValues.Enum<SpendingMode>(mode, SpendingMode.Due) is not { } spendingMode)
                {
                    return QueryValues.Invalid(nameof(mode), mode);
                }

                return (await handler.HandleAsync(new GetSpendingQuery(month, grouping, spendingMode), cancellationToken)).ToHttp();
            })
            .WithTags("Reportes")
            .Returns<SpendingResponse>();

        api.MapGet("/credit-lines/debt", async (
                string from,
                int? months,
                GetDebtScheduleHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (QueryValues.Period(from) is not { } start)
                {
                    return QueryValues.Invalid(nameof(from), from);
                }

                return (await handler.HandleAsync(new GetDebtScheduleQuery(start, months ?? 6), cancellationToken)).ToHttp();
            })
            .WithTags("Tarjetas y créditos")
            .Returns<DebtScheduleResponse>();

        api.MapGet("/exchange-rates/latest", async (GetLatestRatesHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(cancellationToken)).ToHttp())
            .WithTags("Cotizaciones")
            .Returns<LatestRatesResponse>();
    }
}
