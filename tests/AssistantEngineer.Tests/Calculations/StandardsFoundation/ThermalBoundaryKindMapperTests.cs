using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Mappers;

namespace AssistantEngineer.Tests.Calculations.StandardsFoundation;

public sealed class ThermalBoundaryKindMapperTests
{
    [Fact]
    public void OutdoorMapsToOutdoorTransmissionBoundary()
    {
        var result = ThermalBoundaryKindMapper.ToTransmissionBoundaryType(ThermalBoundaryKind.Outdoor);

        Assert.True(result.IsSupported);
        Assert.Equal(TransmissionBoundaryType.Outdoor, result.Value);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void GroundMapsToGroundTransmissionBoundary()
    {
        var result = ThermalBoundaryKindMapper.ToTransmissionBoundaryType(ThermalBoundaryKind.Ground);

        Assert.True(result.IsSupported);
        Assert.Equal(TransmissionBoundaryType.Ground, result.Value);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void AdjacentConditionedMapsToTransmissionAdjacentConditionedZone()
    {
        var result = ThermalBoundaryKindMapper.ToTransmissionBoundaryType(ThermalBoundaryKind.AdjacentConditionedZone);

        Assert.True(result.IsSupported);
        Assert.Equal(TransmissionBoundaryType.AdjacentConditionedZone, result.Value);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void AdjacentUnconditionedMapsToTransmissionAdjacentUnheatedSpace()
    {
        var result = ThermalBoundaryKindMapper.ToTransmissionBoundaryType(ThermalBoundaryKind.AdjacentUnconditionedZone);

        Assert.True(result.IsSupported);
        Assert.Equal(TransmissionBoundaryType.AdjacentUnheatedSpace, result.Value);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void AdiabaticMapsToTransmissionInternalAdiabatic()
    {
        var result = ThermalBoundaryKindMapper.ToTransmissionBoundaryType(ThermalBoundaryKind.Adiabatic);

        Assert.True(result.IsSupported);
        Assert.Equal(TransmissionBoundaryType.InternalAdiabatic, result.Value);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void UnsupportedMappingsProduceDiagnosticsInsteadOfSilentWrongMapping()
    {
        var unsupported = ThermalBoundaryKindMapper.ToIso52016PhysicalSurfaceBoundaryType(ThermalBoundaryKind.Adiabatic);
        var lossy = ThermalBoundaryKindMapper.ToTransmissionBoundaryType(ThermalBoundaryKind.InternalPartition);

        Assert.False(unsupported.IsSupported);
        Assert.Null(unsupported.Value);
        Assert.NotEmpty(unsupported.Diagnostics);

        Assert.True(lossy.IsSupported);
        Assert.True(lossy.IsLossy);
        Assert.NotEmpty(lossy.Diagnostics);
        Assert.Equal(TransmissionBoundaryType.InternalAdiabatic, lossy.Value);
    }

    [Fact]
    public void Iso52016PhysicalMappingsStayDeterministicForSupportedKinds()
    {
        var outdoor = ThermalBoundaryKindMapper.ToIso52016PhysicalSurfaceBoundaryType(ThermalBoundaryKind.Outdoor);
        var ground = ThermalBoundaryKindMapper.ToIso52016PhysicalSurfaceBoundaryType(ThermalBoundaryKind.Ground);
        var adjacentConditioned = ThermalBoundaryKindMapper.ToIso52016PhysicalSurfaceBoundaryType(ThermalBoundaryKind.AdjacentConditionedZone);
        var adjacentUnconditioned = ThermalBoundaryKindMapper.ToIso52016PhysicalSurfaceBoundaryType(ThermalBoundaryKind.AdjacentUnconditionedZone);

        Assert.Equal(Iso52016PhysicalSurfaceBoundaryType.Outdoor, outdoor.Value);
        Assert.Equal(Iso52016PhysicalSurfaceBoundaryType.Ground, ground.Value);
        Assert.Equal(Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned, adjacentConditioned.Value);
        Assert.Equal(Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned, adjacentUnconditioned.Value);
    }
}
