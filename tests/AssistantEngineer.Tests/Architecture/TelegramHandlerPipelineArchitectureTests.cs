namespace AssistantEngineer.Tests.Architecture;

public sealed class TelegramHandlerPipelineArchitectureTests
{
    [Fact]
    public void AdapterRemainsAThinPipelineRunner()
    {
        var source = ReadTelegramSource("EquipmentDiagnosticTelegramAdapter.cs");

        Assert.True(
            source.Split(Environment.NewLine).Length <= 100,
            "EquipmentDiagnosticTelegramAdapter must remain a thin pipeline runner.");
        Assert.Contains("TelegramUpdateHandlerPipeline _pipeline", source, StringComparison.Ordinal);
        Assert.Contains("_pipeline.HandleAsync(update, cancellationToken)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_parser.Parse(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_botFacade.DiagnoseAsync(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CallbackData?.StartsWith(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PipelineRunsHandlersInRegistrationOrderUntilHandled()
    {
        var source = ReadTelegramSource("TelegramUpdateHandlerPipeline.cs");

        Assert.Contains("foreach (var handler in _handlers)", source, StringComparison.Ordinal);
        Assert.Contains("await handler.TryHandleAsync(update, cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("if (response is not null)", source, StringComparison.Ordinal);
        Assert.Contains("return response;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyHandlersAreRegisteredInDeterministicOrder()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Application",
            "EquipmentDiagnosticsModuleServiceCollectionExtensions.cs"));

        var guard = source.IndexOf(
            "AddSingleton<ITelegramUpdateHandler, TelegramUpdateGuardHandler>()",
            StringComparison.Ordinal);
        var callback = source.IndexOf(
            "AddSingleton<ITelegramUpdateHandler, TelegramCallbackUpdateHandler>()",
            StringComparison.Ordinal);
        var message = source.IndexOf(
            "AddSingleton<ITelegramUpdateHandler, TelegramMessageUpdateHandler>()",
            StringComparison.Ordinal);

        Assert.True(guard >= 0 && guard < callback && callback < message);
        Assert.Contains("AddSingleton<TelegramUpdateHandlerPipeline>()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiCompositionRootDoesNotOwnTelegramHandlerDetails()
    {
        var apiProjectPath = TestPaths.ApiProjectPath;
        var source = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(apiProjectPath, "Program.cs")),
            File.ReadAllText(Path.Combine(apiProjectPath, "AssistantEngineerApiHost.cs")));

        Assert.DoesNotContain("TelegramUpdateGuardHandler", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TelegramCallbackUpdateHandler", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TelegramMessageUpdateHandler", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TelegramUpdateHandlerPipeline", source, StringComparison.Ordinal);
    }

    private static string ReadTelegramSource(string fileName) =>
        File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Application",
            "Telegram",
            fileName));
}
