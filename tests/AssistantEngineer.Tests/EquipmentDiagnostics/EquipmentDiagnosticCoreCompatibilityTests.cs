using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticCoreCompatibilityTests
{
    public static TheoryData<EquipmentDiagnosticBotRequest> CompatibilityRequests() =>
        new()
        {
            new("Gree", "H5", Series: "GMV6"),
            new("Gree", "E9", FreeText: "Gree U-Match GUD71PH1/B-S E9", Series: "U-Match R32"),
            new("Gree", "C0", Series: "GMV Mini"),
            new("Gree", "C0", Series: "GMV9 Flex"),
            new("Gree", "C0"),
            new("Gree", "n2"),
            new("Gree", "U0", Series: "GMV6"),
            new("Gree", "HO", Series: "GMV6"),
            new("Gree", "H0", Series: "GMV6"),
            new("Gree", "o1", Series: "GMV X"),
            new("Gree", "01"),
            new("Gree", "D1"),
            new("Gree", "d1"),
            new("Gree", "ZZ99")
        };

    [Theory]
    [MemberData(nameof(CompatibilityRequests))]
    public async Task NeutralCoreRoundTripsExistingBotContractExactly(
        EquipmentDiagnosticBotRequest request)
    {
        using var provider = CreateProvider();
        var core = provider.GetRequiredService<IEquipmentDiagnosticCore>();
        var bot = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var botResponse = await bot.DiagnoseAsync(request);
        var coreResult = await core.DiagnoseAsync(
            EquipmentDiagnosticBotCompatibilityMapper.ToCoreRequest(request));
        var roundTripped = EquipmentDiagnosticBotCompatibilityMapper.ToBotResponse(coreResult);

        Assert.Equal(
            JsonSerializer.Serialize(botResponse),
            JsonSerializer.Serialize(roundTripped));
        Assert.Equal(botResponse.Status.ToString(), coreResult.Status.ToString());
        Assert.Equal(botResponse.ObservedCode.Code, coreResult.ObservedCode.ObservedCode);
        Assert.Equal(botResponse.ObservedCode.NormalizedCode, coreResult.CanonicalCode);
        Assert.Equal(
            botResponse.ClarificationQuestion?.Options.Select(option => option.Label),
            coreResult.Ambiguity?.Candidates.Select(candidate => candidate.Label));
    }

    [Theory]
    [InlineData("U-Match R32", "E9", DiagnosticCoreStatus.Answer)]
    [InlineData("GMV Mini", "C0", DiagnosticCoreStatus.Answer)]
    [InlineData("GMV9 Flex", "C0", DiagnosticCoreStatus.ReferenceOnly)]
    public async Task NeutralCorePreservesMatchedSeriesIdentity(
        string series,
        string code,
        DiagnosticCoreStatus expectedStatus)
    {
        using var provider = CreateProvider();
        var core = provider.GetRequiredService<IEquipmentDiagnosticCore>();

        var result = await core.DiagnoseAsync(new DiagnosticCoreRequest(
            "Gree",
            code,
            Series: series));

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(series, result.Match?.Series);
        Assert.Equal(code, result.CanonicalCode);
        Assert.NotEmpty(result.LocalizedGuidance);
        Assert.NotNull(result.SignalType);
        Assert.NotNull(result.Severity);
    }

    [Fact]
    public async Task NeutralGuidanceUsesChannelIndependentAudiences()
    {
        using var provider = CreateProvider();
        var core = provider.GetRequiredService<IEquipmentDiagnosticCore>();

        var result = await core.DiagnoseAsync(new DiagnosticCoreRequest(
            "Gree",
            "C0",
            Series: "GMV Mini"));

        Assert.Equal(
            [DiagnosticAudience.Consumer, DiagnosticAudience.Installer, DiagnosticAudience.Engineer],
            result.LocalizedGuidance
                .Select(guidance => guidance.Audience)
                .Distinct()
                .Order()
                .ToArray());
    }

    [Fact]
    public async Task BotCompatibilityServiceDelegatesExactlyOnceToNeutralCore()
    {
        using var provider = CreateProvider();
        var realCore = provider.GetRequiredService<IEquipmentDiagnosticCore>();
        var request = new EquipmentDiagnosticBotRequest("Gree", "E9", Series: "U-Match R32");
        var expected = await realCore.DiagnoseAsync(
            EquipmentDiagnosticBotCompatibilityMapper.ToCoreRequest(request));
        var fakeCore = new CapturingCore(expected);
        var service = new EquipmentDiagnosticBotService(fakeCore);

        var response = await service.DiagnoseAsync(request);

        Assert.Equal(1, fakeCore.CallCount);
        Assert.Equal(request.Manufacturer, fakeCore.LastRequest?.Manufacturer);
        Assert.Equal(request.Code, fakeCore.LastRequest?.Code);
        Assert.Equal(
            JsonSerializer.Serialize(EquipmentDiagnosticBotCompatibilityMapper.ToBotResponse(expected)),
            JsonSerializer.Serialize(response));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private sealed class CapturingCore(DiagnosticCoreResult result) : IEquipmentDiagnosticCore
    {
        public int CallCount { get; private set; }
        public DiagnosticCoreRequest? LastRequest { get; private set; }

        public Task<DiagnosticCoreResult> DiagnoseAsync(
            DiagnosticCoreRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(result);
        }
    }
}
