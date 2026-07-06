using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmv6HrProductionSmokeReadinessTests
{
    private static readonly string[] SmokeQueries =
    [
        "Gree GMV6 HR C0",
        "Gree GMV6 HR C2",
        "Gree GMV6 HR C3",
        "Gree GMV6 HR CH",
        "Gree GMV6 HR CL",
        "Gree GMV6 HR U4",
        "Gree GMV6 HR U6",
        "Gree GMV6 HR U8",
        "Gree GMV6 HR U9",
        "Gree GMV6 HR n0",
        "Gree GMV6 HR n7",
        "Gree GMV6 HR A2"
    ];

    [Fact]
    public async Task RequiredProductionSmokeQueriesAreReadyAndLastPreservesHrSeries()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        for (var index = 0; index < SmokeQueries.Length; index++)
        {
            var query = SmokeQueries[index];
            var code = query.Split(' ').Last();
            var response = await adapter.HandleAsync(Update(query, index + 1));

            Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
            Assert.Contains("Gree GMV6 HR", response.Text, StringComparison.Ordinal);
            Assert.Contains(code, response.Text, StringComparison.Ordinal);
            AssertVisibleTextIsClean(response.Text);
        }

        var last = await adapter.HandleAsync(Update("/last", SmokeQueries.Length + 1));
        Assert.Contains("Gree GMV6 HR A2", last.Text, StringComparison.Ordinal);
        AssertVisibleTextIsClean(last.Text);
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

    private static void AssertVisibleTextIsClean(string text)
    {
        Assert.All(
            new[]
            {
                "Подтвердите код",
                "Сверьте модель",
                "карточка",
                "карточка неисправности",
                "manual",
                "sourceNote",
                "packageId",
                "по таблице",
                "основание",
                "руководство"
            },
            fragment => Assert.DoesNotContain(fragment, text, StringComparison.OrdinalIgnoreCase));
    }
}
