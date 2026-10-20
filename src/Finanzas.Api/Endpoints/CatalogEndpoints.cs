using Finanzas.Api.Http;
using Finanzas.Application.Accounts;
using Finanzas.Application.Accounts.CreateAccount;
using Finanzas.Application.Accounts.CreateCreditAccount;
using Finanzas.Application.Accounts.ListAccounts;
using Finanzas.Application.Accounts.SetAccountArchived;
using Finanzas.Application.Accounts.SetOpeningBalance;
using Finanzas.Application.Accounts.UpdateAccount;
using Finanzas.Application.Categories;
using Finanzas.Application.Categories.CreateCategory;
using Finanzas.Application.Categories.ListCategories;
using Finanzas.Application.Categories.UpdateCategory;
using Finanzas.Application.Ledgers;
using Finanzas.Application.Ledgers.GetLedgerSettings;
using Finanzas.Application.Ledgers.UpdateLedgerSettings;
using Finanzas.Application.Recurring;
using Finanzas.Application.Recurring.CreateRecurringTemplate;
using Finanzas.Application.Recurring.ListRecurringTemplates;
using Finanzas.Application.Recurring.UpdateRecurringTemplate;
using Finanzas.Application.Tags;
using Finanzas.Application.Tags.ListTags;
using Finanzas.Application.Tags.SaveTag;
using Finanzas.Application.Transactions;

namespace Finanzas.Api.Endpoints;

/// <summary>Libro, cuentas, categorías, etiquetas y fijos (02-backend §3.2).</summary>
internal static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder api)
    {
        var ledger = api.MapGroup("/ledger").WithTags("Libro");
        ledger.MapGet("/settings", async (GetLedgerSettingsHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(cancellationToken)).ToHttp())
            .Returns<LedgerSettingsResponse>();
        ledger.MapPut("/settings", async (UpdateLedgerSettingsCommand command, UpdateLedgerSettingsHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToHttp())
            .Returns<LedgerSettingsResponse>();

        var accounts = api.MapGroup("/accounts").WithTags("Cuentas");
        accounts.MapGet("/", async (ListAccountsHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(cancellationToken)).ToHttp())
            .Returns<IReadOnlyList<AccountResponse>>();
        accounts.MapPost("/", async (CreateAccountCommand command, CreateAccountHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToCreatedHttp())
            .ReturnsCreated<AccountResponse>();
        accounts.MapPost("/credit", async (CreateCreditAccountCommand command, CreateCreditAccountHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToCreatedHttp())
            .ReturnsCreated<AccountResponse>();
        accounts.MapPut("/{id:guid}", async (Guid id, UpdateAccountRequest request, UpdateAccountHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(
                    new UpdateAccountCommand(id, request.Name, request.Color, request.Icon, request.SortOrder, request.UsualClosingDay, request.UsualDueDay),
                    cancellationToken)).ToHttp())
            .Returns<AccountResponse>();
        accounts.MapPost("/{id:guid}/archive", async (Guid id, SetAccountArchivedHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(new SetAccountArchivedCommand(id, Archived: true), cancellationToken)).ToHttp())
            .Returns<AccountResponse>();
        accounts.MapPost("/{id:guid}/unarchive", async (Guid id, SetAccountArchivedHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(new SetAccountArchivedCommand(id, Archived: false), cancellationToken)).ToHttp())
            .Returns<AccountResponse>();
        accounts.MapPost("/{id:guid}/opening", async (Guid id, OpeningBalanceRequest request, SetOpeningBalanceHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(
                    new SetOpeningBalanceCommand(request.TransactionId, id, request.Date, request.Amount, request.Currency),
                    cancellationToken)).ToCreatedHttp())
            .ReturnsCreated<TransactionResponse>();

        var categories = api.MapGroup("/categories").WithTags("Categorías");
        categories.MapGet("/", async (ListCategoriesHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(cancellationToken)).ToHttp())
            .Returns<IReadOnlyList<CategoryResponse>>();
        categories.MapPost("/", async (CreateCategoryCommand command, CreateCategoryHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToCreatedHttp())
            .ReturnsCreated<CategoryResponse>();
        categories.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest request, UpdateCategoryHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(
                    new UpdateCategoryCommand(id, request.Name, request.Color, request.Icon, request.SortOrder),
                    cancellationToken)).ToHttp())
            .Returns<CategoryResponse>();

        // PUT con Id del front: crea la etiqueta o la actualiza (se puede crear al vuelo mientras se carga un gasto).
        var tags = api.MapGroup("/tags").WithTags("Etiquetas");
        tags.MapGet("/", async (ListTagsHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(cancellationToken)).ToHttp())
            .Returns<IReadOnlyList<TagResponse>>();
        tags.MapPut("/{id:guid}", async (Guid id, SaveTagRequest request, SaveTagHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(new SaveTagCommand(id, request.Name, request.Color), cancellationToken)).ToCreatedHttp())
            .ReturnsCreated<TagResponse>();

        var recurring = api.MapGroup("/recurring-templates").WithTags("Fijos");
        recurring.MapGet("/", async (ListRecurringTemplatesHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(cancellationToken)).ToHttp())
            .Returns<IReadOnlyList<RecurringTemplateResponse>>();
        recurring.MapPost("/", async (CreateRecurringTemplateCommand command, CreateRecurringTemplateHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToCreatedHttp())
            .ReturnsCreated<RecurringTemplateResponse>();
        recurring.MapPut("/{id:guid}", async (Guid id, UpdateRecurringTemplateRequest request, UpdateRecurringTemplateHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(
                    new UpdateRecurringTemplateCommand(id, request.Description, request.EstimatedAmount, request.DayOfMonth, request.EndsOn, request.IsActive),
                    cancellationToken)).ToHttp())
            .Returns<RecurringTemplateResponse>();
    }
}
