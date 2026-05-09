using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public sealed record WeatherSolarTraceSource(
    Iso52016WeatherSolarContext Context,
    string WeatherSource,
    IReadOnlyList<string>? Assumptions = null);

public sealed record ThermalTopologyTraceSource(
    BuildingThermalTopology Topology,
    string AdjacentBoundaryPolicy,
    IReadOnlyList<string>? Assumptions = null);

public sealed record Iso52016MultiZoneTraceSource(
    MultiZoneCalculationResult Result,
    string CalculationMode,
    int TimeStepCount,
    IReadOnlyList<string>? Assumptions = null);

public sealed record NaturalVentilationTraceSource(
    Iso16798NaturalVentilationResult Result,
    int OpeningCount,
    string ControlMode,
    IReadOnlyList<string>? Assumptions = null);

public sealed record GroundTraceSource(
    GroundHeatTransferResult Result,
    int GroundBoundaryCount,
    string ProfileMode,
    string HeatTransferConvention);

public sealed record DomesticHotWaterTraceSource(
    DomesticHotWaterSystemLoadResult Result,
    string DemandBasis,
    string DrawOffProfileMode);

public sealed record SystemEnergyTraceSource(
    SystemEnergyCalculationResult Result,
    IReadOnlyList<string> IntakeUses,
    string StageChain,
    string OwnershipDecision);
