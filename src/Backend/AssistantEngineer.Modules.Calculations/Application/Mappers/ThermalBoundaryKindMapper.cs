using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;

namespace AssistantEngineer.Modules.Calculations.Application.Mappers;

public static class ThermalBoundaryKindMapper
{
    public static ThermalBoundaryMappingResult<TransmissionBoundaryType> ToTransmissionBoundaryType(
        ThermalBoundaryKind boundaryKind) =>
        boundaryKind switch
        {
            ThermalBoundaryKind.Outdoor => Success(TransmissionBoundaryType.Outdoor),
            ThermalBoundaryKind.Ground => Success(TransmissionBoundaryType.Ground),
            ThermalBoundaryKind.AdjacentConditionedZone => Success(TransmissionBoundaryType.AdjacentConditionedZone),
            ThermalBoundaryKind.AdjacentUnconditionedZone => Success(TransmissionBoundaryType.AdjacentUnheatedSpace),
            ThermalBoundaryKind.Adiabatic => Success(TransmissionBoundaryType.InternalAdiabatic),
            ThermalBoundaryKind.InternalPartition => Lossy(
                TransmissionBoundaryType.InternalAdiabatic,
                "Topology.Mapper.InternalPartitionToTransmissionAdiabatic",
                "InternalPartition was mapped to InternalAdiabatic because TransmissionBoundaryType does not define a dedicated InternalPartition value.",
                StandardCalculationFamily.InternalEngineering),
            _ => Unsupported<TransmissionBoundaryType>(
                "Topology.Mapper.TransmissionBoundaryUnsupported",
                $"Thermal boundary kind '{boundaryKind}' is not supported by TransmissionBoundaryType.",
                StandardCalculationFamily.InternalEngineering)
        };

    public static ThermalBoundaryMappingResult<ThermalBoundaryKind> FromTransmissionBoundaryType(
        TransmissionBoundaryType boundaryType) =>
        boundaryType switch
        {
            TransmissionBoundaryType.Outdoor => Success(ThermalBoundaryKind.Outdoor),
            TransmissionBoundaryType.Ground => Success(ThermalBoundaryKind.Ground),
            TransmissionBoundaryType.AdjacentConditionedZone => Success(ThermalBoundaryKind.AdjacentConditionedZone),
            TransmissionBoundaryType.AdjacentUnheatedSpace => Success(ThermalBoundaryKind.AdjacentUnconditionedZone),
            TransmissionBoundaryType.InternalAdiabatic => Success(ThermalBoundaryKind.Adiabatic),
            _ => Unsupported<ThermalBoundaryKind>(
                "Topology.Mapper.ThermalBoundaryFromTransmissionUnsupported",
                $"Transmission boundary type '{boundaryType}' cannot be mapped to thermal boundary kind.",
                StandardCalculationFamily.InternalEngineering)
        };

    public static ThermalBoundaryMappingResult<Iso52016PhysicalSurfaceBoundaryType> ToIso52016PhysicalSurfaceBoundaryType(
        ThermalBoundaryKind boundaryKind) =>
        boundaryKind switch
        {
            ThermalBoundaryKind.Outdoor => Success(Iso52016PhysicalSurfaceBoundaryType.Outdoor),
            ThermalBoundaryKind.Ground => Success(Iso52016PhysicalSurfaceBoundaryType.Ground),
            ThermalBoundaryKind.AdjacentConditionedZone => Success(Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned),
            ThermalBoundaryKind.AdjacentUnconditionedZone => Success(Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned),
            _ => Unsupported<Iso52016PhysicalSurfaceBoundaryType>(
                "Topology.Mapper.Iso52016PhysicalBoundaryUnsupported",
                $"Thermal boundary kind '{boundaryKind}' is not supported by Iso52016PhysicalSurfaceBoundaryType.",
                StandardCalculationFamily.ISO52016)
        };

    public static ThermalBoundaryMappingResult<ThermalBoundaryKind> FromIso52016PhysicalSurfaceBoundaryType(
        Iso52016PhysicalSurfaceBoundaryType boundaryType) =>
        boundaryType switch
        {
            Iso52016PhysicalSurfaceBoundaryType.Outdoor => Success(ThermalBoundaryKind.Outdoor),
            Iso52016PhysicalSurfaceBoundaryType.Ground => Success(ThermalBoundaryKind.Ground),
            Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned => Success(ThermalBoundaryKind.AdjacentConditionedZone),
            Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned => Success(ThermalBoundaryKind.AdjacentUnconditionedZone),
            _ => Unsupported<ThermalBoundaryKind>(
                "Topology.Mapper.ThermalBoundaryFromIso52016PhysicalUnsupported",
                $"Iso52016 physical boundary type '{boundaryType}' cannot be mapped to thermal boundary kind.",
                StandardCalculationFamily.ISO52016)
        };

    public static ThermalBoundaryMappingResult<Iso52016ConstructionBoundaryKind> ToIso52016ConstructionBoundaryKind(
        ThermalBoundaryKind boundaryKind) =>
        boundaryKind switch
        {
            ThermalBoundaryKind.Ground => Success(Iso52016ConstructionBoundaryKind.GroundFloor),
            ThermalBoundaryKind.InternalPartition => Success(Iso52016ConstructionBoundaryKind.InternalPartition),
            ThermalBoundaryKind.AdjacentUnconditionedZone => Success(Iso52016ConstructionBoundaryKind.AdjacentUnconditioned),
            ThermalBoundaryKind.Outdoor => Lossy(
                Iso52016ConstructionBoundaryKind.ExternalWall,
                "Topology.Mapper.OutdoorToIso52016ConstructionExternalWall",
                "Outdoor boundary was mapped to ExternalWall; roof/floor distinctions are not preserved in this canonical mapping.",
                StandardCalculationFamily.ISO52016),
            ThermalBoundaryKind.AdjacentConditionedZone => Lossy(
                Iso52016ConstructionBoundaryKind.InternalPartition,
                "Topology.Mapper.AdjacentConditionedToIso52016ConstructionInternalPartition",
                "AdjacentConditionedZone was mapped to InternalPartition because Iso52016ConstructionBoundaryKind has no adjacent-conditioned value.",
                StandardCalculationFamily.ISO52016),
            ThermalBoundaryKind.Adiabatic => Lossy(
                Iso52016ConstructionBoundaryKind.InternalPartition,
                "Topology.Mapper.AdiabaticToIso52016ConstructionInternalPartition",
                "Adiabatic boundary was mapped to InternalPartition for construction-level compatibility.",
                StandardCalculationFamily.ISO52016),
            _ => Unsupported<Iso52016ConstructionBoundaryKind>(
                "Topology.Mapper.Iso52016ConstructionBoundaryUnsupported",
                $"Thermal boundary kind '{boundaryKind}' is not supported by Iso52016ConstructionBoundaryKind.",
                StandardCalculationFamily.ISO52016)
        };

    public static ThermalBoundaryMappingResult<ThermalBoundaryKind> FromIso52016ConstructionBoundaryKind(
        Iso52016ConstructionBoundaryKind boundaryKind) =>
        boundaryKind switch
        {
            Iso52016ConstructionBoundaryKind.ExternalWall => Success(ThermalBoundaryKind.Outdoor),
            Iso52016ConstructionBoundaryKind.Roof => Lossy(
                ThermalBoundaryKind.Outdoor,
                "Topology.Mapper.Iso52016ConstructionRoofToOutdoor",
                "Iso52016 construction Roof was mapped to Outdoor in the canonical thermal boundary kind.",
                StandardCalculationFamily.ISO52016),
            Iso52016ConstructionBoundaryKind.Floor => Lossy(
                ThermalBoundaryKind.Outdoor,
                "Topology.Mapper.Iso52016ConstructionFloorToOutdoor",
                "Iso52016 construction Floor was mapped to Outdoor in the canonical thermal boundary kind.",
                StandardCalculationFamily.ISO52016),
            Iso52016ConstructionBoundaryKind.GroundFloor => Success(ThermalBoundaryKind.Ground),
            Iso52016ConstructionBoundaryKind.InternalPartition => Success(ThermalBoundaryKind.InternalPartition),
            Iso52016ConstructionBoundaryKind.AdjacentUnconditioned => Success(ThermalBoundaryKind.AdjacentUnconditionedZone),
            _ => Unsupported<ThermalBoundaryKind>(
                "Topology.Mapper.ThermalBoundaryFromIso52016ConstructionUnsupported",
                $"Iso52016 construction boundary kind '{boundaryKind}' cannot be mapped to thermal boundary kind.",
                StandardCalculationFamily.ISO52016)
        };

    private static ThermalBoundaryMappingResult<TBoundary> Success<TBoundary>(TBoundary value)
        where TBoundary : struct, Enum =>
        new(
            Value: value,
            IsSupported: true,
            IsLossy: false,
            Diagnostics: []);

    private static ThermalBoundaryMappingResult<TBoundary> Lossy<TBoundary>(
        TBoundary value,
        string code,
        string message,
        StandardCalculationFamily family)
        where TBoundary : struct, Enum =>
        new(
            Value: value,
            IsSupported: true,
            IsLossy: true,
            Diagnostics:
            [
                CreateDiagnostic(
                    severity: CalculationDiagnosticSeverity.Warning,
                    code: code,
                    message: message,
                    family: family)
            ]);

    private static ThermalBoundaryMappingResult<TBoundary> Unsupported<TBoundary>(
        string code,
        string message,
        StandardCalculationFamily family)
        where TBoundary : struct, Enum =>
        new(
            Value: null,
            IsSupported: false,
            IsLossy: false,
            Diagnostics:
            [
                CreateDiagnostic(
                    severity: CalculationDiagnosticSeverity.Warning,
                    code: code,
                    message: message,
                    family: family)
            ]);

    private static StandardCalculationDiagnostic CreateDiagnostic(
        CalculationDiagnosticSeverity severity,
        string code,
        string message,
        StandardCalculationFamily family) =>
        new(
            Severity: severity,
            Code: code,
            Message: message,
            Context: "ThermalBoundaryKindMapper",
            Source: "ThermalBoundaryKindMapper",
            Family: family,
            Stage: StandardCalculationStage.BoundaryCondition);
}
