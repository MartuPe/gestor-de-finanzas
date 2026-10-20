using Finanzas.Api.Auth;
using Finanzas.Api.Endpoints;
using Finanzas.Api.Http;
using Finanzas.Application;
using Finanzas.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddAppHttp();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// La documentación de la API (/openapi/v1.json y /scalar) no se publica en producción.
if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");

var api = app.MapGroup("/api");
api.MapAuthEndpoints();

var secured = api.MapGroup(string.Empty).RequireAuthorization(AuthSetup.LedgerPolicy);
secured.MapCatalogEndpoints();
secured.MapMovementEndpoints();
secured.MapReportEndpoints();

app.Run();

/// <summary>Visible para los tests de integración (<c>WebApplicationFactory&lt;Program&gt;</c>).</summary>
public partial class Program;
