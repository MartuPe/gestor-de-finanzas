using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Finanzas.Api.IntegrationTests;

/// <summary>La app usada como la va a usar la PWA: por HTTP, con la cookie de sesión y el JSON tal cual viaja.</summary>
public class ApiFlowTests(SqlServerFixture sql)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Una_compra_en_cuotas_con_la_Visa_aparece_en_el_mes_en_que_vence()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        await api.SetupAsync(_ct);
        var visa = await api.PostJsonAsync(
            "/api/accounts/credit",
            new
            {
                id = Guid.NewGuid(), name = "Visa", defaultCurrency = "ARS", lineType = "card", issuer = "Supervielle",
                network = "visa", usualClosingDay = 28, usualDueDay = 5,
            },
            _ct);
        var regalos = await CategoryIdAsync(api, "Regalos");
        var compra = new
        {
            id = Guid.NewGuid(), accountId = visa.GetProperty("id").GetGuid(), date = "2025-09-10", amount = 42_000m, currency = "ARS",
            categoryId = regalos, description = "Perfume Lu", installments = 3,
        };

        var creada = await api.Client.PostAsJsonAsync("/api/expenses", compra, _ct);
        var reintento = await api.Client.PostAsJsonAsync("/api/expenses", compra, _ct);
        var octubre = await api.GetJsonAsync("/api/months/2025-10", _ct);

        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reintento.StatusCode);
        var gasto = await creada.Content.ReadFromJsonAsync<JsonElement>(_ct);
        Assert.Equal("expense", gasto.GetProperty("kind").GetString());
        Assert.Equal("2025-10", gasto.GetProperty("installments")[0].GetProperty("statementPeriod").GetString());

        Assert.Equal("2025-10", octubre.GetProperty("period").GetString());
        var resumen = Assert.Single(octubre.GetProperty("statementsDue").EnumerateArray());
        Assert.Equal("Visa", resumen.GetProperty("creditAccountName").GetString());
        Assert.Equal(14_000m, resumen.GetProperty("outstanding").GetProperty("ARS").GetDecimal());
    }

    [Fact]
    public async Task Las_cuentas_muestran_su_saldo_y_los_movimientos_se_listan()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        await api.SetupAsync(_ct);
        var banco = (await api.PostJsonAsync(
            "/api/accounts", new { id = Guid.NewGuid(), name = "Banco Supervielle", kind = "bank", defaultCurrency = "ARS" }, _ct))
            .GetProperty("id").GetGuid();
        await api.PostJsonAsync(
            $"/api/accounts/{banco}/opening",
            new { transactionId = Guid.NewGuid(), date = "2026-09-30", amount = 1_800_000m, currency = "ARS" },
            _ct);
        await api.PostJsonAsync(
            "/api/expenses",
            new
            {
                id = Guid.NewGuid(), accountId = banco, date = "2026-10-10", amount = 406_000m, currency = "ARS",
                categoryId = await CategoryIdAsync(api, "Otro"),
            },
            _ct);

        var cuentas = await api.GetJsonAsync("/api/accounts", _ct);
        var movimientos = await api.GetJsonAsync("/api/transactions?from=2026-09-01&to=2026-10-31", _ct);

        Assert.Equal(1_394_000m, cuentas[0].GetProperty("balance").GetProperty("ARS").GetDecimal());
        Assert.Equal(2, movimientos.GetProperty("total").GetInt32());
        Assert.Equal("expense", movimientos.GetProperty("items")[0].GetProperty("kind").GetString());
        Assert.Equal("opening", movimientos.GetProperty("items")[1].GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Los_datos_invalidos_vuelven_como_problem_details_con_code_y_campos()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        await api.SetupAsync(_ct);

        var response = await api.Client.PostAsJsonAsync(
            "/api/expenses",
            new { id = Guid.NewGuid(), accountId = Guid.NewGuid(), date = "2026-10-10", amount = 0m, currency = "ars", categoryId = Guid.NewGuid() },
            _ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", problem.GetProperty("code").GetString());
        var campos = problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).Order(StringComparer.Ordinal);
        Assert.Equal(new[] { "amount", "currency" }, campos);
    }

    [Fact]
    public async Task Un_periodo_o_un_filtro_con_formato_invalido_es_un_400()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        await api.SetupAsync(_ct);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync("/api/months/2026-13", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync("/api/reports/spending?period=2026-10&mode=cualquiera", _ct)).StatusCode);
    }

    [Fact]
    public async Task El_documento_OpenAPI_describe_las_respuestas_para_que_la_PWA_genere_sus_tipos()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);

        var document = await api.GetJsonAsync("/openapi/v1.json", _ct);

        var month = document.GetProperty("paths").GetProperty("/api/months/{period}").GetProperty("get")
            .GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.Equal("#/components/schemas/MonthSummaryResponse", month.GetProperty("$ref").GetString());

        var schemas = document.GetProperty("components").GetProperty("schemas");
        var period = schemas.GetProperty("MonthSummaryResponse").GetProperty("properties").GetProperty("period");
        Assert.Equal("string", period.GetProperty("type").GetString());
        Assert.Equal(@"^\d{4}-\d{2}$", period.GetProperty("pattern").GetString());
        Assert.False(schemas.TryGetProperty("Period", out _));

        var amount = schemas.GetProperty("RegisterExpenseCommand").GetProperty("properties").GetProperty("amount");
        Assert.Equal("number", amount.GetProperty("type").GetString());
    }

    private async Task<Guid> CategoryIdAsync(TestApi api, string name) =>
        (await api.GetJsonAsync("/api/categories", _ct)).EnumerateArray()
            .Single(category => category.GetProperty("name").GetString() == name)
            .GetProperty("id").GetGuid();
}
