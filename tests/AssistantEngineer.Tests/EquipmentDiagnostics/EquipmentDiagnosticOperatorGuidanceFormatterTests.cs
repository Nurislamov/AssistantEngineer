using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Guidance;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticOperatorGuidanceFormatterTests
{
    [Fact]
    public async Task FormatsGreeGmvH5GuidanceMessageDeterministically()
    {
        var diagnosticCase = await GetDiagnosticCaseAsync("GMV", "H5");

        var first = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);
        var second = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);

        Assert.Equal(Flatten(first), Flatten(second));
        Assert.Equal("Gree GMV H5 diagnostic guidance", first.Title);
        Assert.Equal(diagnosticCase.ShortSummary, first.Summary);
        Assert.Equal(diagnosticCase.SourceSummary, first.SourceLine);
        Assert.Equal(diagnosticCase.RecommendedNextChecks, first.RecommendedChecks);
        Assert.Equal(diagnosticCase.SafetyBoundary, first.SafetyLine);
        Assert.Equal(diagnosticCase.OperatorNotes, first.OperatorNotes);
        Assert.Equal(diagnosticCase.OperatorNotes.Last(), first.Footer);
        Assert.Contains("Verification required", first.VerificationBanner, StringComparison.Ordinal);
        Assert.Contains("seed knowledge", first.VerificationBanner, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("preliminary", first.VerificationBanner, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Chiller", "E6", "Gree Chiller E6 diagnostic guidance")]
    [InlineData("Indoor", "H6", "Gree Indoor H6 diagnostic guidance")]
    public async Task FormatsChillerAndIndoorGuidanceMessages(
        string series,
        string code,
        string expectedTitle)
    {
        var diagnosticCase = await GetDiagnosticCaseAsync(series, code);

        var message = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);

        Assert.Equal(expectedTitle, message.Title);
        Assert.NotEmpty(message.RecommendedChecks);
        Assert.Contains("SeededEngineeringKnowledge", message.SourceLine, StringComparison.Ordinal);
        Assert.Contains("UnverifiedSeed", message.SourceLine, StringComparison.Ordinal);
        Assert.Contains("Verification required", message.VerificationBanner, StringComparison.Ordinal);
        Assert.Contains("qualified-technician", message.SafetyLine, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(message.OperatorNotes);
    }

    [Theory]
    [InlineData("GMV", "H5")]
    [InlineData("Chiller", "E6")]
    [InlineData("Indoor", "H6")]
    public async Task SeedGuidanceRequiresVerificationAndDoesNotClaimManualVerified(
        string series,
        string code)
    {
        var diagnosticCase = await GetDiagnosticCaseAsync(series, code);
        var message = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);

        Assert.Equal(DiagnosticConfidence.Low, diagnosticCase.Confidence);
        Assert.False(diagnosticCase.IsManualVerified);
        Assert.True(diagnosticCase.IsSeedKnowledge);
        Assert.True(diagnosticCase.VerificationRequired);
        Assert.Contains("Verification required", message.VerificationBanner, StringComparison.Ordinal);
        Assert.DoesNotContain("Manual verified", message.VerificationBanner, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FormatterDoesNotInventRecommendedChecks()
    {
        var diagnosticCase = await GetDiagnosticCaseAsync("GMV", "H5");

        var message = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);

        Assert.Equal(diagnosticCase.RecommendedNextChecks.Count, message.RecommendedChecks.Count);
        Assert.Equal(diagnosticCase.RecommendedNextChecks, message.RecommendedChecks);
    }

    [Fact]
    public async Task FormatterUsesDtoOperatorFieldsForBodySections()
    {
        var diagnosticCase = await GetDiagnosticCaseAsync("Indoor", "H6");

        var message = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);

        Assert.Equal(diagnosticCase.ShortSummary, message.Summary);
        Assert.Equal(diagnosticCase.SourceSummary, message.SourceLine);
        Assert.Equal(diagnosticCase.SafetyBoundary, message.SafetyLine);
        Assert.Equal(diagnosticCase.OperatorNotes, message.OperatorNotes);
        Assert.Equal(diagnosticCase.OperatorNotes.Last(), message.Footer);
    }

    [Fact]
    public async Task FormatterOutputDoesNotContainUnsafeWording()
    {
        var cases = new[]
        {
            await GetDiagnosticCaseAsync("GMV", "H5"),
            await GetDiagnosticCaseAsync("Chiller", "E6"),
            await GetDiagnosticCaseAsync("Indoor", "H6")
        };
        var forbiddenFragments = new[]
        {
            "bypass",
            "disable protection",
            "disable protections",
            "force run",
            "short protection",
            "ignore protection"
        };

        var violations = cases
            .Select(EquipmentDiagnosticOperatorGuidanceFormatter.Format)
            .SelectMany(message => Flatten(message)
                .SelectMany(text => forbiddenFragments
                    .Where(fragment => text.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    .Select(fragment => $"{message.Title}:{fragment}")))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Formatter output contains unsafe diagnostic wording fragments: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void FormatterRejectsNullDiagnosticCase()
    {
        Assert.Throws<ArgumentNullException>(() =>
            EquipmentDiagnosticOperatorGuidanceFormatter.Format(null!));
    }

    private static async Task<EquipmentDiagnosticCaseDto> GetDiagnosticCaseAsync(
        string series,
        string code)
    {
        using var serviceProvider = CreateServiceProvider();
        var service = serviceProvider.GetRequiredService<IEquipmentDiagnosticsService>();

        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            manufacturer: "Gree",
            errorCode: code,
            series: series,
            modelCode: null,
            CancellationToken.None);

        return Assert.IsType<EquipmentDiagnosticCaseDto>(diagnosticCase);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private static IEnumerable<string> Flatten(
        EquipmentDiagnosticOperatorGuidanceMessage message)
    {
        yield return message.Title;
        yield return message.Summary;
        yield return message.VerificationBanner;
        yield return message.SourceLine;
        foreach (var recommendedCheck in message.RecommendedChecks)
        {
            yield return recommendedCheck;
        }

        yield return message.SafetyLine;
        foreach (var operatorNote in message.OperatorNotes)
        {
            yield return operatorNote;
        }

        yield return message.Footer;
    }
}
