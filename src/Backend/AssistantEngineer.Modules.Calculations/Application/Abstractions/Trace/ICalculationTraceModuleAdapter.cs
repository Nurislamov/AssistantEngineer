using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;

public interface ICalculationTraceModuleAdapter
{
    CalculationTraceDocument BuildWeatherSolarTrace(
        WeatherSolarTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard);

    CalculationTraceDocument BuildThermalTopologyTrace(
        ThermalTopologyTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard);

    CalculationTraceDocument BuildIso52016MultiZoneTrace(
        Iso52016MultiZoneTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard);

    CalculationTraceDocument BuildNaturalVentilationTrace(
        NaturalVentilationTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard);

    CalculationTraceDocument BuildGroundTrace(
        GroundTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard);

    CalculationTraceDocument BuildDomesticHotWaterTrace(
        DomesticHotWaterTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard);

    CalculationTraceDocument BuildSystemEnergyTrace(
        SystemEnergyTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard);

    CalculationTraceDocument Merge(
        string traceId,
        string calculationType,
        CalculationTraceModuleKind rootModule,
        IReadOnlyList<CalculationTraceDocument> traces,
        string? calculationId = null,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard);
}
