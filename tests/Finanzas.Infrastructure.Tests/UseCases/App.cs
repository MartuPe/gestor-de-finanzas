using Finanzas.Application;
using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.CreditLines.GetDebtSchedule;
using Finanzas.Application.ExchangeRates;
using Finanzas.Application.Expenses.RegisterExpense;
using Finanzas.Application.Incomes.RegisterIncome;
using Finanzas.Application.Months.GetMonthSummary;
using Finanzas.Application.Recurring.GenerateRecurring;
using Finanzas.Application.Reports.GetSpending;
using Finanzas.Application.Statements.PayStatement;
using Finanzas.Application.Transactions;
using Finanzas.Application.Transactions.ConfirmPlanned;
using Finanzas.Application.Transfers.RegisterTransfer;
using Finanzas.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Finanzas.Infrastructure.Tests.UseCases;

/// <summary>
/// La aplicación armada como la arma la Api (Application + Infrastructure), contra el SQL Server de los tests,
/// con un reloj fijo y una fuente de cotizaciones falsa. Cada llamada corre en su propio scope, como un pedido HTTP.
/// </summary>
internal sealed class App : IAsyncDisposable
{
    /// <summary>"Hoy" en los tests: 18/10/2026 a las 15 h de Argentina, la fecha de la hoja Octubre2026.</summary>
    public static readonly DateTimeOffset Today = new(2026, 10, 18, 15, 0, 0, TimeSpan.FromHours(-3));

    private readonly ServiceProvider _services;

    private App(ServiceProvider services, FakeRateSource rates)
    {
        _services = services;
        RateSource = rates;
    }

    public FakeRateSource RateSource { get; }

    public static App For(SqlServerFixture fixture, Guid ledgerId, DateTimeOffset? now = null, FakeRateSource? rates = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = fixture.ConnectionString,
            })
            .Build();

        var rateSource = rates ?? FakeRateSource.Unavailable();
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FakeTimeProvider(now ?? Today)));
        services.Replace(ServiceDescriptor.Singleton<IExchangeRateSource>(rateSource));
        services.AddSingleton<ICurrentUser>(new TestUser(ledgerId));

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        return new App(provider, rateSource);
    }

    public static App For(SqlServerFixture fixture, TestLedger data) => For(fixture, data.Ledger.Id);

    public Task<Result<Registration<TransactionResponse>>> RegisterExpenseAsync(RegisterExpenseCommand command, CancellationToken cancellationToken) =>
        Run<RegisterExpenseHandler, Result<Registration<TransactionResponse>>>(handler => handler.HandleAsync(command, cancellationToken));

    public Task<Result<Registration<TransactionResponse>>> RegisterIncomeAsync(RegisterIncomeCommand command, CancellationToken cancellationToken) =>
        Run<RegisterIncomeHandler, Result<Registration<TransactionResponse>>>(handler => handler.HandleAsync(command, cancellationToken));

    public Task<Result<Registration<TransferResponse>>> RegisterTransferAsync(RegisterTransferCommand command, CancellationToken cancellationToken) =>
        Run<RegisterTransferHandler, Result<Registration<TransferResponse>>>(handler => handler.HandleAsync(command, cancellationToken));

    public Task<Result<Registration<PayStatementResponse>>> PayStatementAsync(PayStatementCommand command, CancellationToken cancellationToken) =>
        Run<PayStatementHandler, Result<Registration<PayStatementResponse>>>(handler => handler.HandleAsync(command, cancellationToken));

    public Task<Result<IReadOnlyList<TransactionResponse>>> ConfirmPlannedAsync(ConfirmPlannedCommand command, CancellationToken cancellationToken) =>
        Run<ConfirmPlannedHandler, Result<IReadOnlyList<TransactionResponse>>>(handler => handler.HandleAsync(command, cancellationToken));

    public Task<Result<GenerateRecurringResponse>> GenerateRecurringAsync(Period period, CancellationToken cancellationToken) =>
        Run<GenerateRecurringHandler, Result<GenerateRecurringResponse>>(
            handler => handler.HandleAsync(new GenerateRecurringCommand(period), cancellationToken));

    public Task<Result<MonthSummaryResponse>> GetMonthSummaryAsync(Period period, CancellationToken cancellationToken) =>
        Run<GetMonthSummaryHandler, Result<MonthSummaryResponse>>(
            handler => handler.HandleAsync(new GetMonthSummaryQuery(period), cancellationToken));

    public Task<Result<SpendingResponse>> GetSpendingAsync(GetSpendingQuery query, CancellationToken cancellationToken) =>
        Run<GetSpendingHandler, Result<SpendingResponse>>(handler => handler.HandleAsync(query, cancellationToken));

    public Task<Result<DebtScheduleResponse>> GetDebtScheduleAsync(GetDebtScheduleQuery query, CancellationToken cancellationToken) =>
        Run<GetDebtScheduleHandler, Result<DebtScheduleResponse>>(handler => handler.HandleAsync(query, cancellationToken));

    public Task<T> WithExchangeRatesAsync<T>(Func<ExchangeRateService, Task<T>> action) => Run(action);

    /// <summary>Guarda una cotización del dólar tal como la dejaría una actualización.</summary>
    public Task SetUsdRateAsync(string source, DateOnly date, decimal sell, DateTimeOffset fetchedAt, CancellationToken cancellationToken) =>
        Run<IExchangeRateStore, bool>(async store =>
        {
            await store.UpsertAsync([new ExchangeRate(CurrencyCodes.Usd, source, date, sell - 20m, sell, fetchedAt)], cancellationToken);
            return true;
        });

    public ValueTask DisposeAsync() => _services.DisposeAsync();

    private async Task<TResult> Run<TService, TResult>(Func<TService, Task<TResult>> action)
        where TService : notnull
    {
        await using var scope = _services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private sealed record TestUser(Guid LedgerId) : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
    }
}
