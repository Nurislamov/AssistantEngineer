using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class RussianDiagnosticTerminologyTests
{
    [Theory]
    [InlineData(ErrorKnowledgeSignalType.Fault, "Ошибка")]
    [InlineData(ErrorKnowledgeSignalType.Protection, "Защита")]
    [InlineData(ErrorKnowledgeSignalType.Warning, "Предупреждение")]
    [InlineData(ErrorKnowledgeSignalType.Status, "Статус")]
    [InlineData(ErrorKnowledgeSignalType.Debug, "Наладка / ввод в эксплуатацию")]
    [InlineData(ErrorKnowledgeSignalType.Commissioning, "Наладка / ввод в эксплуатацию")]
    [InlineData(ErrorKnowledgeSignalType.Communication, "Связь")]
    public void SignalTypesHaveNaturalRussianLabels(
        ErrorKnowledgeSignalType signalType,
        string expected)
    {
        Assert.Equal(expected, RussianDiagnosticTerminology.SignalTypeLabel(signalType));
    }

    [Theory]
    [InlineData(ErrorKnowledgeEquipmentType.IndoorUnit, "Внутренний блок")]
    [InlineData(ErrorKnowledgeEquipmentType.OutdoorUnit, "Наружный блок")]
    [InlineData(ErrorKnowledgeEquipmentType.WiredRemote, "Проводной пульт")]
    [InlineData(ErrorKnowledgeEquipmentType.CentralController, "Центральный контроллер")]
    [InlineData(ErrorKnowledgeEquipmentType.Unknown, "Не указано")]
    public void EquipmentTypesHaveNaturalRussianLabels(
        ErrorKnowledgeEquipmentType equipmentType,
        string expected)
    {
        Assert.Equal(expected, RussianDiagnosticTerminology.EquipmentTypeLabel(equipmentType));
    }

    [Fact]
    public void RepeatedManualBoundaryTextIsMadeReadableWithoutAddingTechnicalMeaning()
    {
        var text = RussianDiagnosticTerminology.ImprovePhrase(
            "Использовать исходную формулировку руководства и указанный раздел как границу диагностики.");

        Assert.Equal(
            "Сверить код с указанным разделом руководства и не выходить за рамки процедуры для этой модели.",
            text);
    }
}
