using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramLastHistoryTests
{
    [Theory]
    [InlineData("Gree U-Match GUD71PH1/B-S E9", "Gree U-Match R32 E9", "Gree E9")]
    [InlineData("Gree GMV Mini C0", "Gree GMV Mini C0", "Gree C0")]
    [InlineData("Gree GMV9 Flex C0", "Gree GMV9 Flex C0", "Gree C0")]
    public async Task LastDisplaysMatchedDiagnosticSeries(
        string query,
        string expectedTitle,
        string shortTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var diagnosis = await adapter.HandleAsync(Update(query, updateId: 1));
        var last = await adapter.HandleAsync(Update("/last", updateId: 2));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, diagnosis.ResponseKind);
        Assert.Contains(expectedTitle, last.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(
            shortTitle,
            last.Text.Replace(expectedTitle, string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        services.AddSingleton(new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            DefaultManufacturer = "Gree",
            MaxMessageLength = 1400,
            AllowedChatIds = [7]
        });
        return services.BuildServiceProvider();
    }

    private static EquipmentDiagnosticTelegramUpdate Update(string text, long updateId) =>
        new(updateId, ChatId: 7, Username: "operator", Text: text, UserId: 11);
}
