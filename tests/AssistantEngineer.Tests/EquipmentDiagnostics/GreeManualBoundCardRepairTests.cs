using System.Text.Json.Nodes;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeManualBoundCardRepairTests
{
    private static readonly string GreeRoot = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string[] TelegramVisibleFields =
    [
        "title",
        "summary",
        "possibleCauses",
        "checkSteps",
        "recommendedAction",
        "safetyNote",
        "sourceNote"
    ];

    private static readonly string[] ForbiddenVisiblePhrases =
    [
        "по таблице руководства",
        "классифицирован по таблице",
        "источник",
        "manual",
        "source",
        "document code",
        "manualId",
        "sourceReference",
        "packageId",
        "sourceMeaning",
        "текущая карточка",
        "диагностический вывод должен оставаться",
        "точная исходная формулировка",
        "не расширяйте трактовку",
        "дальнейшие действия выполните по сервисной процедуре"
    ];

    private static readonly (string FileName, string Code, string SensorText)[] Gmv6OutdoorSensorBatch =
    [
        ("b1.json", "b1", "датчика температуры наружного воздуха"),
        ("b2.json", "b2", "датчика температуры оттайки 1"),
        ("b3.json", "b3", "датчика температуры оттайки 2"),
        ("b4.json", "b4", "датчика температуры жидкости на выходе субохладителя"),
        ("b5.json", "b5", "датчика температуры газа на выходе субохладителя"),
        ("b6.json", "b6", "датчика температуры всасывания 1"),
        ("b7.json", "b7", "датчика температуры всасывания 2"),
        ("b8.json", "b8", "датчика влажности наружного воздуха"),
        ("b9.json", "b9", "датчика температуры газа на выходе теплообменника"),
        ("ba.json", "bA", "датчика температуры возврата масла")
    ];

    private static readonly (string FileName, string Code, string SensorText)[] Gmv6OutdoorDischargeTemperatureBatch =
    [
        ("f5.json", "F5", "датчик температуры нагнетания компрессора 1"),
        ("f6.json", "F6", "датчик температуры нагнетания компрессора 2"),
        ("f7.json", "F7", "датчик температуры нагнетания компрессора 3"),
        ("f8.json", "F8", "датчик температуры нагнетания компрессора 4"),
        ("f9.json", "F9", "датчик температуры нагнетания компрессора 5"),
        ("fa.json", "FA", "датчик температуры нагнетания компрессора 6")
    ];

    private static readonly Dictionary<string, (string Code, int CauseCount, string[] RequiredPhrases)> Gmv6OutdoorFanDriveBatch =
        new(StringComparer.Ordinal)
        {
            ["h0.json"] = ("H0", 7, ["платы привода вентилятора", "проводной контроллер", "2-разрядном LED", "H3", "H7", "H8", "HC", "HF", "H9", "HJ"]),
            ["h1.json"] = ("H1", 3, ["ненормальная работа платы привода вентилятора", "проводной контроллер", "2-разрядном LED", "H6", "H5", "C3"]),
            ["h2.json"] = ("H2", 2, ["защита напряжения питания платы привода вентилятора", "проводной контроллер", "2-разрядном LED", "HH", "HL"]),
            ["h3.json"] = ("H3", 1, ["защита сброса модуля привода вентилятора", "P3", "плату привода компрессора", "плату привода вентилятора"]),
            ["h5.json"] = ("H5", 4, ["перегрузке тока", "U, V, W", "меньше 10 Ом", "больше 2 МОм", "заменить вентилятор", "заменить плату привода вентилятора"]),
            ["h6.json"] = ("H6", 4, ["защита IPM-модуля", "U, V и W", "меньше 10 Ом", "больше 2 МОм", "замените вентилятор", "замените плату привода вентилятора"]),
            ["h7.json"] = ("H7", 1, ["датчика температуры привода вентилятора", "повторно включите блок более трёх раз", "замените плату привода вентилятора"]),
            ["h8.json"] = ("H8", 3, ["защита IPM привода вентилятора от перегрева", "термопаст", "винты", "замените плату привода вентилятора"]),
            ["h9.json"] = ("H9", 4, ["потери синхронизации", "U, V и W", "меньше 10 Ом", "больше 2 МОм", "замените вентилятор", "замените плату привода вентилятора"]),
            ["hc.json"] = ("HC", 1, ["цепи детекции тока привода вентилятора", "повторно включите блок более трёх раз", "замените плату привода вентилятора"]),
            ["hh.json"] = ("HH", 2, ["повышенного напряжения", "превышает 460 В", "приведите напряжение к 380 В", "замените плату привода вентилятора"]),
            ["hj.json"] = ("HJ", 4, ["отказ запуска инверторного вентилятора", "U, V и W", "меньше 10 Ом", "больше 2 МОм", "замените вентилятор", "замените плату привода вентилятора"]),
            ["hl.json"] = ("HL", 2, ["пониженного напряжения", "ниже 320 В", "приведите напряжение к 380 В", "замените плату привода вентилятора"])
        };

    private static readonly Dictionary<string, (string Code, string ComponentText, string DurationText, string[] RequiredPhrases)> Gmv6OutdoorCurrentAndShellRoofSensorBatch =
        new(StringComparer.Ordinal)
        {
            ["fh.json"] = ("FH", "датчик тока компрессора 1", "3 секунд", ["малая плата датчика тока", "цепь детекции", "замените основную плату управления"]),
            ["fc.json"] = ("FC", "датчик тока компрессора 2", "3 секунд", ["малая плата датчика тока", "цепь детекции", "замените основную плату управления"]),
            ["fl.json"] = ("FL", "датчик тока компрессора 3", "3 секунд", ["малая плата датчика тока", "цепь детекции", "замените основную плату управления"]),
            ["fe.json"] = ("FE", "датчик тока компрессора 4", "3 секунд", ["малая плата датчика тока", "цепь детекции", "замените основную плату управления"]),
            ["ff.json"] = ("FF", "датчик тока компрессора 5", "3 секунд", ["малая плата датчика тока", "цепь детекции", "замените основную плату управления"]),
            ["fj.json"] = ("FJ", "датчик тока компрессора 6", "3 секунд", ["малая плата датчика тока", "цепь детекции", "замените основную плату управления"]),
            ["fu.json"] = ("FU", "датчик температуры верхней части корпуса компрессора 1", "30 секунд", ["датчик температуры верхней части корпуса", "цепь детекции", "замените основную плату управления"]),
            ["fb.json"] = ("Fb", "датчик температуры верхней части корпуса компрессора 2", "30 секунд", ["датчик температуры верхней части корпуса", "цепь детекции", "замените основную плату управления"])
        };

    private static readonly Dictionary<string, (string Code, int CauseCount, string[] RequiredPhrases)> Gmv6OutdoorEfProtectionAndSensorBatch =
        new(StringComparer.Ordinal)
        {
            ["e1.json"] = ("E1", 8, ["выше 65 °C", "выключатель высокого давления", "4,2 МПа", "запорный клапан", "50 °C", "по 1 кг хладагента"]),
            ["e2.json"] = ("E2", 4, ["ниже 10 °C", "датчик температуры верхней части корпуса", "0 PLS", "200 PLS", "проектными требованиями"]),
            ["e3.json"] = ("E3", 7, ["ниже -41 °C", "датчик низкого давления", "недостаточно хладагента", "DIP-переключатель", "воздушный фильтр"]),
            ["e4.json"] = ("E4", 7, ["выше 118 °C", "запорные клапаны", "электронный расширительный клапан", "-5 °C", "+50 °C", "-20 °C", "+24 °C"]),
            ["f0.json"] = ("F0", 3, ["адресный чип", "чип памяти", "чип часов", "CPU", "плату привода компрессора", "основную плату управления"]),
            ["f1.json"] = ("F1", 4, ["датчик высокого давления", "AD-значение", "30 секунд", "точкой измерения давления", "замените основную плату управления"]),
            ["f3.json"] = ("F3", 4, ["датчик низкого давления", "AD-значение", "30 секунд", "точкой измерения давления", "замените основную плату управления"])
        };

    private static readonly Dictionary<string, (string Code, int CauseCount, string[] RequiredPhrases)> Gmv6OutdoorJProtectionBatch =
        new(StringComparer.Ordinal)
        {
            ["j0.json"] = ("J0", 1, ["многомодульной системе", "других исправно работающих модулях", "исходного модуля"]),
            ["j1.json"] = ("J1", 3, ["компрессора 1", "60 °C", "320-460 В", "модуль привода", "замените компрессор"]),
            ["j2.json"] = ("J2", 3, ["компрессора 2", "60 °C", "320-460 В", "модуль привода", "замените компрессор"]),
            ["j3.json"] = ("J3", 3, ["компрессора 3", "60 °C", "320-460 В", "модуль привода", "замените компрессор"]),
            ["j4.json"] = ("J4", 3, ["компрессора 4", "60 °C", "320-460 В", "модуль привода", "замените компрессор"]),
            ["j5.json"] = ("J5", 3, ["компрессора 5", "60 °C", "320-460 В", "модуль привода", "замените компрессор"]),
            ["j6.json"] = ("J6", 3, ["компрессора 6", "60 °C", "320-460 В", "модуль привода", "замените компрессор"]),
            ["j7.json"] = ("J7", 3, ["четырёхходового клапана", "0,1 МПа", "220 В", "замените четырёхходовой клапан"]),
            ["j8.json"] = ("J8", 2, ["превышает 8", "-5 °C", "+50 °C", "-20 °C", "+24 °C", "датчиков высокого и низкого давления"]),
            ["j9.json"] = ("J9", 2, ["меньше 1,8", "-5 °C", "+50 °C", "-20 °C", "+24 °C", "датчиков высокого и низкого давления"])
        };

    private static readonly Dictionary<string, (string Code, int CauseCount, string[] RequiredPhrases)> Gmv6OutdoorCompressorDrivePBatch =
        new(StringComparer.Ordinal)
        {
            ["p0.json"] = ("P0", 7, ["проводном контроллере внутреннего блока", "2-разрядном LED", "P3", "P7", "P8", "PC", "PF", "P9", "PJ"]),
            ["p1.json"] = ("P1", 3, ["проводном контроллере внутреннего блока", "2-разрядном LED", "P5", "P6", "C2"]),
            ["p2.json"] = ("P2", 2, ["проводном контроллере внутреннего блока", "2-разрядном LED", "PH", "PL"]),
            ["p3.json"] = ("P3", 1, ["защита сброса модуля привода компрессора", "более трёх раз", "заменить плату привода компрессора"]),
            ["p5.json"] = ("P5", 5, ["перегрузке тока", "U, V и W", "меньше 2 Ом", "больше 2 МОм", "PJ", "P6", "P9", "заменить плату привода компрессора"]),
            ["p6.json"] = ("P6", 5, ["защита IPM-модуля", "U, V и W", "меньше 2 Ом", "больше 2 МОм", "PJ", "P5", "P9", "заменить плату привода компрессора"]),
            ["p7.json"] = ("P7", 1, ["датчика температуры платы привода компрессора", "более трёх раз", "заменить плату привода компрессора"]),
            ["p8.json"] = ("P8", 3, ["защита IPM привода компрессора от перегрева", "винты IPM-модуля", "термопаст", "заменить плату привода компрессора"]),
            ["p9.json"] = ("P9", 2, ["потере синхронизации", "U, V и W", "меньше 2 Ом", "больше 2 МОм", "PJ", "P6", "P5", "заменить плату привода компрессора"]),
            ["pc.json"] = ("PC", 1, ["цепи детекции тока привода компрессора", "более трёх раз", "заменить плату привода компрессора"]),
            ["ph.json"] = ("PH", 2, ["выше 460 В", "привести напряжение к 380 В", "заменить плату привода компрессора"]),
            ["pl.json"] = ("PL", 2, ["ниже 320 В", "привести напряжение к 380 В", "заменить плату привода компрессора"]),
            ["pj.json"] = ("PJ", 3, ["отказ запуска инверторного компрессора", "U, V и W", "меньше 2 Ом", "больше 2 МОм", "P9", "P6", "P5", "заменить плату привода компрессора"])
        };

    private static readonly Dictionary<string, (string Code, string[] RequiredPhrases)> Gmv6OutdoorTableOnlyBefBatch =
        new(StringComparer.Ordinal)
        {
            ["bb.json"] = ("bb", ["датчика температуры возврата масла 4"]),
            ["bd.json"] = ("bd", ["датчика температуры газа на входе субохладителя"]),
            ["be.json"] = ("bE", ["датчика температуры входной трубки конденсатора"]),
            ["bf.json"] = ("bF", ["датчика температуры выходной трубки конденсатора"]),
            ["bh.json"] = ("bH", ["системных часов"]),
            ["bj.json"] = ("bJ", ["датчиков высокого и низкого давления"]),
            ["bn.json"] = ("bn", ["датчика температуры жидкости на входе субохладителя"]),
            ["bp.json"] = ("bP", ["датчика температуры возврата масла 2"]),
            ["bu.json"] = ("bU", ["датчика температуры возврата масла 3"]),
            ["e0.json"] = ("E0", ["наружного блока"]),
            ["ed.json"] = ("Ed", ["IPM привода", "низкой температуре"]),
            ["fd.json"] = ("Fd", ["датчика температуры выходной трубки переключателя режимов"]),
            ["fn.json"] = ("Fn", ["датчика температуры входной трубки переключателя режимов"]),
            ["fp.json"] = ("FP", ["DC-двигателя"])
        };

    private static readonly Dictionary<string, (string Code, string[] RequiredPhrases)> Gmv6OutdoorTableOnlyGBatch =
        new(StringComparer.Ordinal)
        {
            ["g0.json"] = ("G0", ["PV", "обратного подключения"]),
            ["g1.json"] = ("G1", ["PV", "островного режима"]),
            ["g2.json"] = ("G2", ["PV DC", "перегрузке тока"]),
            ["g3.json"] = ("G3", ["перегрузку выработки PV"]),
            ["g4.json"] = ("G4", ["PV", "току утечки"]),
            ["g5.json"] = ("G5", ["отсутствия фазы", "электросети"]),
            ["g6.json"] = ("G6", ["PV LVRT"]),
            ["g7.json"] = ("G7", ["частоте электросети"]),
            ["g8.json"] = ("G8", ["перегрузке тока", "электросети"]),
            ["g9.json"] = ("G9", ["IPM-модуля", "электросети"]),
            ["ga.json"] = ("GA", ["входному напряжению", "электросети"]),
            ["gb.json"] = ("Gb", ["реле", "электросети"]),
            ["gc.json"] = ("GC", ["аппаратную защиту", "PV DC", "перегрузке тока"]),
            ["gd.json"] = ("Gd", ["току", "электросети"]),
            ["ge.json"] = ("GE", ["напряжению PV"]),
            ["gf.json"] = ("GF", ["нейтральной точки DC-шины"]),
            ["gh.json"] = ("GH", ["PV DC/DC"]),
            ["gj.json"] = ("GJ", ["модуля", "высокой температуры"]),
            ["gl.json"] = ("GL", ["аппаратную защиту", "электросети", "перегрузке тока"]),
            ["gn.json"] = ("Gn", ["сопротивлению изоляции"]),
            ["gp.json"] = ("GP", ["датчика температуры", "электросети"]),
            ["gu.json"] = ("GU", ["цепи зарядки"]),
            ["gy.json"] = ("Gy", ["питания PV"])
        };

    private static readonly Dictionary<string, (string Code, string[] RequiredPhrases)> Gmv6OutdoorTableOnlyHjpBatch =
        new(StringComparer.Ordinal)
        {
            ["h4.json"] = ("H4", ["PFC", "привода вентилятора"]),
            ["ha.json"] = ("HA", ["чипа памяти", "инверторного наружного вентилятора"]),
            ["he.json"] = ("HE", ["отсутствие фазы", "инверторного вентилятора"]),
            ["hf.json"] = ("HF", ["цепи зарядки", "привода вентилятора"]),
            ["hp.json"] = ("HP", ["инверторного вентилятора", "переменному току"]),
            ["hu.json"] = ("HU", ["AC-напряжение", "инверторного вентилятора"]),
            ["ja.json"] = ("JA", ["ненормального давления"]),
            ["jc.json"] = ("JC", ["выключателя протока воды"]),
            ["je.json"] = ("JE", ["трубы возврата масла"]),
            ["jf.json"] = ("JF", ["утечку трубы возврата масла"]),
            ["jl.json"] = ("JL", ["низкого высокого давления"]),
            ["p4.json"] = ("P4", ["PFC", "привода компрессора"]),
            ["pa.json"] = ("PA", ["чипа памяти", "привода компрессора"]),
            ["pe.json"] = ("PE", ["отсутствие фазы", "инверторного компрессора"]),
            ["pf.json"] = ("PF", ["цепи зарядки", "привода компрессора"]),
            ["pp.json"] = ("PP", ["инверторного компрессора", "переменному току"]),
            ["pu.json"] = ("PU", ["AC-напряжение", "инверторного компрессора"])
        };

    private static readonly Dictionary<string, (string Code, int CauseCount, string[] RequiredPhrases)> Gmv6IndoorDetailedD10aBatch =
        new(StringComparer.Ordinal)
        {
            ["d1.json"] = ("d1", 2, ["адресный чип", "чип памяти", "основную плату управления внутреннего блока"]),
            ["d3.json"] = ("d3", 3, ["датчика температуры воздуха", "AD-значение", "5 секунд", "разъём", "замените основную плату управления внутреннего блока"]),
            ["d4.json"] = ("d4", 3, ["датчика температуры входной трубки", "AD-значение", "5 секунд", "разъём", "замените основную плату управления внутреннего блока"]),
            ["d6.json"] = ("d6", 3, ["датчика температуры выходной трубки", "AD-значение", "5 секунд", "разъём", "замените основную плату управления внутреннего блока"]),
            ["d7.json"] = ("d7", 3, ["датчика влажности", "AD-значение", "5 секунд", "разъём", "замените основную плату управления внутреннего блока"]),
            ["d9.json"] = ("d9", 3, ["перемыч", "номер перемычки", "цепь детекции", "замените основную плату управления внутреннего блока"]),
            ["da.json"] = ("dA", 3, ["IP-адрес", "равен 0", "конфликт", "единичная", "основную плату внутреннего блока", "наружного блока"]),
            ["dc.json"] = ("dC", 2, ["DIP-переключателя мощности", "цепь детекции", "замените основную плату управления внутреннего блока"]),
            ["dh.json"] = ("dH", 2, ["IIC-связ", "сенсорная клавиатура", "дисплейной платой", "замените проводной контроллер"]),
            ["dl.json"] = ("dL", 3, ["датчика температуры воздуха на выходе", "AD-значение", "5 секунд", "разъём", "замените основную плату управления внутреннего блока"])
        };

    private static readonly Dictionary<string, (string Code, int CauseCount, string[] RequiredPhrases)> Gmv6IndoorDetailedL10bBatch =
        new(StringComparer.Ordinal)
        {
            ["l0.json"] = ("L0", 1, ["инженерного номера", "неисправного внутреннего блока", "фактическому коду"]),
            ["l1.json"] = ("L1", 2, ["вентилятора внутреннего блока", "DC", "PG", "двигатель", "реле перегрузки", "основную плату внутреннего блока"]),
            ["l3.json"] = ("L3", 4, ["поплавковый выключатель", "дренажный насос", "дренажную трубу", "засорена"]),
            ["l4.json"] = ("L4", 3, ["проводного контроллера", "замкнут", "замените проводной контроллер", "основную плату внутреннего блока"]),
            ["l5.json"] = ("L5", 4, ["замерзания", "фильтр", "испаритель", "хладагент", "скорость вентилятора"]),
            ["l7.json"] = ("L7", 3, ["главный внутренний блок", "одной системе питания", "заново установите"]),
            ["l9.json"] = ("L9", 2, ["больше 16", "P14", "фактически подключённому количеству"]),
            ["la.json"] = ("LA", 1, ["разным сериям", "одной серии", "проводному контроллеру"]),
            ["lc.json"] = ("LC", 1, ["не соответствует наружному блоку", "не может распознать", "совместимый"])
        };

    private static readonly Dictionary<string, (string Code, string Meaning)> Gmv6IndoorTableOnlyD11aBatch =
        new(StringComparer.Ordinal)
        {
            ["d2.json"] = ("d2", "неисправность нижнего датчика температуры воды в баке"),
            ["d5.json"] = ("d5", "неисправность датчика температуры средней части трубки"),
            ["d8.json"] = ("d8", "неисправность датчика температуры воды"),
            ["db.json"] = ("db", "статус отладки"),
            ["dd.json"] = ("dd", "неисправность датчика температуры солнечной системы"),
            ["de.json"] = ("dE", "неисправность датчика CO₂ внутреннего блока"),
            ["df.json"] = ("dF", "неисправность верхнего датчика температуры воды в баке"),
            ["dj.json"] = ("dJ", "неисправность датчика температуры обратной воды"),
            ["dn.json"] = ("dn", "неисправность механизма качания"),
            ["dp.json"] = ("dP", "неисправность датчика температуры входной трубки генератора"),
            ["du.json"] = ("dU", "неисправность датчика температуры дренажной трубки генератора"),
            ["dy.json"] = ("dy", "неисправность датчика температуры воды"),
            ["l2.json"] = ("L2", "защита вспомогательного нагревателя"),
            ["l6.json"] = ("L6", "конфликт режимов"),
            ["l8.json"] = ("L8", "недостаточная мощность"),
            ["lb.json"] = ("Lb", "несоответствие внутренних блоков при групповом управлении системой осушения с повторным нагревом"),
            ["le.json"] = ("LE", "ненормальная частота вращения водяного насоса EC DC"),
            ["lf.json"] = ("LF", "неисправность настройки перепускного клапана")
        };

    [Fact]
    public void TelegramVisibleGreeTextsDoNotExposeImportOrProvenanceWording()
    {
        foreach (var (path, entry) in ReadEntries())
        {
            var visible = VisibleBlob(entry);

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.False(
                string.IsNullOrWhiteSpace(visible),
                $"Visible Telegram text is empty: {path}");
        }
    }

    [Fact]
    public void Gmv6AjIsAFilterCleaningPromptWithTheDocumentedAction()
    {
        var entry = ReadEntry("gmv6", "status", "aj.json");
        var visible = VisibleBlob(entry);

        Assert.Contains("чистк", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("фильтр", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("статус", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("сброс", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("сервисный цикл", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("интервал", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "напоминание по обслуживанию оборудования",
            visible,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "наружного блока",
            visible,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "основная плата",
            visible,
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(RequiredArray(RequiredTexts(entry)[0], "possibleCauses"));

        Assert.Equal("OutdoorUnit", RequiredString(entry, "equipmentType"));
        Assert.Equal("OutdoorBoard", RequiredString(entry, "displaySource"));
        Assert.Contains(
            "2.12",
            RequiredString(entry, "sourceReference"),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gmv6B1ContainsTheDocumentedDiagnosisCausesAndFlowchart()
    {
        var entry = ReadEntry("gmv6", "outdoor", "b1.json");
        var visible = VisibleBlob(entry);

        Assert.Contains(
            "датчика температуры наружного воздуха",
            visible,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("30 секунд", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("плохой контакт", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("разъём", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("датчик температуры", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("цепь детекции", visible, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("основную плату", visible, StringComparison.OrdinalIgnoreCase);

        var technicalTexts = RequiredTexts(entry)
            .Where(text =>
                !string.Equals(
                    RequiredString(text, "audience"),
                    "Consumer",
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(technicalTexts);
        Assert.All(
            technicalTexts,
            text => Assert.True(
                RequiredArray(text, "checkSteps").Count >= 3,
                $"Audience {RequiredString(text, "audience")} must retain all flowchart steps."));
    }

    [Fact]
    public void Gmv6OutdoorSensorBatchB1ToBAContainsManualDiagnosisCausesAndFlowchart()
    {
        foreach (var (fileName, code, sensorText) in Gmv6OutdoorSensorBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains($"Gree GMV6 — {code} —", visible, StringComparison.Ordinal);
            Assert.Contains(sensorText, visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("30 секунд", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("основная плата наружного блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("проводной контроллер внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("приёмник сигнала внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("плохой контакт", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("разъём", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("неисправен датчик", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("цепь детекции", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("замените датчик", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("замените основную плату", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text => Assert.Equal(3, RequiredArray(text, "possibleCauses").Count));

            var technicalTexts = RequiredTexts(entry)
                .Where(text =>
                    !string.Equals(
                        RequiredString(text, "audience"),
                        "Consumer",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.NotEmpty(technicalTexts);
            Assert.All(
                technicalTexts,
                text =>
                {
                    Assert.Contains("AD", RequiredString(text, "summary"), StringComparison.OrdinalIgnoreCase);
                    Assert.True(
                        RequiredArray(text, "checkSteps").Count >= 3,
                        $"Audience {RequiredString(text, "audience")} for {code} must retain all flowchart steps.");
                });
        }
    }

    [Fact]
    public void Gmv6OutdoorF5ToFAContainsManualDiagnosisCausesAndFlowchart()
    {
        foreach (var (fileName, code, sensorText) in Gmv6OutdoorDischargeTemperatureBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains($"Gree GMV6 — {code} —", visible, StringComparison.Ordinal);
            Assert.Contains(sensorText, visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AD-значение", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("30 секунд", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("основная плата наружного блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("проводной контроллер внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("приёмник сигнала внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("плохой контакт", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("разъём", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("посторонних предметов", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("неисправен ли датчик температуры нагнетания", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("цепь детекции", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("замените датчик", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("замените основную плату", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text => Assert.Equal(3, RequiredArray(text, "possibleCauses").Count));

            Assert.All(
                RequiredTexts(entry),
                text => Assert.True(
                    RequiredArray(text, "checkSteps").Count >= 3,
                    $"Audience {RequiredString(text, "audience")} for {code} must retain all flowchart steps."));
        }
    }

    [Fact]
    public void Gmv6OutdoorFanDriveH0ToHLContainsManualDiagnosisCausesAndFlowchart()
    {
        foreach (var (fileName, expectation) in Gmv6OutdoorFanDriveBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            if (expectation.Code == "H5")
            {
                Assert.Contains("Gree GMV6 — H5 —", visible, StringComparison.Ordinal);
            }
            else
            {
                Assert.Contains($"Gree GMV6 — {expectation.Code} —", visible, StringComparison.Ordinal);
            }
            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text => Assert.Equal(expectation.CauseCount, RequiredArray(text, "possibleCauses").Count));

            Assert.All(
                RequiredTexts(entry),
                text => Assert.True(
                    RequiredArray(text, "checkSteps").Count >= 2,
                    $"Audience {RequiredString(text, "audience")} for {expectation.Code} must retain the documented troubleshooting steps."));
        }
    }

    [Fact]
    public void Gmv6OutdoorCurrentAndShellRoofSensorBatchContainsManualDiagnosisCausesAndFlowchart()
    {
        foreach (var (fileName, expectation) in Gmv6OutdoorCurrentAndShellRoofSensorBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains($"Gree GMV", visible, StringComparison.Ordinal);
            Assert.Contains(expectation.Code, visible, StringComparison.Ordinal);
            Assert.Contains(expectation.ComponentText, visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AD-значение", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(expectation.DurationText, visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("основная плата наружного блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("проводной контроллер внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("приёмник сигнала внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("плохой контакт", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("интерфейсом основной платы", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("посторонних предметов", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("подключите разъём плотно", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text => Assert.Equal(3, RequiredArray(text, "possibleCauses").Count));

            var technicalTexts = RequiredTexts(entry)
                .Where(text =>
                    !string.Equals(
                        RequiredString(text, "audience"),
                        "Consumer",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.NotEmpty(technicalTexts);
            Assert.All(
                technicalTexts,
                text => Assert.True(
                    RequiredArray(text, "checkSteps").Count >= 3,
                    $"Audience {RequiredString(text, "audience")} for {expectation.Code} must retain the documented troubleshooting steps."));
        }
    }

    [Fact]
    public void Gmv6OutdoorEfProtectionAndSensorBatchContainsManualDiagnosisCausesAndFlowchart()
    {
        foreach (var (fileName, expectation) in Gmv6OutdoorEfProtectionAndSensorBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains("Gree GMV", visible, StringComparison.Ordinal);
            Assert.Contains(expectation.Code, visible, StringComparison.Ordinal);
            Assert.Contains("основная плата наружного блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("проводной контроллер внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("приёмник сигнала внутреннего блока", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text => Assert.Equal(expectation.CauseCount, RequiredArray(text, "possibleCauses").Count));

            var technicalTexts = RequiredTexts(entry)
                .Where(text =>
                    !string.Equals(
                        RequiredString(text, "audience"),
                        "Consumer",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.NotEmpty(technicalTexts);
            Assert.All(
                technicalTexts,
                text => Assert.True(
                    RequiredArray(text, "checkSteps").Count >= 3,
                    $"Audience {RequiredString(text, "audience")} for {expectation.Code} must retain the documented troubleshooting steps."));
        }
    }

    [Fact]
    public void Gmv6OutdoorJProtectionBatchContainsManualDiagnosisCausesAndFlowchart()
    {
        foreach (var (fileName, expectation) in Gmv6OutdoorJProtectionBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains($"Gree GMV6 — {expectation.Code} —", visible, StringComparison.Ordinal);
            Assert.Contains(expectation.Code, visible, StringComparison.Ordinal);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text => Assert.Equal(expectation.CauseCount, RequiredArray(text, "possibleCauses").Count));

            var technicalTexts = RequiredTexts(entry)
                .Where(text =>
                    !string.Equals(
                        RequiredString(text, "audience"),
                        "Consumer",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.NotEmpty(technicalTexts);
            Assert.All(
                technicalTexts,
                text => Assert.True(
                    RequiredArray(text, "checkSteps").Count >= 3,
                    $"Audience {RequiredString(text, "audience")} for {expectation.Code} must retain the documented troubleshooting steps."));
        }
    }

    [Fact]
    public void Gmv6OutdoorCompressorDrivePBatchContainsManualDiagnosisCausesAndFlowchart()
    {
        foreach (var (fileName, expectation) in Gmv6OutdoorCompressorDrivePBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains("Gree GMV", visible, StringComparison.Ordinal);
            Assert.Contains(expectation.Code, visible, StringComparison.Ordinal);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text => Assert.Equal(expectation.CauseCount, RequiredArray(text, "possibleCauses").Count));

            var technicalTexts = RequiredTexts(entry)
                .Where(text =>
                    !string.Equals(
                        RequiredString(text, "audience"),
                        "Consumer",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.NotEmpty(technicalTexts);
            Assert.All(
                technicalTexts,
                text => Assert.True(
                    RequiredArray(text, "checkSteps").Count >= 3,
                    $"Audience {RequiredString(text, "audience")} for {expectation.Code} must retain the documented troubleshooting steps."));
        }
    }

    [Fact]
    public void Gmv6OutdoorTableOnlyBefBatchUsesOnlySafeMeaningWithoutInventedCauses()
    {
        foreach (var (fileName, expectation) in Gmv6OutdoorTableOnlyBefBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains("Gree GMV", visible, StringComparison.Ordinal);
            Assert.Contains(expectation.Code, visible, StringComparison.Ordinal);
            Assert.Contains($"Код {expectation.Code} означает", visible, StringComparison.Ordinal);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("замен", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("flowchart", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("раздел 2.", visible, StringComparison.OrdinalIgnoreCase);

            Assert.All(
                RequiredTexts(entry),
                text =>
                {
                    var textVisible = string.Join(
                        "\n",
                        TelegramVisibleFields
                            .Select(field => text[field])
                            .Where(node => node is not null)
                            .SelectMany(node => node is JsonArray array
                                ? array.Select(item => item!.GetValue<string>())
                                : [node!.GetValue<string>()]));

                    Assert.Empty(RequiredArray(text, "possibleCauses"));
                    Assert.Equal(3, RequiredArray(text, "checkSteps").Count);
                    Assert.Contains("Зафиксируйте код", textVisible, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("квалифицированному специалисту", textVisible, StringComparison.OrdinalIgnoreCase);
                });
        }
    }

    [Fact]
    public void Gmv6OutdoorTableOnlyGBatchUsesOnlySafeMeaningWithoutInventedCauses()
    {
        foreach (var (fileName, expectation) in Gmv6OutdoorTableOnlyGBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains("Gree GMV6", visible, StringComparison.Ordinal);
            Assert.Contains(expectation.Code, visible, StringComparison.Ordinal);
            Assert.Contains($"Код {expectation.Code} означает", visible, StringComparison.Ordinal);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("замен", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("flowchart", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("раздел 2.", visible, StringComparison.OrdinalIgnoreCase);

            Assert.All(
                RequiredTexts(entry),
                text =>
                {
                    var textVisible = string.Join(
                        "\n",
                        TelegramVisibleFields
                            .Select(field => text[field])
                            .Where(node => node is not null)
                            .SelectMany(node => node is JsonArray array
                                ? array.Select(item => item!.GetValue<string>())
                                : [node!.GetValue<string>()]));

                    Assert.Empty(RequiredArray(text, "possibleCauses"));
                    Assert.Equal(3, RequiredArray(text, "checkSteps").Count);
                    Assert.Contains("Зафиксируйте код", textVisible, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("квалифицированному специалисту", textVisible, StringComparison.OrdinalIgnoreCase);
                });
        }
    }

    [Fact]
    public void Gmv6OutdoorTableOnlyHjpBatchUsesOnlySafeMeaningWithoutInventedCauses()
    {
        foreach (var (fileName, expectation) in Gmv6OutdoorTableOnlyHjpBatch)
        {
            var entry = ReadEntry("gmv6", "outdoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains("Gree GMV6", visible, StringComparison.Ordinal);
            Assert.Contains(expectation.Code, visible, StringComparison.Ordinal);
            Assert.Contains($"Код {expectation.Code} означает", visible, StringComparison.Ordinal);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("замен", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("flowchart", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("раздел 2.", visible, StringComparison.OrdinalIgnoreCase);

            Assert.All(
                RequiredTexts(entry),
                text =>
                {
                    var textVisible = string.Join(
                        "\n",
                        TelegramVisibleFields
                            .Select(field => text[field])
                            .Where(node => node is not null)
                            .SelectMany(node => node is JsonArray array
                                ? array.Select(item => item!.GetValue<string>())
                                : [node!.GetValue<string>()]));

                    Assert.Empty(RequiredArray(text, "possibleCauses"));
                    Assert.Equal(3, RequiredArray(text, "checkSteps").Count);
                    Assert.Contains("Зафиксируйте код", textVisible, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("квалифицированному специалисту", textVisible, StringComparison.OrdinalIgnoreCase);
                });
        }
    }

    [Fact]
    public void Gmv6IndoorDetailedD10aBatchContainsManualDiagnosisCausesAndIndoorFlow()
    {
        foreach (var (fileName, expectation) in Gmv6IndoorDetailedD10aBatch)
        {
            var entry = ReadEntry("gmv6", "indoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Contains($"Gree GMV6 — {expectation.Code} —", visible, StringComparison.Ordinal);
            Assert.Contains("внутренн", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("наружного блока и электрического отсека", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("2-разрядном LED", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("краткое значение кода", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("пошаговая процедура в эту запись не перенесена", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("раздел 2.", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text =>
                {
                    Assert.Equal(expectation.CauseCount, RequiredArray(text, "possibleCauses").Count);
                    Assert.NotEmpty(RequiredArray(text, "checkSteps"));
                    Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "recommendedAction")));
                    Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "safetyNote")));
                });
        }
    }

    [Fact]
    public void Gmv6IndoorDetailedL10bBatchContainsManualDiagnosisCausesAndIndoorFlow()
    {
        foreach (var (fileName, expectation) in Gmv6IndoorDetailedL10bBatch)
        {
            var entry = ReadEntry("gmv6", "indoor", fileName);
            var visible = VisibleBlob(entry);

            var expectedTitlePrefix = expectation.Code == "L1"
                ? "Gree GMV6 — L1 —"
                : $"Gree GMV6 — {expectation.Code} —";

            Assert.Contains(expectedTitlePrefix, visible, StringComparison.Ordinal);
            Assert.Contains("внутренн", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("2-разрядном LED", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("краткое значение кода", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("пошаговая процедура в эту запись не перенесена", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("раздел 2.", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var phrase in expectation.RequiredPhrases)
            {
                Assert.Contains(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text =>
                {
                    Assert.Equal(expectation.CauseCount, RequiredArray(text, "possibleCauses").Count);
                    Assert.NotEmpty(RequiredArray(text, "checkSteps"));
                    Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "recommendedAction")));
                    Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "safetyNote")));
                });
        }
    }

    [Fact]
    public void Gmv6IndoorTableOnlyD11aBatchUsesExactMeaningAndSafeHandoff()
    {
        foreach (var (fileName, expectation) in Gmv6IndoorTableOnlyD11aBatch)
        {
            var entry = ReadEntry("gmv6", "indoor", fileName);
            var visible = VisibleBlob(entry);

            Assert.Equal(expectation.Code, RequiredString(entry, "code"));
            Assert.Contains(expectation.Meaning, visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("краткое значение кода", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("пошаговая процедура", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(phrase, visible, StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text =>
                {
                    Assert.StartsWith(
                        $"Gree GMV6 — {expectation.Code} —",
                        RequiredString(text, "title"),
                        StringComparison.Ordinal);
                    Assert.Empty(RequiredArray(text, "possibleCauses"));
                    Assert.Equal(3, RequiredArray(text, "checkSteps").Count);
                    Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "recommendedAction")));
                    Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "safetyNote")));
                });
        }
    }

    [Fact]
    public void Gmv6OutdoorManualBoundVisibleScopeIsClosed()
    {
        var outdoorRoot = Path.Combine(GreeRoot, "gmv6", "outdoor");
        var outdoorEntries = Directory
            .EnumerateFiles(outdoorRoot, "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .Select(path => (Path: path, Entry: ReadObject(path)))
            .ToArray();

        Assert.Equal(121, outdoorEntries.Length);

        foreach (var (path, entry) in outdoorEntries)
        {
            var visible = VisibleBlob(entry);

            Assert.False(string.IsNullOrWhiteSpace(visible), $"Visible text is empty: {path}");
            Assert.DoesNotContain("краткое значение кода", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("пошаговая процедура в эту запись не перенесена", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("раздел 2.", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var phrase in ForbiddenVisiblePhrases)
            {
                Assert.DoesNotContain(
                    phrase,
                    visible,
                    StringComparison.OrdinalIgnoreCase);
            }

            Assert.All(
                RequiredTexts(entry),
                text =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "title")), $"Title is empty: {path}");
                    Assert.False(string.IsNullOrWhiteSpace(RequiredString(text, "summary")), $"Summary is empty: {path}");
                    Assert.True(RequiredArray(text, "checkSteps").Count > 0, $"Check steps are empty: {path}");
                    Assert.False(
                        string.IsNullOrWhiteSpace(RequiredString(text, "recommendedAction")),
                        $"Recommended action is empty: {path}");
                    Assert.False(
                        string.IsNullOrWhiteSpace(RequiredString(text, "safetyNote")),
                        $"Safety note is empty: {path}");
                });
        }
    }

    [Fact]
    public void Gmv6VisibleTitlesAndSourceNotesAreUserSafe()
    {
        var gmv6Root = Path.Combine(GreeRoot, "gmv6");
        var entries = Directory
            .EnumerateFiles(gmv6Root, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => (Path: path, Entry: ReadObject(path)))
            .ToArray();

        Assert.Equal(263, entries.Length);

        foreach (var (path, entry) in entries)
        {
            foreach (var text in RequiredTexts(entry))
            {
                var title = RequiredString(text, "title");
                var sourceNote = RequiredString(text, "sourceNote");

                Assert.StartsWith("Gree GMV6", title, StringComparison.Ordinal);
                Assert.DoesNotContain("Gree GMV ", title, StringComparison.Ordinal);

                foreach (var phrase in ForbiddenVisiblePhrases)
                {
                    Assert.DoesNotContain(phrase, sourceNote, StringComparison.OrdinalIgnoreCase);
                }

                Assert.DoesNotContain("по таблице", sourceNote, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("классифицирован", sourceNote, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(
                    "Код передан без предположений",
                    sourceNote,
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(
                    "без пошаговой диагностики",
                    sourceNote,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void GreeRuntimeAndSeriesCountsRemainStable()
    {
        var entries = ReadEntries().Select(item => item.Entry).ToArray();
        var expectedSeriesCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["GMV6 HR"] = 262,
            ["GMV6"] = 263,
            ["GMV Mini"] = 136,
            ["GMV X"] = 263,
            ["GMV9 Flex"] = 260,
            ["U-Match R32"] = 107,
            ["ERV B Series"] = 5
        };

        Assert.Equal(1296, entries.Length);
        foreach (var (series, expectedCount) in expectedSeriesCounts)
        {
            Assert.Equal(
                expectedCount,
                entries.Count(entry =>
                    string.Equals(
                        RequiredString(entry, "series"),
                        series,
                        StringComparison.Ordinal)));
        }
    }

    private static IEnumerable<(string Path, JsonObject Entry)> ReadEntries() =>
        Directory
            .EnumerateFiles(GreeRoot, "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => (path, ReadObject(path)));

    private static JsonObject ReadEntry(params string[] pathSegments) =>
        ReadObject(Path.Combine(new[] { GreeRoot }.Concat(pathSegments).ToArray()));

    private static JsonObject ReadObject(string path)
    {
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);
        return Assert.IsType<JsonObject>(node);
    }

    private static JsonObject[] RequiredTexts(JsonObject entry) =>
        RequiredArray(entry, "texts")
            .Select(node => Assert.IsType<JsonObject>(node))
            .ToArray();

    private static string VisibleBlob(JsonObject entry)
    {
        var values = new List<string>();
        foreach (var text in RequiredTexts(entry))
        {
            foreach (var field in TelegramVisibleFields)
            {
                var node = text[field];
                switch (node)
                {
                    case JsonValue value:
                        values.Add(value.GetValue<string>());
                        break;
                    case JsonArray array:
                        values.AddRange(array.Select(item => item!.GetValue<string>()));
                        break;
                }
            }
        }

        return string.Join("\n", values);
    }

    private static JsonArray RequiredArray(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return Assert.IsType<JsonArray>(node);
    }

    private static string RequiredString(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return node.GetValue<string>();
    }
}
