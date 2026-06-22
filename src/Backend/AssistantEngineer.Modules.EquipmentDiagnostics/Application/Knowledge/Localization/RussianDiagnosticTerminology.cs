using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;

public static class RussianDiagnosticTerminology
{
    public static string SignalTypeLabel(ErrorKnowledgeSignalType signalType) =>
        signalType switch
        {
            ErrorKnowledgeSignalType.Fault => "Ошибка",
            ErrorKnowledgeSignalType.Protection => "Защита",
            ErrorKnowledgeSignalType.Warning => "Предупреждение",
            ErrorKnowledgeSignalType.Status => "Статус",
            ErrorKnowledgeSignalType.Debug or ErrorKnowledgeSignalType.Commissioning =>
                "Наладка / ввод в эксплуатацию",
            ErrorKnowledgeSignalType.Maintenance => "Обслуживание",
            ErrorKnowledgeSignalType.Communication => "Связь",
            ErrorKnowledgeSignalType.RemoteDisplay => "Индикация на пульте",
            _ => "Не указано"
        };

    public static string EquipmentTypeLabel(ErrorKnowledgeEquipmentType equipmentType) =>
        equipmentType switch
        {
            ErrorKnowledgeEquipmentType.IndoorUnit => "Внутренний блок",
            ErrorKnowledgeEquipmentType.OutdoorUnit => "Наружный блок",
            ErrorKnowledgeEquipmentType.WiredRemote => "Проводной пульт",
            ErrorKnowledgeEquipmentType.CentralController => "Центральный контроллер",
            ErrorKnowledgeEquipmentType.Gateway => "Шлюз",
            ErrorKnowledgeEquipmentType.EnergyMeter => "Счётчик энергии",
            ErrorKnowledgeEquipmentType.Chiller => "Чиллер",
            _ => "Не указано"
        };

    public static string EquipmentTypeLabel(EquipmentCategory category) =>
        category switch
        {
            EquipmentCategory.VrfIndoorUnit => "Внутренний блок",
            EquipmentCategory.VrfOutdoorUnit => "Наружный блок",
            EquipmentCategory.Chiller => "Чиллер",
            EquipmentCategory.Controller => "Контроллер / пульт",
            EquipmentCategory.SplitSystem => "Сплит-система",
            _ => "Не указано"
        };

    public static string ImprovePhrase(string value) =>
        value
            .Replace(
                "сообщение о связи связи и адресации",
                "сообщение о связи и адресации",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "Использовать исходную формулировку руководства и указанный раздел как границу диагностики.",
                "Сверить код с указанным разделом руководства и не выходить за рамки процедуры для этой модели.",
                StringComparison.Ordinal)
            .Replace(
                "Сопоставить код, модель и фактические симптомы с указанным разделом руководства; если подробная процедура не перенесена, выполнять её непосредственно по руководству.",
                "Сопоставить код, модель и фактические симптомы с указанным разделом руководства и продолжить диагностику только в границах этого раздела.",
                StringComparison.Ordinal)
            .Replace(
                "категорию «Наладка»",
                "категорию «Наладка / ввод в эксплуатацию»",
                StringComparison.Ordinal)
            .Replace(
                "категории наладки системы",
                "разделу наладки и ввода в эксплуатацию",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "для наладки системы",
                "для наладки и ввода в эксплуатацию",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "категорию «Состояния»",
                "категорию «Статус»",
                StringComparison.Ordinal)
            .Replace(
                "для состояния системы",
                "для статуса системы",
                StringComparison.OrdinalIgnoreCase);
}
