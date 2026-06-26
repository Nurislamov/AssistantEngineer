using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticBotServiceTests
{
    [Fact]
    public async Task ExactRuntimeMatchReturnsAnswerWithSeedVerificationWarning()
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var response = await service.DiagnoseAsync(
            new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV"),
            CancellationToken.None);

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, response.Status);
        Assert.NotNull(response.AnswerCard);
        Assert.Null(response.ClarificationQuestion);
        Assert.True(response.VerificationRequired);
        Assert.True(response.IsSeedKnowledge);
        Assert.False(response.IsManualVerified);
        Assert.Equal(DiagnosticConfidence.Low, response.Confidence);
        Assert.Contains(response.Warnings, warning => warning.Contains("Seed knowledge", StringComparison.Ordinal));
        Assert.Equal("GREE", response.NormalizedManufacturer);
        Assert.Equal("H5", response.NormalizedCode);
    }

    [Fact]
    public async Task SharedRuntimeCodeReturnsDeterministicClarificationOptions()
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var first = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "E1"));
        var second = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "E1"));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ClarificationRequired, first.Status);
        Assert.Null(first.AnswerCard);
        var question = Assert.IsType<EquipmentDiagnosticBotClarificationQuestion>(first.ClarificationQuestion);
        Assert.True(question.Options.Count >= 3);
        Assert.Equal(
            JsonSerializer.Serialize(question.Options),
            JsonSerializer.Serialize(second.ClarificationQuestion!.Options));
        Assert.Contains(question.Options, option => option.EquipmentSide == EquipmentDiagnosticBotEquipmentSide.Outdoor);
        Assert.Contains(question.Options, option => option.EquipmentSide == EquipmentDiagnosticBotEquipmentSide.Indoor);
        Assert.Contains(question.Options, option => option.EquipmentSide == EquipmentDiagnosticBotEquipmentSide.Chiller);
    }

    [Fact]
    public async Task GreeC5WithoutContextRequiresClarificationWhenRuntimeServiceHasMultipleContexts()
    {
        var summaries = new[]
        {
            Summary("C5", "Indoor", EquipmentCategory.VrfIndoorUnit),
            Summary("C5", "GMV", EquipmentCategory.VrfOutdoorUnit)
        };
        var service = new EquipmentDiagnosticBotService(
            new FakeDiagnosticsService(summaries),
            new JsonErrorKnowledgeLocalizationSource());

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "C5"));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ClarificationRequired, response.Status);
        Assert.Equal(2, response.ClarificationQuestion!.Options.Count);
        Assert.Null(response.AnswerCard);
    }

    [Fact]
    public async Task ManualVerifiedRuntimeFixtureReturnsVerifiedAnswer()
    {
        var realCase = await GetRuntimeCaseAsync("GMV", "H5");
        var verifiedCase = realCase with
        {
            Confidence = DiagnosticConfidence.ManualVerified,
            IsManualVerified = true,
            IsSeedKnowledge = false,
            VerificationRequired = false,
            ConfidenceExplanation = "Verified against an exact manual page.",
            Source = realCase.Source with
            {
                SourceType = "ServiceManual",
                EvidenceLevel = "ManualPageVerified",
                ManualTitle = "Synthetic focused-test manual",
                Page = "PDF 10",
                Section = "Troubleshooting H5"
            }
        };
        var service = new EquipmentDiagnosticBotService(
            new FakeDiagnosticsService([Summary("H5", "GMV", EquipmentCategory.VrfOutdoorUnit)], verifiedCase),
            new JsonErrorKnowledgeLocalizationSource());

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV"));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, response.Status);
        Assert.True(response.IsManualVerified);
        Assert.False(response.IsSeedKnowledge);
        Assert.False(response.VerificationRequired);
        Assert.Equal("ManualPageVerified", response.SourceCard!.EvidenceLevel);
    }

    [Fact]
    public async Task UnknownCodeReturnsSafeNotFoundWithoutNonRuntimeDiagnosis()
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "ZZ99"));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.NotFound, response.Status);
        Assert.Null(response.AnswerCard);
        Assert.Null(response.SourceCard);
        Assert.Contains("No runtime diagnostic case found", response.Message, StringComparison.Ordinal);
        Assert.Contains(response.Warnings, warning => warning.Contains("not used as a final diagnosis", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("A0")]
    [InlineData("n6")]
    [InlineData("qA")]
    [InlineData("db")]
    [InlineData("C00")]
    [InlineData("P10")]
    public async Task ReferenceOnlyPatternsAreNotPresentedAsFaultDiagnosis(string code)
    {
        var service = new EquipmentDiagnosticBotService(
            new FakeDiagnosticsService([]),
            new JsonErrorKnowledgeLocalizationSource());

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", code));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, response.Status);
        Assert.Null(response.AnswerCard);
        Assert.Contains("not a confirmed runtime diagnostic case", response.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("CE41")]
    [InlineData("CE42")]
    [InlineData("CE52")]
    public async Task ControllerModelNamesAreNotParsedAsFaultCodes(string code)
    {
        var service = new EquipmentDiagnosticBotService(
            new FakeDiagnosticsService([]),
            new JsonErrorKnowledgeLocalizationSource());

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", code));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Unsupported, response.Status);
        Assert.Null(response.AnswerCard);
        Assert.Contains("model name", response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BotFacadeIsRegisteredAndUsesRuntimeCatalogOnly()
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var diagnostics = provider.GetRequiredService<IEquipmentDiagnosticsService>();
        var before = await diagnostics.GetCatalogIndexAsync();

        var response = await facade.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV"));
        var after = await diagnostics.GetCatalogIndexAsync();

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, response.Status);
        Assert.Equal(before.TotalEntries, after.TotalEntries);
        Assert.DoesNotContain(after.Codes, code => code.Confidence == DiagnosticConfidence.ManualVerified);
        var constructor = Assert.Single(typeof(EquipmentDiagnosticBotService).GetConstructors());
        Assert.Equal(
            [typeof(IEquipmentDiagnosticsService), typeof(IErrorKnowledgeLocalizationSource)],
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("GMV6", null)]
    [InlineData(null, "Gree debugging U0")]
    public async Task Gmv6U0FallsBackToManualBackedDebuggingKnowledge(
        string? series,
        string? freeText)
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var response = await service.DiagnoseAsync(
            new EquipmentDiagnosticBotRequest("Gree", "U0", FreeText: freeText, Series: series));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, response.Status);
        Assert.Equal("GMV6", response.EquipmentContext!.Series);
        Assert.True(response.IsManualVerified);
        Assert.False(response.VerificationRequired);
        Assert.Equal(DiagnosticConfidence.High, response.Confidence);
        Assert.Equal("Manual", response.SourceCard!.SourceType);
        Assert.Contains("LocalizedKnowledgeMatch", response.InternalDecisionTrace!);
    }

    [Fact]
    public async Task C0UsesExplicitMeaningGroupWhenSeriesIsNotSpecified()
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "C0"));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, response.Status);
        Assert.Equal("GMV6", response.EquipmentContext!.Series);
        Assert.Contains("Gree GMV6", response.ApplicableContexts);
        Assert.Contains("Gree GMV Mini", response.ApplicableContexts);
        Assert.Contains(response.InternalDecisionTrace!, value => value.StartsWith("LocalizedKnowledgeMeaningGroup:", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Ho")]
    [InlineData("HO")]
    public async Task HoVisualAliasResolvesToExistingGmv6H0(string code)
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", code));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, response.Status);
        Assert.Equal("H0", response.NormalizedCode);
        Assert.Equal(code, response.ObservedCode.Code);
        Assert.Contains("VisualCodeAlias:HO->H0", response.InternalDecisionTrace!);
    }

    [Fact]
    public async Task GmvMiniC0SeriesHintSelectsGmvMiniEntry()
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "C0", Series: "GMV Mini"));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, response.Status);
        Assert.Equal("GMV Mini", response.EquipmentContext!.Series);
        Assert.Empty(response.ApplicableContexts);
    }

    [Fact]
    public async Task BotResponsesDoNotContainUnsafeWordingOrRawGeneratedKnowledge()
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();
        var responses = new[]
        {
            await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV")),
            await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "E1")),
            await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "ZZ99"))
        };
        var forbidden = new[]
        {
            "bypass", "disable protection", "force run", "short protection", "ignore protection",
            "DraftPreview", "artifacts/verification", "Knowledge/manual-codebook", "Knowledge/staging",
            "staging-candidate-preview", "D:\\", "C:\\", "/src/"
        };

        foreach (var response in responses)
        {
            var json = JsonSerializer.Serialize(response);
            Assert.All(forbidden, fragment => Assert.DoesNotContain(fragment, json, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void BotServiceSourceDoesNotLoadStagingCodebookOrPreviewArtifacts()
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Application", "Bot", "EquipmentDiagnosticBotService.cs");
        var source = File.ReadAllText(path);

        Assert.DoesNotContain("manual-codebook", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("staging-candidate", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("staging-candidate-preview", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("File.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Directory.", source, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private static async Task<EquipmentDiagnosticCaseDto> GetRuntimeCaseAsync(string series, string code)
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticsService>();
        return Assert.IsType<EquipmentDiagnosticCaseDto>(
            await service.GetDiagnosticCaseAsync("Gree", code, series, null, CancellationToken.None));
    }

    private static EquipmentErrorCodeSummaryDto Summary(string code, string series, EquipmentCategory category) =>
        new("Gree", series, null, code, $"{code} title", $"{code} meaning", "Service review", category, DiagnosticConfidence.Low, null);

    private sealed class FakeDiagnosticsService(
        IReadOnlyList<EquipmentErrorCodeSummaryDto> summaries,
        EquipmentDiagnosticCaseDto? diagnosticCase = null) : IEquipmentDiagnosticsService
    {
        public Task<IReadOnlyList<EquipmentErrorCodeSummaryDto>> SearchErrorCodesAsync(
            SearchEquipmentErrorCodesQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(summaries);

        public Task<EquipmentDiagnosticCaseDto?> GetDiagnosticCaseAsync(
            string manufacturer,
            string errorCode,
            string? series,
            string? modelCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(diagnosticCase);

        public Task<EquipmentDiagnosticsCatalogIndexDto> GetCatalogIndexAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
