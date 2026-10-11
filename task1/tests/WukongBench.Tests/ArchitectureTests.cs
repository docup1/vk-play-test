namespace WukongBench.Tests;

using System.Reflection;
using Xunit;

/// <summary>
/// Guards the layering rules of the solution. These tests fail the build if
/// somebody adds a reference in the wrong direction:
///
/// * Domain has no dependency at all (not even a package);
/// * Application depends only on Domain;
/// * Infrastructure depends only on Domain;
/// * Application and Infrastructure never reference each other;
/// * Presentation is the only project that knows all three (composition root).
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(Domain.Models.BenchmarkReport).Assembly;
    private static readonly Assembly Application = typeof(Application.UseCases.RunBenchmarkSuiteUseCase).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.Steam.SteamInstallationLocator).Assembly;
    private static readonly Assembly Presentation = typeof(Presentation.Rendering.MarkdownReportRenderer).Assembly;

    private static IReadOnlyList<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("WukongBench", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    [Fact]
    public void Domain_DependsOnNothing()
    {
        Assert.Empty(References(Domain));
        Assert.DoesNotContain(
            Domain.GetReferencedAssemblies(),
            reference => (reference.Name ?? string.Empty).StartsWith("Microsoft.", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_DependsOnlyOnDomain()
    {
        Assert.Equal(new[] { "WukongBench.Domain" }, References(Application));
    }

    [Fact]
    public void Infrastructure_DependsOnlyOnDomain()
    {
        Assert.Equal(new[] { "WukongBench.Domain" }, References(Infrastructure));
    }

    [Fact]
    public void ApplicationAndInfrastructure_DoNotKnowEachOther()
    {
        Assert.DoesNotContain("WukongBench.Infrastructure", References(Application));
        Assert.DoesNotContain("WukongBench.Application", References(Infrastructure));
    }

    [Fact]
    public void Presentation_ComposesAllLayers()
    {
        var references = References(Presentation);

        Assert.Contains("WukongBench.Domain", references);
        Assert.Contains("WukongBench.Application", references);
        Assert.Contains("WukongBench.Infrastructure", references);
    }

    [Fact]
    public void Domain_ModelsAreDataOnly()
    {
        var models = Domain.GetTypes()
            .Where(type => type.IsPublic && type.IsClass && !type.IsAbstract)
            .ToArray();

        Assert.NotEmpty(models);

        foreach (var model in models)
        {
            // Property accessors are special-name members and are allowed;
            // anything else would be behaviour in the Domain layer.
            var behaviour = model
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Select(method => method.Name)
                .ToArray();

            Assert.True(
                behaviour.Length == 0,
                $"{model.FullName} declares behaviour, which the Domain layer must not contain: {string.Join(", ", behaviour)}");
        }
    }
}
