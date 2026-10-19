using System.Globalization;
using Finanzas.Application.CreditLines.GetDebtSchedule;
using Finanzas.Application.ExchangeRates;
using Finanzas.Application.Expenses.RegisterExpense;
using Finanzas.Application.Incomes.RegisterIncome;
using Finanzas.Application.Months.GetMonthSummary;
using Finanzas.Application.Recurring.GenerateRecurring;
using Finanzas.Application.Reports.GetSpending;
using Finanzas.Application.Statements.PayStatement;
using Finanzas.Application.Transactions.ConfirmPlanned;
using Finanzas.Application.Transfers.RegisterTransfer;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Finanzas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ExchangeRateService>();

        // Escritura
        services.AddScoped<RegisterExpenseHandler>();
        services.AddScoped<RegisterIncomeHandler>();
        services.AddScoped<RegisterTransferHandler>();
        services.AddScoped<PayStatementHandler>();
        services.AddScoped<ConfirmPlannedHandler>();
        services.AddScoped<GenerateRecurringHandler>();

        // Consultas
        services.AddScoped<GetMonthSummaryHandler>();
        services.AddScoped<GetSpendingHandler>();
        services.AddScoped<GetDebtScheduleHandler>();

        return services;
    }
}
