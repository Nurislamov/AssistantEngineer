using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class ErrorKnowledgeJsonValidationTests
{
    private static readonly string KnowledgePath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "gmv",
        "h5.json");

    [Fact]
    public void GreeGmvH5JsonLoadsFromEmbeddedResource()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();

        var entry = Assert.Single(source.GetEntries());

        Assert.Equal("gree-gmv-h5", entry.Id);
        Assert.Equal("Gree", entry.Manufacturer);
        Assert.Equal("GMV", entry.Series);
        Assert.Equal("H5", entry.Code);
        Assert.Contains(
            JsonErrorKnowledgeLocalizationSource.GetEmbeddedResourceNames(),
            name => name.EndsWith(".gree.gmv.h5.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RepositoryKnowledgeDirectoryLoadsSuccessfully()
    {
        var directory = Path.GetDirectoryName(
            Path.GetDirectoryName(
                Path.GetDirectoryName(KnowledgePath)))!;

        var entries = new ErrorKnowledgeJsonLoader().LoadFromDirectory(directory);

        Assert.Single(entries, entry => entry.Id == "gree-gmv-h5");
    }

    [Fact]
    public void GreeH5ConsumerOutputUsesJsonRussianTextAndIsCustomerSafe()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new JsonErrorKnowledgeLocalizationSource());

        var text = formatter.FormatConsumer(Response(), hasPhoneNumber: false, maxLength: 2000);

        Assert.Contains("Сработала защита оборудования", text, StringComparison.Ordinal);
        Assert.Contains("Код H5 указывает на защитное состояние системы", text, StringComparison.Ordinal);
        Assert.DoesNotContain("измерить напряжение", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("добавить хладагент", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("заменить компрессор", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Installer, "Точное значение необходимо сверить")]
    [InlineData(TelegramUserRole.Engineer, "не как подтверждённый отказ")]
    [InlineData(TelegramUserRole.Admin, "не как подтверждённый отказ")]
    [InlineData(TelegramUserRole.Owner, "не как подтверждённый отказ")]
    public void GreeH5TechnicalRolesUseJsonRussianText(
        TelegramUserRole role,
        string expected)
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new JsonErrorKnowledgeLocalizationSource());

        var text = formatter.FormatTechnical(Response(), role);

        Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.DoesNotContain("Preliminary diagnostic entry", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingRequiredRussianAudienceFailsValidation()
    {
        var json = Mutate(root =>
        {
            var texts = root["texts"]!.AsArray();
            var consumer = texts.Single(node =>
                node!["locale"]!.GetValue<string>() == "ru" &&
                node["audience"]!.GetValue<string>() == "Consumer");
            texts.Remove(consumer);
        });

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("missing required ru text for audience Consumer", StringComparison.Ordinal));
    }

    [Fact]
    public void EnglishUiLabelInRussianTextFailsValidation()
    {
        var json = Mutate(root => RussianConsumer(root)["summary"] = "Safety: остановите оборудование.");

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("English UI label 'Safety'", StringComparison.Ordinal));
    }

    [Fact]
    public void UnsafeConsumerInstructionFailsValidation()
    {
        var json = Mutate(root =>
            RussianConsumer(root)["checkSteps"] = new JsonArray("Измерить напряжение на клеммах."));

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("unsafe advice", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateKnowledgeKeyFailsValidation()
    {
        var json = File.ReadAllText(KnowledgePath);

        var result = new ErrorKnowledgeJsonValidator().Validate(
        [
            new("first.json", json),
            new("second.json", json)
        ]);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("duplicate knowledge key", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("locale", "fr", "locale 'fr' is invalid")]
    [InlineData("audience", "Technician", "audience 'Technician' is invalid")]
    [InlineData("confidence", "Certain", "confidence 'Certain' is invalid")]
    [InlineData("verificationStatus", "Published", "verificationStatus 'Published' is invalid")]
    public void InvalidControlledValueFailsValidation(
        string property,
        string value,
        string expected)
    {
        var json = Mutate(root =>
        {
            if (property is "locale" or "audience")
            {
                RussianConsumer(root)[property] = value;
            }
            else
            {
                root[property] = value;
            }
        });

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Позвоните +998 90 123 45 67.", "phone-number-like")]
    [InlineData("token=123456:abcdefghijklmnopqrstuvwxyzABCDE", "token/webhook-secret-like")]
    [InlineData("Нажмите callback sr:t:12.", "callback payload")]
    [InlineData("Пользователь 123456789012.", "raw chat/platform-user-id-like")]
    public void SensitivePlatformValueFailsValidation(string text, string expected)
    {
        var json = Mutate(root => RussianConsumer(root)["summary"] = text);

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    private static ErrorKnowledgeValidationResult Validate(string json) =>
        new ErrorKnowledgeJsonValidator().Validate([new("test.json", json)]);

    private static string Mutate(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(File.ReadAllText(KnowledgePath))!.AsObject();
        mutation(root);
        return root.ToJsonString();
    }

    private static JsonObject RussianConsumer(JsonObject root) =>
        root["texts"]!
            .AsArray()
            .Select(node => node!.AsObject())
            .Single(node =>
                node["locale"]!.GetValue<string>() == "ru" &&
                node["audience"]!.GetValue<string>() == "Consumer");

    private static EquipmentDiagnosticBotResponse Response() =>
        new(
            EquipmentDiagnosticBotResponseStatus.Answer,
            "English title",
            "English message",
            "Gree",
            "H5",
            new EquipmentDiagnosticBotEquipmentContext(
                "Gree",
                "GMV",
                null,
                EquipmentCategory.VrfOutdoorUnit,
                EquipmentDiagnosticBotEquipmentSide.Outdoor,
                EquipmentDiagnosticBotDisplayContext.OduMainBoardLed),
            new EquipmentDiagnosticBotObservedCodeContext("H5", "H5", null),
            new EquipmentDiagnosticBotAnswerCard(
                "English title",
                "English summary",
                "English verification",
                [],
                [],
                [],
                [],
                []),
            null,
            new EquipmentDiagnosticBotSourceCard(
                "SeededEngineeringKnowledge",
                "UnverifiedSeed",
                "English source",
                null,
                null,
                null,
                null,
                null,
                []),
            new EquipmentDiagnosticBotSafetyCard("English safety", []),
            true,
            DiagnosticConfidence.Low,
            false,
            true,
            [],
            [],
            null);
}
