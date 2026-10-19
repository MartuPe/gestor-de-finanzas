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

    [Fact]
    public void Infrastructure_no_depende_de_la_Api() =>
        AssertNoDependencies(Infrastructure, Api, AspNetCore);

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
