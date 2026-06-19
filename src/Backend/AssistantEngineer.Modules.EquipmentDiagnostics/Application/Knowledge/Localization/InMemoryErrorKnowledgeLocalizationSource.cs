using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;

public sealed class InMemoryErrorKnowledgeLocalizationSource : IErrorKnowledgeLocalizationSource
{
    private static readonly DateTimeOffset SeedTimestamp =
        new(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyCollection<ErrorKnowledgeEntryV2> Entries =
    [
        GreeGmvH5()
    ];

    public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => Entries;

    public ErrorKnowledgeLocalizationSelection? Select(
        EquipmentDiagnosticBotResponse response,
        string locale,
        ErrorKnowledgeAudience audience)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var entry = Entries.FirstOrDefault(item =>
            item.Manufacturer.Equals(response.NormalizedManufacturer, StringComparison.OrdinalIgnoreCase) &&
            item.Code.Equals(response.NormalizedCode, StringComparison.OrdinalIgnoreCase) &&
            Matches(item.Series, response.EquipmentContext?.Series) &&
            Matches(item.Model, response.EquipmentContext?.ModelCode));
        var text = entry?.Texts.FirstOrDefault(item =>
            item.Locale.Equals(normalizedLocale, StringComparison.OrdinalIgnoreCase) &&
            item.Audience == audience);
        return entry is null || text is null
            ? null
            : new ErrorKnowledgeLocalizationSelection(entry, text);
    }

    private static bool Matches(string? expected, string? actual) =>
        string.IsNullOrWhiteSpace(expected) ||
        string.IsNullOrWhiteSpace(actual) ||
        expected.Equals(actual, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeLocale(string locale)
    {
        var normalized = locale.Trim().Replace('_', '-').ToLowerInvariant();
        var separator = normalized.IndexOf('-');
        return separator < 0 ? normalized : normalized[..separator];
    }

    private static ErrorKnowledgeEntryV2 GreeGmvH5()
    {
        const string entryId = "gree-gmv-h5";
        return new ErrorKnowledgeEntryV2(
            entryId,
            "Gree",
            "Наружный блок VRF",
            "GMV",
            null,
            "H5",
            "en",
            "SeededEngineeringKnowledge",
            "Gree GMV seeded diagnostic source",
            null,
            "Low",
            "UnverifiedSeed",
            SeedTimestamp,
            SeedTimestamp,
            [
                new ErrorKnowledgeTextV2(
                    $"{entryId}-en-engineer",
                    entryId,
                    "en",
                    ErrorKnowledgeAudience.Engineer,
                    "Gree GMV H5 protection alarm",
                    "Preliminary diagnostic entry for a Gree GMV H5 protection alarm. Verify the exact meaning against the installed-model service manual.",
                    "Qualified electrical and refrigerant-circuit service is required. Keep all protections active.",
                    [
                        "Power-supply or phase condition.",
                        "Protection wiring, connector, or sensor condition.",
                        "Compressor or inverter protection condition requiring proper measurements."
                    ],
                    [
                        "Confirm the installed model, GMV series, controller, and displayed H5 code.",
                        "Follow the manufacturer service procedure for qualified measurements."
                    ],
                    [
                        "Do not conclude compressor or inverter failure from the code alone.",
                        "Do not bypass protection circuits."
                    ],
                    "Verify the code and measured conditions against the exact service manual.",
                    "Original English seed text retained as source material.",
                    IsMachineTranslated: false,
                    IsReviewed: false,
                    SeedTimestamp,
                    SeedTimestamp),
                Text(
                    entryId,
                    ErrorKnowledgeAudience.Consumer,
                    "Сработала защита оборудования",
                    "Код H5 указывает на защитное состояние системы. Точное значение зависит от серии и установленной модели.",
                    [],
                    [],
                    [],
                    "Передайте код H5 квалифицированному сервисному специалисту и не разбирайте оборудование самостоятельно."),
                Text(
                    entryId,
                    ErrorKnowledgeAudience.Installer,
                    "Gree GMV H5 — предварительный сигнал защиты",
                    "H5 рассматривается как предварительный сигнал защиты GMV. Точное значение необходимо сверить с сервисным руководством установленной модели.",
                    [
                        "Несоответствие фактической модели, серии или места отображения кода выбранному справочному случаю.",
                        "Состояние электропитания или фаз, которое должен проверять квалифицированный специалист.",
                        "Видимое повреждение соединений, разъёмов или датчиков цепей защиты."
                    ],
                    [
                        "Подтвердить модель наружного блока, серию GMV и устройство, на котором показан H5.",
                        "Поручить квалифицированному специалисту проверку состояния электропитания по документации оборудования.",
                        "При снятом напряжении выполнить визуальный осмотр доступных соединений, разъёмов и датчиков защиты."
                    ],
                    [
                        "Не объявлять неисправность компрессора или инвертора без измерений и проверки по сервисному руководству.",
                        "Не обходить и не отключать защитные цепи."
                    ],
                    "Зафиксировать модель и условия появления H5, затем сверить код с руководством именно для установленного оборудования."),
                Text(
                    entryId,
                    ErrorKnowledgeAudience.Engineer,
                    "Gree GMV H5 — предварительный сигнал защиты",
                    "H5 рассматривается как предварительный сигнал защиты GMV, а не как подтверждённый отказ конкретного узла. Нужна проверка по точной модели.",
                    [
                        "Отклонение параметров электропитания или фаз.",
                        "Нарушение соединений цепей управления и защиты.",
                        "Защитное состояние компрессорно-инверторного контура, требующее штатной диагностики."
                    ],
                    [
                        "Подтвердить модель, серию GMV, контроллер и точное место отображения H5.",
                        "Проверить электропитание и фазное состояние квалифицированным специалистом по сервисной процедуре.",
                        "Осмотреть цепи управления, защиты, разъёмы и датчики на видимые повреждения или следы влаги.",
                        "Сопоставить измерения и последовательность появления кода с сервисным руководством установленной модели."
                    ],
                    [
                        "Не делать вывод о неисправности компрессора, платы или инвертора только по коду H5.",
                        "Не обходить защиты и не выполнять принудительный запуск."
                    ],
                    "Продолжить диагностику только по штатной процедуре производителя для подтверждённой модели.")
            ]);
    }

    private static ErrorKnowledgeTextV2 Text(
        string entryId,
        ErrorKnowledgeAudience audience,
        string title,
        string summary,
        IReadOnlyList<string> causes,
        IReadOnlyList<string> checks,
        IReadOnlyList<string> doNotAdvise,
        string action) =>
        new(
            $"{entryId}-ru-{audience.ToString().ToLowerInvariant()}",
            entryId,
            "ru",
            audience,
            title,
            summary,
            "Работы с электропитанием, компрессором, инвертором и холодильным контуром выполняет только квалифицированный специалист. Защитные устройства должны оставаться включёнными.",
            causes,
            checks,
            doNotAdvise,
            action,
            "Русский текст подготовлен по осторожной интерпретации непроверенного исходного seed-материала.",
            IsMachineTranslated: false,
            IsReviewed: false,
            SeedTimestamp,
            SeedTimestamp);
}
