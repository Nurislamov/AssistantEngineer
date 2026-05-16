using System.Reflection;
using AssistantEngineer.Modules.Identity;
using NetArchTest.Rules;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P5IdentityArchitectureTests
{
    private static readonly Assembly IdentityAssembly = typeof(DependencyInjection).Assembly;

    [Fact]
    public void IdentityModuleProjectExists()
    {
        Assert.True(File.Exists(IdentityProjectPath), $"Identity module project is missing: {IdentityProjectPath}");
    }

    [Fact]
    public void IdentityDomain_DoesNotDependOnApiOrInfrastructure()
    {
        var domainNamespaces = new[]
        {
            "AssistantEngineer.Modules.Identity.Domain.Entities",
            "AssistantEngineer.Modules.Identity.Domain.Enums",
            "AssistantEngineer.Modules.Identity.Domain.ValueObjects"
        };

        foreach (var domainNamespace in domainNamespaces)
        {
            var domainToApi = Types.InAssembly(IdentityAssembly)
                .That()
                .ResideInNamespace(domainNamespace)
                .Should()
                .NotHaveDependencyOn("AssistantEngineer.Api")
                .GetResult();

            var domainToInfrastructure = Types.InAssembly(IdentityAssembly)
                .That()
                .ResideInNamespace(domainNamespace)
                .Should()
                .NotHaveDependencyOn("AssistantEngineer.Infrastructure")
                .GetResult();

            Assert.True(domainToApi.IsSuccessful, $"Identity domain namespace {domainNamespace} has forbidden API dependencies: {FormatFailingTypes(domainToApi)}");
            Assert.True(domainToInfrastructure.IsSuccessful, $"Identity domain namespace {domainNamespace} has forbidden infrastructure dependencies: {FormatFailingTypes(domainToInfrastructure)}");
        }
    }

    [Fact]
    public void IdentityApplicationAbstractions_DoNotDependOnApi()
    {
        var abstractions = Types.InAssembly(IdentityAssembly)
            .That()
            .ResideInNamespace("AssistantEngineer.Modules.Identity.Application.Abstractions")
            .Should()
            .NotHaveDependencyOn("AssistantEngineer.Api")
            .GetResult();

        Assert.True(abstractions.IsSuccessful, $"Identity abstractions depend on API: {FormatFailingTypes(abstractions)}");
    }

    [Fact]
    public void NoNewAuthControllersOrRoutesWereIntroducedInApi()
    {
        var controllerRoot = Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Controllers");
        var controllerFiles = Directory.GetFiles(controllerRoot, "*Controller.cs", SearchOption.AllDirectories);

        var suspiciousRouteTokens = new[] { "auth", "login", "signin", "oauth", "oidc", "jwt" };
        var violations = new List<string>();

        foreach (var file in controllerFiles)
        {
            var text = File.ReadAllText(file);
            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!line.StartsWith("[Route(\"", StringComparison.Ordinal))
                {
                    continue;
                }

                if (suspiciousRouteTokens.Any(token => line.Contains(token, StringComparison.OrdinalIgnoreCase)))
                {
                    violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)}:{i + 1}:{line}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Unexpected auth-like controller routes were found:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void SecurityInventoryReferencesP5IdentityStep()
    {
        var content = File.ReadAllText(SecurityInventoryPath);
        Assert.Contains("P5-01", content, StringComparison.Ordinal);
        Assert.Contains("Identity domain skeleton", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecurityBoundaryPolicyRetainsNonClaims()
    {
        var content = File.ReadAllText(SecurityBoundaryPolicyPath);

        Assert.Contains("No production security certification claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No SOC 2 / ISO 27001 compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No full multi-tenant isolation claim yet", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No external identity provider integration claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No certified/certification claim", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatFailingTypes(TestResult result) =>
        result.FailingTypeNames is null
            ? "no failing type details"
            : string.Join(", ", result.FailingTypeNames);

    private static string IdentityProjectPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.Identity",
            "AssistantEngineer.Modules.Identity.csproj");

    private static string SecurityInventoryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "production-saas-readiness-inventory.md");

    private static string SecurityBoundaryPolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "security-boundary-policy.md");
}
