using System.Globalization;
using Finanzas.Application.Accounts.CreateAccount;
using Finanzas.Application.Accounts.CreateCreditAccount;
using Finanzas.Application.Accounts.ListAccounts;
using Finanzas.Application.Accounts.SetAccountArchived;
using Finanzas.Application.Accounts.SetOpeningBalance;
using Finanzas.Application.Accounts.UpdateAccount;
using Finanzas.Application.Categories.CreateCategory;
using Finanzas.Application.Categories.ListCategories;
using Finanzas.Application.Categories.UpdateCategory;
using Finanzas.Application.CreditLines.GetDebtSchedule;
using Finanzas.Application.ExchangeRates;
using Finanzas.Application.ExchangeRates.GetLatestRates;
using Finanzas.Application.Expenses.RegisterExpense;
using Finanzas.Application.Incomes.RegisterIncome;
using Finanzas.Application.Ledgers.CreateLedger;
using Finanzas.Application.Ledgers.GetLedgerSettings;
using Finanzas.Application.Ledgers.UpdateLedgerSettings;
using Finanzas.Application.Months.GetMonthSummary;
using Finanzas.Application.Recurring.CreateRecurringTemplate;
using Finanzas.Application.Recurring.GenerateRecurring;
using Finanzas.Application.Recurring.ListRecurringTemplates;
using Finanzas.Application.Recurring.UpdateRecurringTemplate;
using Finanzas.Application.Reports.GetSpending;
using Finanzas.Application.Statements.PayStatement;
using Finanzas.Application.Tags.ListTags;
using Finanzas.Application.Tags.SaveTag;
using Finanzas.Application.Transactions.ConfirmPlanned;
using Finanzas.Application.Transactions.GetTransaction;
using Finanzas.Application.Transactions.ListTransactions;
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

        // Libro y catálogos
        services.AddScoped<CreateLedgerHandler>();
        services.AddScoped<GetLedgerSettingsHandler>();
        services.AddScoped<UpdateLedgerSettingsHandler>();
        services.AddScoped<CreateAccountHandler>();
        services.AddScoped<CreateCreditAccountHandler>();
        services.AddScoped<UpdateAccountHandler>();
        services.AddScoped<SetAccountArchivedHandler>();
        services.AddScoped<SetOpeningBalanceHandler>();
        services.AddScoped<ListAccountsHandler>();
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<ListCategoriesHandler>();
        services.AddScoped<SaveTagHandler>();
        services.AddScoped<ListTagsHandler>();
        services.AddScoped<CreateRecurringTemplateHandler>();
        services.AddScoped<UpdateRecurringTemplateHandler>();
        services.AddScoped<ListRecurringTemplatesHandler>();

        // Movimientos
        services.AddScoped<RegisterExpenseHandler>();
        services.AddScoped<RegisterIncomeHandler>();
        services.AddScoped<RegisterTransferHandler>();
        services.AddScoped<PayStatementHandler>();
        services.AddScoped<ConfirmPlannedHandler>();
        services.AddScoped<GenerateRecurringHandler>();
        services.AddScoped<ListTransactionsHandler>();
        services.AddScoped<GetTransactionHandler>();

        // Consultas
        services.AddScoped<GetMonthSummaryHandler>();
        services.AddScoped<GetSpendingHandler>();
        services.AddScoped<GetDebtScheduleHandler>();
        services.AddScoped<GetLatestRatesHandler>();

        return services;
    }
}
