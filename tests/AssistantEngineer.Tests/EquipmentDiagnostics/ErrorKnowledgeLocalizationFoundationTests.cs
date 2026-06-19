using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class ErrorKnowledgeLocalizationFoundationTests
{
    [Fact]
    public void SourceLanguageAndLocalizedAudienceTextsAreSeparated()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();
        var entry = Assert.Single(source.GetEntries(), item => item.Id == "gree-gmv-h5");

        Assert.Equal("en", entry.SourceLanguage);
        Assert.Equal("SeededEngineeringKnowledge", entry.SourceType);
        Assert.Contains(entry.Texts, item => item.Locale == "en" && item.Audience == ErrorKnowledgeAudience.Engineer);
        Assert.Contains(entry.Texts, item => item.Locale == "ru" && item.Audience == ErrorKnowledgeAudience.Consumer);
        Assert.Contains(entry.Texts, item => item.Locale == "ru" && item.Audience == ErrorKnowledgeAudience.Installer);
        Assert.Contains(entry.Texts, item => item.Locale == "ru" && item.Audience == ErrorKnowledgeAudience.Engineer);
        Assert.All(entry.Texts, item => Assert.False(item.IsMachineTranslated));
    }

    [Fact]
    public void AudienceSpecificRussianTextIsSelected()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();
        var response = Response("Gree", "H5", "GMV");

        var installer = source.Select(response, "ru-RU", ErrorKnowledgeAudience.Installer);
        var engineer = source.Select(response, "ru", ErrorKnowledgeAudience.Engineer);

        Assert.NotNull(installer);
        Assert.NotNull(engineer);
        Assert.Contains("предварительный сигнал защиты", installer.Text.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не как подтверждённый отказ", engineer.Text.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(installer.Text.Summary, engineer.Text.Summary);
    }

    [Fact]
    public void MissingAudienceLocalizationUsesRussianFallbackWithoutEnglishSourceText()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(new EmptyLocalizationSource());
        var response = Response("Other", "X1", null);

        var text = formatter.FormatTechnical(response, TelegramUserRole.Installer);

        Assert.Contains("Техническое описание пока не локализовано", text, StringComparison.Ordinal);
        Assert.Contains("Источник: встроенный черновой каталог", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Preliminary diagnostic entry", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Qualified technician required", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Safety", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Step", text, StringComparison.Ordinal);
    }

    private static EquipmentDiagnosticBotResponse Response(
        string manufacturer,
        string code,
        string? series) =>
        new(
            EquipmentDiagnosticBotResponseStatus.Answer,
            "English title",
            "Preliminary diagnostic entry.",
            manufacturer,
            code,
            series is null
                ? null
                : new EquipmentDiagnosticBotEquipmentContext(
                    manufacturer,
                    series,
                    null,
                    EquipmentCategory.VrfOutdoorUnit,
                    EquipmentDiagnosticBotEquipmentSide.Outdoor,
                    EquipmentDiagnosticBotDisplayContext.OduMainBoardLed),
            new EquipmentDiagnosticBotObservedCodeContext(code, code, null),
            new EquipmentDiagnosticBotAnswerCard(
                "English title",
                "English technical summary.",
                "Verification required.",
                ["English cause."],
                [],
                [],
                ["English check."],
                []),
            null,
            new EquipmentDiagnosticBotSourceCard(
                "SeededEngineeringKnowledge",
                "UnverifiedSeed",
                "English source summary.",
                null,
                null,
                null,
                null,
                null,
                []),
            new EquipmentDiagnosticBotSafetyCard(
                "Qualified technician required.",
                ["Do not bypass protections."]),
            true,
            DiagnosticConfidence.Low,
            false,
            true,
            ["English step."],
            [],
            null);

    private sealed class EmptyLocalizationSource : IErrorKnowledgeLocalizationSource
    {
        public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => [];

        public ErrorKnowledgeLocalizationSelection? Select(
            EquipmentDiagnosticBotResponse response,
            string locale,
            ErrorKnowledgeAudience audience) =>
            null;
    }
}
