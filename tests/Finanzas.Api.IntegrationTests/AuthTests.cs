using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Finanzas.Api.IntegrationTests;

public class AuthTests(SqlServerFixture sql)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sin_sesion_la_api_responde_401_y_no_redirige()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);

        var response = await api.Client.GetAsync("/api/accounts", _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task La_configuracion_inicial_crea_el_usuario_y_el_libro_y_deja_la_sesion_iniciada()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        Assert.True((await api.GetJsonAsync("/api/auth/setup", _ct)).GetProperty("needed").GetBoolean());

        await api.SetupAsync(_ct);

        var me = await api.GetJsonAsync("/api/auth/me", _ct);
        Assert.Equal(TestApi.Email, me.GetProperty("email").GetString());
        Assert.NotEqual(JsonValueKind.Null, me.GetProperty("ledgerId").ValueKind);
        Assert.Equal(21, (await api.GetJsonAsync("/api/categories", _ct)).GetArrayLength());
        Assert.False((await api.GetJsonAsync("/api/auth/setup", _ct)).GetProperty("needed").GetBoolean());
    }

    [Fact]
    public async Task Una_contrasenia_corta_se_rechaza_con_el_motivo_en_castellano()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);

        var response = await api.Client.PostAsJsonAsync(
            "/api/auth/setup", new { email = TestApi.Email, password = "corta", displayName = "Martu" }, _ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var motivo = Assert.Single(problem.GetProperty("errors").GetProperty("password").EnumerateArray());
        Assert.Equal("La contraseña tiene que tener al menos 12 caracteres.", motivo.GetString());
        Assert.True((await api.GetJsonAsync("/api/auth/setup", _ct)).GetProperty("needed").GetBoolean());
    }

    [Fact]
    public async Task Despues_del_primer_usuario_el_registro_queda_cerrado()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        await api.SetupAsync(_ct);

        var response = await api.Client.PostAsJsonAsync(
            "/api/auth/setup", new { email = "otra@test.local", password = TestApi.Password, displayName = "Otra" }, _ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("auth.already_set_up", await CodeAsync(response));
    }

    [Fact]
    public async Task Se_entra_con_la_contrasenia_y_se_sale()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        await api.SetupAsync(_ct);

        Assert.Equal(HttpStatusCode.NoContent, (await api.Client.PostAsync("/api/auth/logout", null, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/auth/me", _ct)).StatusCode);

        var mal = await api.Client.PostAsJsonAsync("/api/auth/login", new { email = TestApi.Email, password = "otra-clave-cualquiera" }, _ct);
        Assert.Equal(HttpStatusCode.Unauthorized, mal.StatusCode);
        Assert.Equal("auth.invalid_credentials", await CodeAsync(mal));

        var bien = await api.Client.PostAsJsonAsync("/api/auth/login", new { email = TestApi.Email, password = TestApi.Password }, _ct);
        Assert.Equal(HttpStatusCode.NoContent, bien.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/api/auth/me", _ct)).StatusCode);
    }

    [Fact]
    public async Task Las_opciones_para_registrar_una_passkey_traen_el_desafio_y_el_usuario()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        await api.SetupAsync(_ct);

        var options = await api.PostJsonAsync("/api/auth/passkeys/creation-options", new { }, _ct);

        Assert.False(string.IsNullOrEmpty(options.GetProperty("challenge").GetString()));
        Assert.Equal(TestApi.Email, options.GetProperty("user").GetProperty("name").GetString());
        Assert.Equal("Martu", options.GetProperty("user").GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Una_passkey_invalida_no_inicia_sesion()
    {
        await using var api = await TestApi.StartAsync(sql, _ct);
        await api.SetupAsync(_ct);
        await api.Client.PostAsync("/api/auth/logout", null, _ct);
        await api.PostJsonAsync("/api/auth/passkeys/request-options", new { }, _ct);

        var response = await api.Client.PostAsJsonAsync("/api/auth/passkeys/login", new { credentialJson = """{"id":"no-existe"}""" }, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/auth/me", _ct)).StatusCode);
    }

    private async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("code").GetString();
}
