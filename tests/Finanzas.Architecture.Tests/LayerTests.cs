using System.Reflection;
using NetArchTest.Rules;

namespace Finanzas.Architecture.Tests;

/// <summary>Las referencias permitidas entre capas (02-backend §2).</summary>
public class LayerTests
{
    private const string Application = "Finanzas.Application";
    private const string Infrastructure = "Finanzas.Infrastructure";
    private const string Api = "Finanzas.Api";
    private const string EntityFramework = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";

    [Fact]
    public void El_dominio_no_depende_de_otras_capas_ni_de_frameworks() =>
        AssertNoDependencies("Finanzas.Domain", Application, Infrastructure, Api, EntityFramework, AspNetCore);

    [Fact]
    public void Application_no_depende_de_Infrastructure_ni_de_la_Api() =>
        AssertNoDependencies(Application, Infrastructure, Api, EntityFramework, AspNetCore);

    /// <summary>
    /// Infrastructure puede usar los stores de Identity y de Data Protection (son persistencia),
    /// pero nada de la capa web: eso es de la Api.
    /// </summary>
    [Fact]
    public void Infrastructure_no_depende_de_la_Api_ni_de_la_capa_web() =>
        AssertNoDependencies(
            Infrastructure,
            Api,
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.AspNetCore.Builder",
            "Microsoft.AspNetCore.Routing",
            "Microsoft.AspNetCore.Authentication");

    private static void AssertNoDependencies(string assemblyName, params string[] forbidden)
    {
        var result = Types.InAssembly(Assembly.Load(assemblyName))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        var failing = string.Join(", ", result.FailingTypeNames ?? Enumerable.Empty<string>());
        Assert.True(result.IsSuccessful, $"{assemblyName} depende de {string.Join(" / ", forbidden)} en: {failing}");
    }
}
