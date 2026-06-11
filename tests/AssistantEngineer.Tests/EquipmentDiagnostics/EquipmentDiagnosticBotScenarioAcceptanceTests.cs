using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticBotScenarioAcceptanceTests
{
    [Fact]
    public void ScenarioPackIsValidUniqueAndSafe()
    {
        var scenarios = BotScenarioTestCatalog.LoadAndValidate();

        Assert.Contains(scenarios, item => item.ScenarioId == "gree-h5-answer");
        Assert.Contains(scenarios, item => item.ScenarioId == "gree-c5-clarification");
        Assert.Contains(scenarios, item => item.ScenarioId == "gree-f5-answer-or-not-found");
        Assert.All(scenarios, scenario =>
        {
            Assert.True(scenario.Expected.MustNotUseUnsafeWording);
            Assert.True(scenario.Expected.MustNotExposeInternalArtifacts);
            Assert.True(scenario.Expected.MustNotExposeStagingOrCodebook);
        });
    }

    [Fact]
    public async Task EveryScenarioMatchesCurrentRuntimeBehaviorAndIsDeterministic()
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var diagnostics = provider.GetRequiredService<IEquipmentDiagnosticsService>();
        var before = await diagnostics.GetCatalogIndexAsync(CancellationToken.None);

        foreach (var scenario in BotScenarioTestCatalog.LoadAndValidate())
        {
            var request = BotScenarioTestCatalog.ToRequest(scenario);
            var first = await facade.DiagnoseAsync(request, CancellationToken.None);
            var second = await facade.DiagnoseAsync(request, CancellationToken.None);
            var json = JsonSerializer.Serialize(first);

            Assert.Equal(scenario.Expected.ResponseStatus, first.Status);
            Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));

            if (scenario.Expected.VerificationRequired is not null)
                Assert.Equal(scenario.Expected.VerificationRequired, first.VerificationRequired);
            if (scenario.Expected.RequiresSafetyBoundary)
                Assert.False(string.IsNullOrWhiteSpace(first.SafetyCard.Boundary));
            if (scenario.Expected.RequiresSourceOrProvenance)
                Assert.NotNull(first.SourceCard);
            if (scenario.Expected.ClarificationOptionsMinimum is not null)
                Assert.True(first.ClarificationQuestion?.Options.Count >= scenario.Expected.ClarificationOptionsMinimum);

            AssertUiExpectation(scenario.Expected.UiExpectation, first);
            Assert.All(scenario.Expected.MustContainText, fragment =>
                Assert.Contains(fragment, json, StringComparison.OrdinalIgnoreCase));
            Assert.All(scenario.Expected.MustNotContainText, fragment =>
                Assert.DoesNotContain(fragment, json, StringComparison.OrdinalIgnoreCase));
            Assert.All(BotScenarioTestCatalog.UnsafeFragments, fragment =>
                Assert.DoesNotContain(fragment, json, StringComparison.OrdinalIgnoreCase));
            Assert.All(BotScenarioTestCatalog.InternalArtifactFragments, fragment =>
                Assert.DoesNotContain(fragment, json, StringComparison.OrdinalIgnoreCase));
        }

        var after = await diagnostics.GetCatalogIndexAsync(CancellationToken.None);
        Assert.Equal(before.TotalEntries, after.TotalEntries);
        Assert.DoesNotContain(after.Codes, code => code.Confidence == DiagnosticConfidence.ManualVerified);
    }

    [Fact]
    public void ScenarioPackIsNotEmbeddedRuntimeKnowledge()
    {
        var resources = EquipmentDiagnosticsJsonKnowledgeSource.GetEmbeddedKnowledgeResourceNames();

        Assert.DoesNotContain(resources, resource =>
            resource.Contains("bot-scenarios", StringComparison.OrdinalIgnoreCase) ||
            resource.Contains(".scenario.", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertUiExpectation(
        BotScenarioUiExpectation expected,
        EquipmentDiagnosticBotResponse response)
    {
        Assert.Equal(expected.ShowAnswerCard, response.AnswerCard is not null);
        Assert.Equal(expected.ShowClarificationOptions, response.ClarificationQuestion?.Options.Count > 0);
        Assert.Equal(expected.ShowReferenceOnlyNotice, response.Status == EquipmentDiagnosticBotResponseStatus.ReferenceOnly);
        Assert.Equal(expected.ShowNotFoundFallback, response.Status == EquipmentDiagnosticBotResponseStatus.NotFound);
        Assert.Equal(expected.ShowVerificationBanner, response.VerificationRequired);
        Assert.Equal(expected.ShowSafetyCard, !string.IsNullOrWhiteSpace(response.SafetyCard.Boundary));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }
}
