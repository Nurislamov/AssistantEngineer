using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterReferenceDataProvider
{
    private static readonly DomesticHotWaterTemperatureAssumptions DefaultTemperatureAssumptions =
        new(
            DefaultHotWaterTemperatureC: 55.0,
            DefaultColdWaterTemperatureC: 10.0,
            Notes: "Internal analytical anchor assumptions for EN12831-3-style standard-based calculation.");

    private static readonly double[] ResidentialWeekday =
    [
        0.01, 0.005, 0.005, 0.005, 0.01, 0.05,
        0.12, 0.11, 0.06, 0.03, 0.02, 0.025,
        0.03, 0.025, 0.02, 0.025, 0.04, 0.08,
        0.12, 0.11, 0.07, 0.035, 0.02, 0.01
    ];

    private static readonly double[] ResidentialWeekend =
    [
        0.01, 0.005, 0.005, 0.005, 0.01, 0.025,
        0.06, 0.09, 0.1, 0.075, 0.045, 0.035,
        0.035, 0.03, 0.03, 0.035, 0.045, 0.07,
        0.11, 0.115, 0.075, 0.045, 0.025, 0.015
    ];

    private static readonly double[] OfficeDaytime =
    [
        0.00, 0.00, 0.00, 0.00, 0.005, 0.02,
        0.06, 0.09, 0.10, 0.11, 0.11, 0.10,
        0.10, 0.10, 0.09, 0.07, 0.05, 0.03,
        0.015, 0.01, 0.005, 0.005, 0.005, 0.005
    ];

    private static readonly double[] HotelMorningEvening =
    [
        0.02, 0.015, 0.01, 0.01, 0.02, 0.06,
        0.10, 0.12, 0.08, 0.04, 0.03, 0.025,
        0.02, 0.02, 0.02, 0.03, 0.05, 0.08,
        0.10, 0.09, 0.07, 0.05, 0.03, 0.02
    ];

    private static readonly double[] SchoolDaytime =
    [
        0.00, 0.00, 0.00, 0.00, 0.005, 0.015,
        0.05, 0.09, 0.11, 0.12, 0.11, 0.10,
        0.10, 0.09, 0.08, 0.06, 0.03, 0.015,
        0.005, 0.005, 0.005, 0.005, 0.005, 0.005
    ];

    private static readonly double[] HealthcareShiftPattern =
    [
        0.03, 0.025, 0.02, 0.02, 0.025, 0.04,
        0.055, 0.06, 0.055, 0.05, 0.045, 0.04,
        0.04, 0.04, 0.04, 0.045, 0.05, 0.055,
        0.06, 0.06, 0.055, 0.045, 0.035, 0.03
    ];

    private static readonly double[] Flat =
    [
        1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
        1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
        1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
        1.0, 1.0, 1.0, 1.0, 1.0, 1.0
    ];

    private static readonly IReadOnlyDictionary<DomesticHotWaterUsageCategory, DomesticHotWaterReferenceTableEntry> ReferenceTable =
        new Dictionary<DomesticHotWaterUsageCategory, DomesticHotWaterReferenceTableEntry>
        {
            [DomesticHotWaterUsageCategory.ResidentialDwelling] = new(
                EntryId: "dhw-en12831-style-residential",
                UsageCategory: DomesticHotWaterUsageCategory.ResidentialDwelling,
                LitersPerPersonDay: 48.0,
                LitersPerM2Day: 1.1,
                LitersPerUnitDay: 150.0,
                EquivalentOccupantFactor: 1.0),
            [DomesticHotWaterUsageCategory.Office] = new(
                EntryId: "dhw-en12831-style-office",
                UsageCategory: DomesticHotWaterUsageCategory.Office,
                LitersPerPersonDay: 14.0,
                LitersPerM2Day: 0.35,
                LitersPerUnitDay: 190.0,
                EquivalentOccupantFactor: 0.8),
            [DomesticHotWaterUsageCategory.School] = new(
                EntryId: "dhw-en12831-style-school",
                UsageCategory: DomesticHotWaterUsageCategory.School,
                LitersPerPersonDay: 11.0,
                LitersPerM2Day: 0.28,
                LitersPerUnitDay: 130.0,
                EquivalentOccupantFactor: 0.9),
            [DomesticHotWaterUsageCategory.Hotel] = new(
                EntryId: "dhw-en12831-style-hotel",
                UsageCategory: DomesticHotWaterUsageCategory.Hotel,
                LitersPerPersonDay: 68.0,
                LitersPerM2Day: 1.45,
                LitersPerUnitDay: 235.0,
                EquivalentOccupantFactor: 1.1),
            [DomesticHotWaterUsageCategory.Healthcare] = new(
                EntryId: "dhw-en12831-style-healthcare",
                UsageCategory: DomesticHotWaterUsageCategory.Healthcare,
                LitersPerPersonDay: 85.0,
                LitersPerM2Day: 1.2,
                LitersPerUnitDay: 280.0,
                EquivalentOccupantFactor: 1.2),
            [DomesticHotWaterUsageCategory.GenericFallback] = new(
                EntryId: "dhw-en12831-style-generic",
                UsageCategory: DomesticHotWaterUsageCategory.GenericFallback,
                LitersPerPersonDay: 40.0,
                LitersPerM2Day: 0.6,
                LitersPerUnitDay: 180.0,
                EquivalentOccupantFactor: 1.0)
        };

    public Iso12831DomesticHotWaterReferenceDefaults Resolve(
        Iso12831DomesticHotWaterUsageCategory usageCategory) =>
        Resolve(usageCategory, useTableDrivenReferenceData: false, tableDrivenUsageCategory: null).ReferenceDefaults;

    public Iso12831DomesticHotWaterResolvedReference Resolve(
        Iso12831DomesticHotWaterUsageCategory usageCategory,
        bool useTableDrivenReferenceData,
        DomesticHotWaterUsageCategory? tableDrivenUsageCategory)
    {
        if (!useTableDrivenReferenceData)
        {
            return new Iso12831DomesticHotWaterResolvedReference(
                ResolveBaselineDefaults(usageCategory),
                usageCategory.ToString(),
                DefaultTemperatureAssumptions,
                UsageProfileSet: null);
        }

        var resolvedCategory = tableDrivenUsageCategory ?? MapToTableUsageCategory(usageCategory);
        var usageProfileSet = ResolveUsageProfileSet(resolvedCategory);
        var entry = usageProfileSet.ReferenceTableEntry;

        return new Iso12831DomesticHotWaterResolvedReference(
            new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: entry.LitersPerPersonDay,
                LitersPerM2Day: entry.LitersPerM2Day,
                LitersPerUnitDay: entry.LitersPerUnitDay,
                EquivalentOccupantFactor: entry.EquivalentOccupantFactor,
                DrawProfileKind: usageProfileSet.DrawProfileTable.DrawProfileKind),
            usageProfileSet.ReferenceTableEntry.EntryId,
            usageProfileSet.TemperatureAssumptions,
            usageProfileSet);
    }

    public DomesticHotWaterUsageProfileSet ResolveUsageProfileSet(
        DomesticHotWaterUsageCategory usageCategory)
    {
        var normalizedCategory = ReferenceTable.ContainsKey(usageCategory)
            ? usageCategory
            : DomesticHotWaterUsageCategory.GenericFallback;
        var referenceEntry = ReferenceTable[normalizedCategory];
        var drawProfileTable = BuildDrawProfileTable(normalizedCategory);

        return new DomesticHotWaterUsageProfileSet(
            ReferenceTableEntry: referenceEntry,
            DrawProfileTable: drawProfileTable,
            TemperatureAssumptions: DefaultTemperatureAssumptions);
    }

    private static Iso12831DomesticHotWaterReferenceDefaults ResolveBaselineDefaults(
        Iso12831DomesticHotWaterUsageCategory usageCategory) =>
        usageCategory switch
        {
            Iso12831DomesticHotWaterUsageCategory.ResidentialApartment => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 45.0,
                LitersPerM2Day: 1.2,
                LitersPerUnitDay: 120.0,
                EquivalentOccupantFactor: 1.0,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend),
            Iso12831DomesticHotWaterUsageCategory.SingleFamilyHouse => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 50.0,
                LitersPerM2Day: 1.0,
                LitersPerUnitDay: 180.0,
                EquivalentOccupantFactor: 1.0,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend),
            Iso12831DomesticHotWaterUsageCategory.Office => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 15.0,
                LitersPerM2Day: 0.35,
                LitersPerUnitDay: 200.0,
                EquivalentOccupantFactor: 0.8,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.OfficeDaytime),
            Iso12831DomesticHotWaterUsageCategory.Hotel => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 65.0,
                LitersPerM2Day: 1.4,
                LitersPerUnitDay: 220.0,
                EquivalentOccupantFactor: 1.1,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.HotelMorningEvening),
            Iso12831DomesticHotWaterUsageCategory.School => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 12.0,
                LitersPerM2Day: 0.3,
                LitersPerUnitDay: 140.0,
                EquivalentOccupantFactor: 0.9,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.SchoolDaytime),
            Iso12831DomesticHotWaterUsageCategory.Healthcare => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 80.0,
                LitersPerM2Day: 1.1,
                LitersPerUnitDay: 260.0,
                EquivalentOccupantFactor: 1.2,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat),
            Iso12831DomesticHotWaterUsageCategory.Restaurant => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 25.0,
                LitersPerM2Day: 0.8,
                LitersPerUnitDay: 320.0,
                EquivalentOccupantFactor: 1.0,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat),
            _ => new Iso12831DomesticHotWaterReferenceDefaults(
                LitersPerPersonDay: 40.0,
                LitersPerM2Day: 0.6,
                LitersPerUnitDay: 180.0,
                EquivalentOccupantFactor: 1.0,
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat)
        };

    private static DomesticHotWaterUsageCategory MapToTableUsageCategory(
        Iso12831DomesticHotWaterUsageCategory usageCategory) =>
        usageCategory switch
        {
            Iso12831DomesticHotWaterUsageCategory.ResidentialApartment => DomesticHotWaterUsageCategory.ResidentialDwelling,
            Iso12831DomesticHotWaterUsageCategory.SingleFamilyHouse => DomesticHotWaterUsageCategory.ResidentialDwelling,
            Iso12831DomesticHotWaterUsageCategory.Office => DomesticHotWaterUsageCategory.Office,
            Iso12831DomesticHotWaterUsageCategory.Hotel => DomesticHotWaterUsageCategory.Hotel,
            Iso12831DomesticHotWaterUsageCategory.School => DomesticHotWaterUsageCategory.School,
            Iso12831DomesticHotWaterUsageCategory.Healthcare => DomesticHotWaterUsageCategory.Healthcare,
            _ => DomesticHotWaterUsageCategory.GenericFallback
        };

    private static DomesticHotWaterDrawProfileTable BuildDrawProfileTable(
        DomesticHotWaterUsageCategory category) =>
        category switch
        {
            DomesticHotWaterUsageCategory.ResidentialDwelling => new DomesticHotWaterDrawProfileTable(
                ProfileId: "residential-weekday-weekend",
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend,
                WeekdayWeights24: ResidentialWeekday,
                WeekendWeights24: ResidentialWeekend),
            DomesticHotWaterUsageCategory.Office => new DomesticHotWaterDrawProfileTable(
                ProfileId: "office-daytime",
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.OfficeDaytime,
                WeekdayWeights24: OfficeDaytime,
                WeekendWeights24: OfficeDaytime),
            DomesticHotWaterUsageCategory.School => new DomesticHotWaterDrawProfileTable(
                ProfileId: "school-daytime",
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.SchoolDaytime,
                WeekdayWeights24: SchoolDaytime,
                WeekendWeights24: SchoolDaytime),
            DomesticHotWaterUsageCategory.Hotel => new DomesticHotWaterDrawProfileTable(
                ProfileId: "hotel-morning-evening",
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.HotelMorningEvening,
                WeekdayWeights24: HotelMorningEvening,
                WeekendWeights24: HotelMorningEvening),
            DomesticHotWaterUsageCategory.Healthcare => new DomesticHotWaterDrawProfileTable(
                ProfileId: "healthcare-shift-pattern",
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat,
                WeekdayWeights24: HealthcareShiftPattern,
                WeekendWeights24: HealthcareShiftPattern),
            _ => new DomesticHotWaterDrawProfileTable(
                ProfileId: "generic-flat",
                DrawProfileKind: Iso12831DomesticHotWaterDrawProfileKind.Flat,
                WeekdayWeights24: Flat,
                WeekendWeights24: Flat)
        };
}

public sealed record Iso12831DomesticHotWaterReferenceDefaults(
    double LitersPerPersonDay,
    double LitersPerM2Day,
    double LitersPerUnitDay,
    double EquivalentOccupantFactor,
    Iso12831DomesticHotWaterDrawProfileKind DrawProfileKind);

public sealed record Iso12831DomesticHotWaterResolvedReference(
    Iso12831DomesticHotWaterReferenceDefaults ReferenceDefaults,
    string ReferenceEntryId,
    DomesticHotWaterTemperatureAssumptions TemperatureAssumptions,
    DomesticHotWaterUsageProfileSet? UsageProfileSet);
