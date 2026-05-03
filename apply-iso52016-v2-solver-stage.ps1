param(
    [string] $RepoRoot = (Get-Location).Path,
    [switch] $RunTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Stage2File {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [Parameter(Mandatory = $true)] [string] $Content
    )

    $fullPath = Join-Path $RepoRoot $RelativePath
    $directory = Split-Path -Parent $fullPath

    if (-not [System.IO.Directory]::Exists($directory)) {
        [System.IO.Directory]::CreateDirectory($directory) | Out-Null
    }

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($fullPath, $Content, $utf8NoBom)
    Write-Host "Wrote $RelativePath"
}

function Patch-Iso52016Registration {
    $relativePath = 'src\Backend\AssistantEngineer.Modules.Calculations\Composition\Iso52016Registration.cs'
    $path = Join-Path $RepoRoot $relativePath

    if (-not [System.IO.File]::Exists($path)) {
        throw "Cannot find $relativePath. Run this script from repository root or pass -RepoRoot."
    }

    $text = [System.IO.File]::ReadAllText($path)

    if ($text.Contains('IIso52016V2HourlySolver')) {
        Write-Host 'Iso52016Registration.cs already contains ISO52016 V2 registrations. Skipping patch.'
        return
    }

    $needle = '        services.AddScoped<IBuildingEnergyCalculator, Iso52016BuildingEnergyCalculator>();'
    if (-not $text.Contains($needle)) {
        throw "Cannot patch Iso52016Registration.cs because expected registration anchor was not found."
    }

    $block = @'
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2.IIso52016V2HourlySolver, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2.Iso52016V2HourlySolver>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2.IIso52016InternalGainReferenceDataProvider, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2.Iso52016InternalGainReferenceDataProvider>();
        services.AddScoped<AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2.IIso52016AdjacentUnconditionedZoneTemperatureSolver, AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2.Iso52016AdjacentUnconditionedZoneTemperatureSolver>();
'@

    $replacement = $block + "`r`n" + $needle
    $text = $text.Replace($needle, $replacement)

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
    Write-Host 'Patched Iso52016Registration.cs with ISO52016 V2 registrations.'
}

$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
if (-not [System.IO.Directory]::Exists($RepoRoot)) {
    throw "RepoRoot does not exist: $RepoRoot"
}

Write-Host "Applying ISO52016 V2 solver stage into $RepoRoot"

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2NodeDefinition.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// Thermal node used by the ISO 52016 V2 implicit hourly matrix solver.
/// A node can represent zone air, an internal mass node, a surface layer node, or a reduced equivalent node.
/// </summary>
public sealed record Iso52016V2NodeDefinition(
    string NodeId,
    double HeatCapacityJPerK,
    double InitialTemperatureC,
    bool IsAirNode = false);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2ConductanceLink.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// Conductive/convective coupling between two thermal nodes.
/// </summary>
public sealed record Iso52016V2ConductanceLink(
    string FromNodeId,
    string ToNodeId,
    double ConductanceWPerK);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2BoundaryConductance.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// Coupling between a thermal node and a named boundary condition, such as outdoor, ground, adjacent zone or supply air.
/// </summary>
public sealed record Iso52016V2BoundaryConductance(
    string NodeId,
    string BoundaryId,
    double ConductanceWPerK);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2HourlyInputRecord.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// One hourly ISO 52016 V2 solver input row.
/// Boundary temperatures are keyed by BoundaryId from Iso52016V2BoundaryConductance.
/// Node gains are keyed by NodeId and may include solar, internal gains and other sensible heat sources.
/// </summary>
public sealed record Iso52016V2HourlyInputRecord(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    IReadOnlyDictionary<string, double> BoundaryTemperaturesC,
    IReadOnlyDictionary<string, double> NodeHeatGainsW,
    double? HeatingSetpointC = null,
    double? CoolingSetpointC = null);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2HourlySolverOptions.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2HourlySolverOptions(
    double TimeStepSeconds = 3600.0,
    string AirNodeId = "air",
    double DefaultHeatingSetpointC = 20.0,
    double DefaultCoolingSetpointC = 26.0);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2HourlySolverRequest.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2HourlySolverRequest(
    string ZoneCode,
    IReadOnlyList<Iso52016V2NodeDefinition> Nodes,
    IReadOnlyList<Iso52016V2ConductanceLink> InternalConductances,
    IReadOnlyList<Iso52016V2BoundaryConductance> BoundaryConductances,
    IReadOnlyList<Iso52016V2HourlyInputRecord> Hours,
    Iso52016V2HourlySolverOptions? Options = null);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2HourlyNodeState.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2HourlyNodeState(
    string NodeId,
    double TemperatureBeforeHvacC,
    double TemperatureAfterHvacC,
    double HeatGainW);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2HourlyResult.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2HourlyResult(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    double HeatingSetpointC,
    double CoolingSetpointC,
    double AirTemperatureBeforeHvacC,
    double AirTemperatureAfterHvacC,
    double HeatingLoadW,
    double CoolingLoadW,
    double TimeStepSeconds,
    IReadOnlyList<Iso52016V2HourlyNodeState> NodeStates)
{
    public double HeatingEnergyKWh => HeatingLoadW * TimeStepSeconds / 3_600_000.0;

    public double CoolingEnergyKWh => CoolingLoadW * TimeStepSeconds / 3_600_000.0;

    public double TotalNodeHeatGainsW => NodeStates.Sum(node => node.HeatGainW);

    public double TotalNodeHeatGainsKWh => TotalNodeHeatGainsW * TimeStepSeconds / 3_600_000.0;

    public double GetNodeTemperatureAfterHvacC(string nodeId) =>
        NodeStates.First(node => string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase)).TemperatureAfterHvacC;

    public double GetNodeTemperatureBeforeHvacC(string nodeId) =>
        NodeStates.First(node => string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase)).TemperatureBeforeHvacC;
}
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2MonthlySummary.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2MonthlySummary(
    int Month,
    double HeatingEnergyKWh,
    double CoolingEnergyKWh,
    double TotalNodeHeatGainsKWh,
    double PeakHeatingLoadW,
    double PeakCoolingLoadW,
    double AverageAirTemperatureAfterHvacC);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016V2HourlySolverProfile.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2HourlySolverProfile(
    string ZoneCode,
    Iso52016V2HourlySolverOptions Options,
    IReadOnlyList<Iso52016V2HourlyResult> Hours,
    IReadOnlyList<Iso52016V2MonthlySummary> MonthlySummaries)
{
    public int HourCount => Hours.Count;

    public double AnnualHeatingEnergyKWh => Hours.Sum(hour => hour.HeatingEnergyKWh);

    public double AnnualCoolingEnergyKWh => Hours.Sum(hour => hour.CoolingEnergyKWh);

    public double AnnualTotalNodeHeatGainsKWh => Hours.Sum(hour => hour.TotalNodeHeatGainsKWh);

    public double PeakHeatingLoadW => Hours.Count == 0 ? 0.0 : Hours.Max(hour => hour.HeatingLoadW);

    public double PeakCoolingLoadW => Hours.Count == 0 ? 0.0 : Hours.Max(hour => hour.CoolingLoadW);
}
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016InternalGainReferenceData.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// Seed reference data for sensible internal gains. Values are intentionally explicit and replaceable by project data,
/// national annex values or a later ISO 16798/52016 profile table import.
/// </summary>
public sealed record Iso52016InternalGainReferenceData(
    string UseType,
    double OccupantDensityPersonPerM2,
    double SensibleHeatPerPersonW,
    double LightingPowerDensityWPerM2,
    double EquipmentPowerDensityWPerM2,
    double ConvectiveFraction,
    double RadiativeFraction,
    string SourceNote);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016InternalGainCalculationResult.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016InternalGainCalculationResult(
    string UseType,
    double FloorAreaM2,
    double OccupantGainW,
    double LightingGainW,
    double EquipmentGainW,
    double TotalSensibleGainW,
    double ConvectiveGainW,
    double RadiativeGainW);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016AdjacentUnconditionedZoneTemperatureRequest.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016AdjacentUnconditionedZoneTemperatureRequest(
    double OutdoorTemperatureC,
    double AdjacentZonePreviousTemperatureC,
    double ConditionedZoneTemperatureC,
    double HeatTransferToOutdoorWPerK,
    double HeatTransferToGroundWPerK,
    double GroundTemperatureC,
    double HeatTransferToConditionedZoneWPerK,
    double InternalGainsW,
    double SolarGainsW,
    double ThermalCapacityJPerK,
    double TimeStepSeconds = 3600.0);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Contracts\Iso52016\V2\Iso52016AdjacentUnconditionedZoneTemperatureResult.cs' -Content @'
namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016AdjacentUnconditionedZoneTemperatureResult(
    double TemperatureC,
    double HeatFlowToConditionedZoneW,
    double TotalBoundaryConductanceWPerK,
    double TotalGainsW);
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Abstractions\Iso52016\V2\IIso52016V2HourlySolver.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;

public interface IIso52016V2HourlySolver
{
    Result<Iso52016V2HourlySolverProfile> Solve(
        Iso52016V2HourlySolverRequest request);
}
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Abstractions\Iso52016\V2\IIso52016InternalGainReferenceDataProvider.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;

public interface IIso52016InternalGainReferenceDataProvider
{
    IReadOnlyList<Iso52016InternalGainReferenceData> GetAll();

    Result<Iso52016InternalGainReferenceData> GetByUseType(
        string useType);

    Result<Iso52016InternalGainCalculationResult> CalculatePeakSensibleGain(
        string useType,
        double floorAreaM2,
        double occupancyFactor = 1.0,
        double lightingFactor = 1.0,
        double equipmentFactor = 1.0);
}
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Abstractions\Iso52016\V2\IIso52016AdjacentUnconditionedZoneTemperatureSolver.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;

public interface IIso52016AdjacentUnconditionedZoneTemperatureSolver
{
    Result<Iso52016AdjacentUnconditionedZoneTemperatureResult> Solve(
        Iso52016AdjacentUnconditionedZoneTemperatureRequest request);
}
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\V2\Iso52016V2HourlySolver.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

public sealed class Iso52016V2HourlySolver : IIso52016V2HourlySolver
{
    private const double MinimumPositive = 0.000000001;
    private const double UnitHvacLoadW = 1.0;

    public Result<Iso52016V2HourlySolverProfile> Solve(
        Iso52016V2HourlySolverRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016V2HourlySolverProfile>.Failure(validation);

        var options = request.Options ?? new Iso52016V2HourlySolverOptions();

        try
        {
            var hours = SolveHours(
                request,
                options);

            return Result<Iso52016V2HourlySolverProfile>.Success(
                new Iso52016V2HourlySolverProfile(
                    ZoneCode: request.ZoneCode,
                    Options: options,
                    Hours: hours,
                    MonthlySummaries: BuildMonthlySummaries(hours)));
        }
        catch (InvalidOperationException exception)
        {
            return Result<Iso52016V2HourlySolverProfile>.Failure(exception.Message);
        }
    }

    private static IReadOnlyList<Iso52016V2HourlyResult> SolveHours(
        Iso52016V2HourlySolverRequest request,
        Iso52016V2HourlySolverOptions options)
    {
        var nodeIndex = request.Nodes
            .Select((node, index) => new { node.NodeId, Index = index })
            .ToDictionary(
                node => node.NodeId,
                node => node.Index,
                StringComparer.OrdinalIgnoreCase);

        var previousTemperatures = request.Nodes
            .Select(node => node.InitialTemperatureC)
            .ToArray();

        var results = new List<Iso52016V2HourlyResult>(request.Hours.Count);

        foreach (var hour in request.Hours)
        {
            var result = SolveHour(
                request.Nodes,
                request.InternalConductances,
                request.BoundaryConductances,
                nodeIndex,
                hour,
                previousTemperatures,
                options);

            results.Add(result);

            previousTemperatures = result.NodeStates
                .Select(state => state.TemperatureAfterHvacC)
                .ToArray();
        }

        return results;
    }

    private static Iso52016V2HourlyResult SolveHour(
        IReadOnlyList<Iso52016V2NodeDefinition> nodes,
        IReadOnlyList<Iso52016V2ConductanceLink> internalConductances,
        IReadOnlyList<Iso52016V2BoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016V2HourlyInputRecord hour,
        IReadOnlyList<double> previousTemperaturesC,
        Iso52016V2HourlySolverOptions options)
    {
        var heatingSetpointC = hour.HeatingSetpointC ?? options.DefaultHeatingSetpointC;
        var coolingSetpointC = hour.CoolingSetpointC ?? options.DefaultCoolingSetpointC;
        var airNodeIndex = nodeIndex[options.AirNodeId];

        var freeFloatingTemperatures = SolveTemperatures(
            nodes,
            internalConductances,
            boundaryConductances,
            nodeIndex,
            hour,
            previousTemperaturesC,
            options.TimeStepSeconds,
            hvacLoadW: 0.0,
            hvacNodeIndex: airNodeIndex);

        var freeFloatingAirTemperatureC = freeFloatingTemperatures[airNodeIndex];
        var controlledTemperatures = freeFloatingTemperatures;
        var heatingLoadW = 0.0;
        var coolingLoadW = 0.0;

        if (freeFloatingAirTemperatureC < heatingSetpointC)
        {
            var responsePerW = CalculateAirTemperatureResponsePerW(
                nodes,
                internalConductances,
                boundaryConductances,
                nodeIndex,
                hour,
                previousTemperaturesC,
                options.TimeStepSeconds,
                airNodeIndex,
                freeFloatingAirTemperatureC);

            if (responsePerW <= MinimumPositive)
                throw new InvalidOperationException("ISO 52016 V2 solver cannot control the air node because HVAC response is zero.");

            heatingLoadW = (heatingSetpointC - freeFloatingAirTemperatureC) / responsePerW;

            controlledTemperatures = SolveTemperatures(
                nodes,
                internalConductances,
                boundaryConductances,
                nodeIndex,
                hour,
                previousTemperaturesC,
                options.TimeStepSeconds,
                hvacLoadW: heatingLoadW,
                hvacNodeIndex: airNodeIndex);
        }
        else if (freeFloatingAirTemperatureC > coolingSetpointC)
        {
            var responsePerW = CalculateAirTemperatureResponsePerW(
                nodes,
                internalConductances,
                boundaryConductances,
                nodeIndex,
                hour,
                previousTemperaturesC,
                options.TimeStepSeconds,
                airNodeIndex,
                freeFloatingAirTemperatureC);

            if (responsePerW <= MinimumPositive)
                throw new InvalidOperationException("ISO 52016 V2 solver cannot control the air node because HVAC response is zero.");

            coolingLoadW = (freeFloatingAirTemperatureC - coolingSetpointC) / responsePerW;

            controlledTemperatures = SolveTemperatures(
                nodes,
                internalConductances,
                boundaryConductances,
                nodeIndex,
                hour,
                previousTemperaturesC,
                options.TimeStepSeconds,
                hvacLoadW: -coolingLoadW,
                hvacNodeIndex: airNodeIndex);
        }

        return CreateHourlyResult(
            nodes,
            hour,
            heatingSetpointC,
            coolingSetpointC,
            freeFloatingTemperatures,
            controlledTemperatures,
            heatingLoadW,
            coolingLoadW,
            options.TimeStepSeconds,
            airNodeIndex);
    }

    private static double CalculateAirTemperatureResponsePerW(
        IReadOnlyList<Iso52016V2NodeDefinition> nodes,
        IReadOnlyList<Iso52016V2ConductanceLink> internalConductances,
        IReadOnlyList<Iso52016V2BoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016V2HourlyInputRecord hour,
        IReadOnlyList<double> previousTemperaturesC,
        double timeStepSeconds,
        int airNodeIndex,
        double freeFloatingAirTemperatureC)
    {
        var withUnitLoad = SolveTemperatures(
            nodes,
            internalConductances,
            boundaryConductances,
            nodeIndex,
            hour,
            previousTemperaturesC,
            timeStepSeconds,
            hvacLoadW: UnitHvacLoadW,
            hvacNodeIndex: airNodeIndex);

        return withUnitLoad[airNodeIndex] - freeFloatingAirTemperatureC;
    }

    private static double[] SolveTemperatures(
        IReadOnlyList<Iso52016V2NodeDefinition> nodes,
        IReadOnlyList<Iso52016V2ConductanceLink> internalConductances,
        IReadOnlyList<Iso52016V2BoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016V2HourlyInputRecord hour,
        IReadOnlyList<double> previousTemperaturesC,
        double timeStepSeconds,
        double hvacLoadW,
        int hvacNodeIndex)
    {
        var matrix = BuildCoefficientMatrix(
            nodes,
            internalConductances,
            boundaryConductances,
            nodeIndex,
            timeStepSeconds);

        var rhs = BuildRightHandSide(
            nodes,
            boundaryConductances,
            nodeIndex,
            hour,
            previousTemperaturesC,
            timeStepSeconds,
            hvacLoadW,
            hvacNodeIndex);

        return SolveLinearSystem(matrix, rhs);
    }

    private static double[,] BuildCoefficientMatrix(
        IReadOnlyList<Iso52016V2NodeDefinition> nodes,
        IReadOnlyList<Iso52016V2ConductanceLink> internalConductances,
        IReadOnlyList<Iso52016V2BoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        double timeStepSeconds)
    {
        var size = nodes.Count;
        var matrix = new double[size, size];

        for (var i = 0; i < size; i++)
        {
            matrix[i, i] = nodes[i].HeatCapacityJPerK / timeStepSeconds;
        }

        foreach (var link in internalConductances)
        {
            var fromIndex = nodeIndex[link.FromNodeId];
            var toIndex = nodeIndex[link.ToNodeId];
            var conductance = link.ConductanceWPerK;

            matrix[fromIndex, fromIndex] += conductance;
            matrix[toIndex, toIndex] += conductance;
            matrix[fromIndex, toIndex] -= conductance;
            matrix[toIndex, fromIndex] -= conductance;
        }

        foreach (var link in boundaryConductances)
        {
            matrix[nodeIndex[link.NodeId], nodeIndex[link.NodeId]] += link.ConductanceWPerK;
        }

        return matrix;
    }

    private static double[] BuildRightHandSide(
        IReadOnlyList<Iso52016V2NodeDefinition> nodes,
        IReadOnlyList<Iso52016V2BoundaryConductance> boundaryConductances,
        IReadOnlyDictionary<string, int> nodeIndex,
        Iso52016V2HourlyInputRecord hour,
        IReadOnlyList<double> previousTemperaturesC,
        double timeStepSeconds,
        double hvacLoadW,
        int hvacNodeIndex)
    {
        var rhs = new double[nodes.Count];

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            rhs[i] = node.HeatCapacityJPerK / timeStepSeconds * previousTemperaturesC[i];

            if (hour.NodeHeatGainsW.TryGetValue(node.NodeId, out var gainW))
                rhs[i] += gainW;
        }

        rhs[hvacNodeIndex] += hvacLoadW;

        foreach (var link in boundaryConductances)
        {
            rhs[nodeIndex[link.NodeId]] +=
                link.ConductanceWPerK *
                hour.BoundaryTemperaturesC[link.BoundaryId];
        }

        return rhs;
    }

    private static double[] SolveLinearSystem(
        double[,] matrix,
        double[] rhs)
    {
        var size = rhs.Length;
        var a = (double[,])matrix.Clone();
        var b = (double[])rhs.Clone();

        for (var pivot = 0; pivot < size; pivot++)
        {
            var bestRow = pivot;
            var bestValue = Math.Abs(a[pivot, pivot]);

            for (var row = pivot + 1; row < size; row++)
            {
                var value = Math.Abs(a[row, pivot]);

                if (value > bestValue)
                {
                    bestValue = value;
                    bestRow = row;
                }
            }

            if (bestValue <= MinimumPositive)
                throw new InvalidOperationException("ISO 52016 V2 solver matrix is singular or ill-conditioned.");

            if (bestRow != pivot)
            {
                for (var column = pivot; column < size; column++)
                {
                    (a[pivot, column], a[bestRow, column]) = (a[bestRow, column], a[pivot, column]);
                }

                (b[pivot], b[bestRow]) = (b[bestRow], b[pivot]);
            }

            for (var row = pivot + 1; row < size; row++)
            {
                var factor = a[row, pivot] / a[pivot, pivot];
                if (Math.Abs(factor) <= MinimumPositive)
                    continue;

                for (var column = pivot; column < size; column++)
                {
                    a[row, column] -= factor * a[pivot, column];
                }

                b[row] -= factor * b[pivot];
            }
        }

        var solution = new double[size];

        for (var row = size - 1; row >= 0; row--)
        {
            var sum = b[row];

            for (var column = row + 1; column < size; column++)
            {
                sum -= a[row, column] * solution[column];
            }

            solution[row] = sum / a[row, row];
        }

        return solution;
    }

    private static Iso52016V2HourlyResult CreateHourlyResult(
        IReadOnlyList<Iso52016V2NodeDefinition> nodes,
        Iso52016V2HourlyInputRecord hour,
        double heatingSetpointC,
        double coolingSetpointC,
        IReadOnlyList<double> freeFloatingTemperatures,
        IReadOnlyList<double> controlledTemperatures,
        double heatingLoadW,
        double coolingLoadW,
        double timeStepSeconds,
        int airNodeIndex)
    {
        var nodeStates = nodes
            .Select((node, index) => new Iso52016V2HourlyNodeState(
                NodeId: node.NodeId,
                TemperatureBeforeHvacC: freeFloatingTemperatures[index],
                TemperatureAfterHvacC: controlledTemperatures[index],
                HeatGainW: hour.NodeHeatGainsW.TryGetValue(node.NodeId, out var gainW) ? gainW : 0.0))
            .ToArray();

        return new Iso52016V2HourlyResult(
            HourOfYear: hour.HourOfYear,
            Month: hour.Month,
            Day: hour.Day,
            Hour: hour.Hour,
            HeatingSetpointC: heatingSetpointC,
            CoolingSetpointC: coolingSetpointC,
            AirTemperatureBeforeHvacC: freeFloatingTemperatures[airNodeIndex],
            AirTemperatureAfterHvacC: controlledTemperatures[airNodeIndex],
            HeatingLoadW: heatingLoadW,
            CoolingLoadW: coolingLoadW,
            TimeStepSeconds: timeStepSeconds,
            NodeStates: nodeStates);
    }

    private static IReadOnlyList<Iso52016V2MonthlySummary> BuildMonthlySummaries(
        IReadOnlyList<Iso52016V2HourlyResult> hours) =>
        hours
            .GroupBy(hour => hour.Month)
            .OrderBy(group => group.Key)
            .Select(group => new Iso52016V2MonthlySummary(
                Month: group.Key,
                HeatingEnergyKWh: group.Sum(hour => hour.HeatingEnergyKWh),
                CoolingEnergyKWh: group.Sum(hour => hour.CoolingEnergyKWh),
                TotalNodeHeatGainsKWh: group.Sum(hour => hour.TotalNodeHeatGainsKWh),
                PeakHeatingLoadW: group.Max(hour => hour.HeatingLoadW),
                PeakCoolingLoadW: group.Max(hour => hour.CoolingLoadW),
                AverageAirTemperatureAfterHvacC: group.Average(hour => hour.AirTemperatureAfterHvacC)))
            .ToArray();

    private static Result Validate(
        Iso52016V2HourlySolverRequest request)
    {
        if (request is null)
            return Result.Validation("ISO 52016 V2 solver request is required.");

        if (string.IsNullOrWhiteSpace(request.ZoneCode))
            return Result.Validation("ISO 52016 V2 zone code is required.");

        if (request.Nodes is null || request.Nodes.Count == 0)
            return Result.Validation("ISO 52016 V2 solver requires at least one thermal node.");

        if (request.Hours is null || request.Hours.Count == 0)
            return Result.Validation("ISO 52016 V2 solver requires at least one hourly input record.");

        var options = request.Options ?? new Iso52016V2HourlySolverOptions();

        if (options.TimeStepSeconds <= 0)
            return Result.Validation("ISO 52016 V2 solver time step must be greater than zero.");

        if (options.DefaultCoolingSetpointC <= options.DefaultHeatingSetpointC)
            return Result.Validation("ISO 52016 V2 default cooling setpoint must be greater than heating setpoint.");

        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in request.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId))
                return Result.Validation("ISO 52016 V2 node id is required.");

            if (!nodeIds.Add(node.NodeId))
                return Result.Validation($"ISO 52016 V2 node id '{node.NodeId}' is duplicated.");

            if (node.HeatCapacityJPerK <= 0)
                return Result.Validation($"ISO 52016 V2 node '{node.NodeId}' heat capacity must be greater than zero.");
        }

        if (!nodeIds.Contains(options.AirNodeId))
            return Result.Validation($"ISO 52016 V2 air node '{options.AirNodeId}' was not found in node definitions.");

        foreach (var link in request.InternalConductances ?? Array.Empty<Iso52016V2ConductanceLink>())
        {
            if (!nodeIds.Contains(link.FromNodeId))
                return Result.Validation($"ISO 52016 V2 internal conductance from-node '{link.FromNodeId}' was not found.");

            if (!nodeIds.Contains(link.ToNodeId))
                return Result.Validation($"ISO 52016 V2 internal conductance to-node '{link.ToNodeId}' was not found.");

            if (string.Equals(link.FromNodeId, link.ToNodeId, StringComparison.OrdinalIgnoreCase))
                return Result.Validation("ISO 52016 V2 internal conductance cannot connect a node to itself.");

            if (link.ConductanceWPerK <= 0)
                return Result.Validation("ISO 52016 V2 internal conductance must be greater than zero.");
        }

        foreach (var link in request.BoundaryConductances ?? Array.Empty<Iso52016V2BoundaryConductance>())
        {
            if (!nodeIds.Contains(link.NodeId))
                return Result.Validation($"ISO 52016 V2 boundary conductance node '{link.NodeId}' was not found.");

            if (string.IsNullOrWhiteSpace(link.BoundaryId))
                return Result.Validation("ISO 52016 V2 boundary id is required.");

            if (link.ConductanceWPerK <= 0)
                return Result.Validation("ISO 52016 V2 boundary conductance must be greater than zero.");
        }

        foreach (var hour in request.Hours)
        {
            if (hour.BoundaryTemperaturesC is null)
                return Result.Validation($"ISO 52016 V2 boundary temperatures are required at hour {hour.HourOfYear}.");

            if (hour.NodeHeatGainsW is null)
                return Result.Validation($"ISO 52016 V2 node heat gains are required at hour {hour.HourOfYear}.");

            foreach (var boundary in request.BoundaryConductances ?? Array.Empty<Iso52016V2BoundaryConductance>())
            {
                if (!hour.BoundaryTemperaturesC.ContainsKey(boundary.BoundaryId))
                    return Result.Validation($"ISO 52016 V2 boundary temperature '{boundary.BoundaryId}' is missing at hour {hour.HourOfYear}.");
            }

            foreach (var nodeGain in hour.NodeHeatGainsW)
            {
                if (!nodeIds.Contains(nodeGain.Key))
                    return Result.Validation($"ISO 52016 V2 node heat gain references unknown node '{nodeGain.Key}' at hour {hour.HourOfYear}.");
            }

            var heatingSetpointC = hour.HeatingSetpointC ?? options.DefaultHeatingSetpointC;
            var coolingSetpointC = hour.CoolingSetpointC ?? options.DefaultCoolingSetpointC;

            if (coolingSetpointC <= heatingSetpointC)
                return Result.Validation($"ISO 52016 V2 cooling setpoint must be greater than heating setpoint at hour {hour.HourOfYear}.");
        }

        return Result.Success();
    }
}
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\V2\Iso52016InternalGainReferenceDataProvider.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

public sealed class Iso52016InternalGainReferenceDataProvider : IIso52016InternalGainReferenceDataProvider
{
    private const string SourceNote = "Seed data for ISO 52016-style sensible internal gains. Replace with national annex, project schedule or verified reference table when available.";

    private static readonly IReadOnlyList<Iso52016InternalGainReferenceData> Items =
    [
        new("Residential", 0.04, 70.0, 4.0, 3.0, 0.50, 0.50, SourceNote),
        new("Office", 0.10, 75.0, 9.0, 12.0, 0.55, 0.45, SourceNote),
        new("HotelGuestRoom", 0.05, 70.0, 7.0, 4.0, 0.50, 0.50, SourceNote),
        new("HotelLobby", 0.20, 75.0, 10.0, 8.0, 0.55, 0.45, SourceNote),
        new("Restaurant", 0.80, 80.0, 12.0, 18.0, 0.60, 0.40, SourceNote),
        new("Retail", 0.20, 75.0, 14.0, 8.0, 0.55, 0.45, SourceNote),
        new("School", 0.40, 75.0, 9.0, 7.0, 0.55, 0.45, SourceNote),
        new("Healthcare", 0.12, 75.0, 10.0, 15.0, 0.55, 0.45, SourceNote),
        new("Storage", 0.02, 70.0, 3.0, 2.0, 0.50, 0.50, SourceNote),
        new("TechnicalRoom", 0.01, 70.0, 4.0, 20.0, 0.70, 0.30, SourceNote)
    ];

    public IReadOnlyList<Iso52016InternalGainReferenceData> GetAll() => Items;

    public Result<Iso52016InternalGainReferenceData> GetByUseType(
        string useType)
    {
        if (string.IsNullOrWhiteSpace(useType))
            return Result<Iso52016InternalGainReferenceData>.Validation("Internal gain use type is required.");

        var item = Items.FirstOrDefault(reference =>
            string.Equals(reference.UseType, useType, StringComparison.OrdinalIgnoreCase));

        return item is null
            ? Result<Iso52016InternalGainReferenceData>.NotFound($"Internal gain reference data for use type '{useType}' was not found.")
            : Result<Iso52016InternalGainReferenceData>.Success(item);
    }

    public Result<Iso52016InternalGainCalculationResult> CalculatePeakSensibleGain(
        string useType,
        double floorAreaM2,
        double occupancyFactor = 1.0,
        double lightingFactor = 1.0,
        double equipmentFactor = 1.0)
    {
        if (floorAreaM2 <= 0)
            return Result<Iso52016InternalGainCalculationResult>.Validation("Floor area for internal gains must be greater than zero.");

        if (occupancyFactor < 0 || lightingFactor < 0 || equipmentFactor < 0)
            return Result<Iso52016InternalGainCalculationResult>.Validation("Internal gain schedule factors cannot be negative.");

        var referenceResult = GetByUseType(useType);

        if (referenceResult.IsFailure)
            return Result<Iso52016InternalGainCalculationResult>.Failure(referenceResult);

        var reference = referenceResult.Value;
        var occupantGainW = floorAreaM2 * reference.OccupantDensityPersonPerM2 * reference.SensibleHeatPerPersonW * occupancyFactor;
        var lightingGainW = floorAreaM2 * reference.LightingPowerDensityWPerM2 * lightingFactor;
        var equipmentGainW = floorAreaM2 * reference.EquipmentPowerDensityWPerM2 * equipmentFactor;
        var totalGainW = occupantGainW + lightingGainW + equipmentGainW;

        return Result<Iso52016InternalGainCalculationResult>.Success(
            new Iso52016InternalGainCalculationResult(
                UseType: reference.UseType,
                FloorAreaM2: floorAreaM2,
                OccupantGainW: occupantGainW,
                LightingGainW: lightingGainW,
                EquipmentGainW: equipmentGainW,
                TotalSensibleGainW: totalGainW,
                ConvectiveGainW: totalGainW * reference.ConvectiveFraction,
                RadiativeGainW: totalGainW * reference.RadiativeFraction));
    }
}
'@

Write-Stage2File -RelativePath 'src\Backend\AssistantEngineer.Modules.Calculations\Application\Services\Iso52016\V2\Iso52016AdjacentUnconditionedZoneTemperatureSolver.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

public sealed class Iso52016AdjacentUnconditionedZoneTemperatureSolver : IIso52016AdjacentUnconditionedZoneTemperatureSolver
{
    public Result<Iso52016AdjacentUnconditionedZoneTemperatureResult> Solve(
        Iso52016AdjacentUnconditionedZoneTemperatureRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016AdjacentUnconditionedZoneTemperatureResult>.Failure(validation);

        var capacityTermWPerK = request.ThermalCapacityJPerK / request.TimeStepSeconds;
        var totalConductance =
            request.HeatTransferToOutdoorWPerK +
            request.HeatTransferToGroundWPerK +
            request.HeatTransferToConditionedZoneWPerK;

        var totalGainsW = request.InternalGainsW + request.SolarGainsW;

        var numerator =
            capacityTermWPerK * request.AdjacentZonePreviousTemperatureC +
            request.HeatTransferToOutdoorWPerK * request.OutdoorTemperatureC +
            request.HeatTransferToGroundWPerK * request.GroundTemperatureC +
            request.HeatTransferToConditionedZoneWPerK * request.ConditionedZoneTemperatureC +
            totalGainsW;

        var denominator = capacityTermWPerK + totalConductance;
        var temperatureC = numerator / denominator;
        var heatFlowToConditionedZoneW =
            request.HeatTransferToConditionedZoneWPerK *
            (temperatureC - request.ConditionedZoneTemperatureC);

        return Result<Iso52016AdjacentUnconditionedZoneTemperatureResult>.Success(
            new Iso52016AdjacentUnconditionedZoneTemperatureResult(
                TemperatureC: temperatureC,
                HeatFlowToConditionedZoneW: heatFlowToConditionedZoneW,
                TotalBoundaryConductanceWPerK: totalConductance,
                TotalGainsW: totalGainsW));
    }

    private static Result Validate(
        Iso52016AdjacentUnconditionedZoneTemperatureRequest request)
    {
        if (request.TimeStepSeconds <= 0)
            return Result.Validation("Adjacent unconditioned zone time step must be greater than zero.");

        if (request.ThermalCapacityJPerK <= 0)
            return Result.Validation("Adjacent unconditioned zone thermal capacity must be greater than zero.");

        if (request.HeatTransferToOutdoorWPerK < 0 ||
            request.HeatTransferToGroundWPerK < 0 ||
            request.HeatTransferToConditionedZoneWPerK < 0)
        {
            return Result.Validation("Adjacent unconditioned zone heat transfer coefficients cannot be negative.");
        }

        var totalConductance =
            request.HeatTransferToOutdoorWPerK +
            request.HeatTransferToGroundWPerK +
            request.HeatTransferToConditionedZoneWPerK;

        if (totalConductance <= 0)
            return Result.Validation("Adjacent unconditioned zone requires at least one heat transfer path.");

        return Result.Success();
    }
}
'@

Write-Stage2File -RelativePath 'tests\AssistantEngineer.Tests\Calculations\Iso52016\V2\Iso52016V2HourlySolverTests.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016V2HourlySolverTests
{
    private readonly Iso52016V2HourlySolver _solver = new();

    [Fact]
    public void Solve_HoldsAirNodeAtSteadyStateWithoutHvac()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: 22.0,
            gainsW: 0.0,
            initialAirTemperatureC: 22.0,
            initialMassTemperatureC: 22.0,
            hourCount: 24);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.All(result.Value.Hours, hour =>
        {
            Assert.Equal(0.0, hour.HeatingLoadW, precision: 6);
            Assert.Equal(0.0, hour.CoolingLoadW, precision: 6);
            Assert.Equal(22.0, hour.AirTemperatureAfterHvacC, precision: 6);
        });
        Assert.Equal(0.0, result.Value.AnnualHeatingEnergyKWh, precision: 6);
        Assert.Equal(0.0, result.Value.AnnualCoolingEnergyKWh, precision: 6);
    }

    [Fact]
    public void Solve_UsesImplicitNodeMatrixAndProducesHeatingLoad()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: -5.0,
            gainsW: 0.0,
            initialAirTemperatureC: 20.0,
            initialMassTemperatureC: 20.0,
            hourCount: 12);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Hours, hour => hour.HeatingLoadW > 0.0);
        Assert.All(result.Value.Hours, hour => Assert.Equal(0.0, hour.CoolingLoadW, precision: 6));
        Assert.True(result.Value.AnnualHeatingEnergyKWh > 0.0);
    }

    [Fact]
    public void Solve_UsesImplicitNodeMatrixAndProducesCoolingLoad()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: 35.0,
            gainsW: 2500.0,
            initialAirTemperatureC: 26.0,
            initialMassTemperatureC: 26.0,
            hourCount: 12);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Hours, hour => hour.CoolingLoadW > 0.0);
        Assert.All(result.Value.Hours, hour => Assert.Equal(0.0, hour.HeatingLoadW, precision: 6));
        Assert.True(result.Value.AnnualCoolingEnergyKWh > 0.0);
    }

    [Fact]
    public void Solve_KeepsMassNodeSeparateFromAirNode()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: -10.0,
            gainsW: 0.0,
            initialAirTemperatureC: 20.0,
            initialMassTemperatureC: 24.0,
            hourCount: 1);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);

        var hour = result.Value.Hours[0];
        var airTemperature = hour.GetNodeTemperatureAfterHvacC("air");
        var massTemperature = hour.GetNodeTemperatureAfterHvacC("mass");

        Assert.NotEqual(airTemperature, massTemperature, precision: 6);
        Assert.True(massTemperature > airTemperature);
    }

    [Fact]
    public void Solve_BuildsMonthlySummariesAndTracksGains()
    {
        var request = CreateTwoNodeRequest(
            outdoorTemperatureC: 22.0,
            gainsW: 500.0,
            initialAirTemperatureC: 22.0,
            initialMassTemperatureC: 22.0,
            hourCount: 48);

        var result = _solver.Solve(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Single(result.Value.MonthlySummaries);
        Assert.Equal(24.0, result.Value.AnnualTotalNodeHeatGainsKWh, precision: 6);
    }

    [Fact]
    public void Solve_RejectsMissingBoundaryTemperature()
    {
        var request = new Iso52016V2HourlySolverRequest(
            ZoneCode: "zone-1",
            Nodes:
            [
                new Iso52016V2NodeDefinition("air", 1_000_000.0, 20.0, IsAirNode: true)
            ],
            InternalConductances: [],
            BoundaryConductances:
            [
                new Iso52016V2BoundaryConductance("air", "outdoor", 100.0)
            ],
            Hours:
            [
                new Iso52016V2HourlyInputRecord(
                    HourOfYear: 0,
                    Month: 1,
                    Day: 1,
                    Hour: 0,
                    BoundaryTemperaturesC: new Dictionary<string, double>(),
                    NodeHeatGainsW: new Dictionary<string, double>())
            ]);

        var result = _solver.Solve(request);

        Assert.True(result.IsFailure);
        Assert.Equal("ISO 52016 V2 boundary temperature 'outdoor' is missing at hour 0.", result.Error);
    }

    private static Iso52016V2HourlySolverRequest CreateTwoNodeRequest(
        double outdoorTemperatureC,
        double gainsW,
        double initialAirTemperatureC,
        double initialMassTemperatureC,
        int hourCount)
    {
        var hours = Enumerable
            .Range(0, hourCount)
            .Select(hour => new Iso52016V2HourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour % 24,
                BoundaryTemperaturesC: new Dictionary<string, double>
                {
                    ["outdoor"] = outdoorTemperatureC
                },
                NodeHeatGainsW: new Dictionary<string, double>
                {
                    ["air"] = gainsW,
                    ["mass"] = 0.0
                },
                HeatingSetpointC: 20.0,
                CoolingSetpointC: 26.0))
            .ToArray();

        return new Iso52016V2HourlySolverRequest(
            ZoneCode: "zone-1",
            Nodes:
            [
                new Iso52016V2NodeDefinition("air", 1_200_000.0, initialAirTemperatureC, IsAirNode: true),
                new Iso52016V2NodeDefinition("mass", 8_000_000.0, initialMassTemperatureC)
            ],
            InternalConductances:
            [
                new Iso52016V2ConductanceLink("air", "mass", 40.0)
            ],
            BoundaryConductances:
            [
                new Iso52016V2BoundaryConductance("air", "outdoor", 90.0),
                new Iso52016V2BoundaryConductance("mass", "outdoor", 15.0)
            ],
            Hours: hours,
            Options: new Iso52016V2HourlySolverOptions(
                TimeStepSeconds: 3600.0,
                AirNodeId: "air",
                DefaultHeatingSetpointC: 20.0,
                DefaultCoolingSetpointC: 26.0));
    }
}
'@

Write-Stage2File -RelativePath 'tests\AssistantEngineer.Tests\Calculations\Iso52016\V2\Iso52016InternalGainReferenceDataProviderTests.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016InternalGainReferenceDataProviderTests
{
    private readonly Iso52016InternalGainReferenceDataProvider _provider = new();

    [Fact]
    public void GetAll_ReturnsReferenceDataForCommonBuildingUses()
    {
        var items = _provider.GetAll();

        Assert.Contains(items, item => item.UseType == "Residential");
        Assert.Contains(items, item => item.UseType == "Office");
        Assert.Contains(items, item => item.UseType == "HotelGuestRoom");
        Assert.Contains(items, item => item.UseType == "Restaurant");
    }

    [Fact]
    public void CalculatePeakSensibleGain_SplitsConvectiveAndRadiativeFractions()
    {
        var result = _provider.CalculatePeakSensibleGain(
            useType: "Office",
            floorAreaM2: 100.0,
            occupancyFactor: 1.0,
            lightingFactor: 0.5,
            equipmentFactor: 0.25);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1350.0, result.Value.TotalSensibleGainW, precision: 6);
        Assert.Equal(result.Value.TotalSensibleGainW, result.Value.ConvectiveGainW + result.Value.RadiativeGainW, precision: 6);
    }

    [Fact]
    public void CalculatePeakSensibleGain_RejectsUnknownUseType()
    {
        var result = _provider.CalculatePeakSensibleGain(
            useType: "Unknown",
            floorAreaM2: 100.0);

        Assert.True(result.IsFailure);
        Assert.Equal("Internal gain reference data for use type 'Unknown' was not found.", result.Error);
    }
}
'@

Write-Stage2File -RelativePath 'tests\AssistantEngineer.Tests\Calculations\Iso52016\V2\Iso52016AdjacentUnconditionedZoneTemperatureSolverTests.cs' -Content @'
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016AdjacentUnconditionedZoneTemperatureSolverTests
{
    private readonly Iso52016AdjacentUnconditionedZoneTemperatureSolver _solver = new();

    [Fact]
    public void Solve_ReturnsTemperatureBetweenOutdoorGroundAndConditionedZoneWhenNoGains()
    {
        var result = _solver.Solve(new Iso52016AdjacentUnconditionedZoneTemperatureRequest(
            OutdoorTemperatureC: 0.0,
            AdjacentZonePreviousTemperatureC: 10.0,
            ConditionedZoneTemperatureC: 20.0,
            HeatTransferToOutdoorWPerK: 100.0,
            HeatTransferToGroundWPerK: 50.0,
            GroundTemperatureC: 8.0,
            HeatTransferToConditionedZoneWPerK: 60.0,
            InternalGainsW: 0.0,
            SolarGainsW: 0.0,
            ThermalCapacityJPerK: 3_600_000.0));

        Assert.True(result.IsSuccess, result.Error);
        Assert.InRange(result.Value.TemperatureC, 0.0, 20.0);
        Assert.True(result.Value.HeatFlowToConditionedZoneW < 0.0);
    }

    [Fact]
    public void Solve_InternalAndSolarGainsWarmAdjacentZone()
    {
        var withoutGains = _solver.Solve(new Iso52016AdjacentUnconditionedZoneTemperatureRequest(
            OutdoorTemperatureC: 5.0,
            AdjacentZonePreviousTemperatureC: 8.0,
            ConditionedZoneTemperatureC: 20.0,
            HeatTransferToOutdoorWPerK: 80.0,
            HeatTransferToGroundWPerK: 20.0,
            GroundTemperatureC: 10.0,
            HeatTransferToConditionedZoneWPerK: 40.0,
            InternalGainsW: 0.0,
            SolarGainsW: 0.0,
            ThermalCapacityJPerK: 2_000_000.0));

        var withGains = _solver.Solve(new Iso52016AdjacentUnconditionedZoneTemperatureRequest(
            OutdoorTemperatureC: 5.0,
            AdjacentZonePreviousTemperatureC: 8.0,
            ConditionedZoneTemperatureC: 20.0,
            HeatTransferToOutdoorWPerK: 80.0,
            HeatTransferToGroundWPerK: 20.0,
            GroundTemperatureC: 10.0,
            HeatTransferToConditionedZoneWPerK: 40.0,
            InternalGainsW: 200.0,
            SolarGainsW: 300.0,
            ThermalCapacityJPerK: 2_000_000.0));

        Assert.True(withoutGains.IsSuccess, withoutGains.Error);
        Assert.True(withGains.IsSuccess, withGains.Error);
        Assert.True(withGains.Value.TemperatureC > withoutGains.Value.TemperatureC);
        Assert.Equal(500.0, withGains.Value.TotalGainsW, precision: 6);
    }

    [Fact]
    public void Solve_RejectsNegativeConductance()
    {
        var result = _solver.Solve(new Iso52016AdjacentUnconditionedZoneTemperatureRequest(
            OutdoorTemperatureC: 0.0,
            AdjacentZonePreviousTemperatureC: 10.0,
            ConditionedZoneTemperatureC: 20.0,
            HeatTransferToOutdoorWPerK: -1.0,
            HeatTransferToGroundWPerK: 0.0,
            GroundTemperatureC: 8.0,
            HeatTransferToConditionedZoneWPerK: 60.0,
            InternalGainsW: 0.0,
            SolarGainsW: 0.0,
            ThermalCapacityJPerK: 3_600_000.0));

        Assert.True(result.IsFailure);
        Assert.Equal("Adjacent unconditioned zone heat transfer coefficients cannot be negative.", result.Error);
    }
}
'@

Write-Stage2File -RelativePath 'tests\AssistantEngineer.Tests\Calculations\Iso52016\V2\Iso52016V2RegistrationTests.cs' -Content @'
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016V2RegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersIso52016V2Services()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        AssertScoped<IIso52016V2HourlySolver, Iso52016V2HourlySolver>(services);
        AssertScoped<IIso52016InternalGainReferenceDataProvider, Iso52016InternalGainReferenceDataProvider>(services);
        AssertScoped<IIso52016AdjacentUnconditionedZoneTemperatureSolver, Iso52016AdjacentUnconditionedZoneTemperatureSolver>(services);
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
    }
}
'@


Patch-Iso52016Registration

Write-Host ''
Write-Host 'ISO52016 V2 stage files were added.'
Write-Host 'Recommended verification:'
Write-Host '  dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016V2|FullyQualifiedName~InternalGainReferenceData|FullyQualifiedName~AdjacentUnconditioned"'

if ($RunTests) {
    Push-Location $RepoRoot
    try {
        dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~Iso52016V2|FullyQualifiedName~InternalGainReferenceData|FullyQualifiedName~AdjacentUnconditioned"
    }
    finally {
        Pop-Location
    }
}
