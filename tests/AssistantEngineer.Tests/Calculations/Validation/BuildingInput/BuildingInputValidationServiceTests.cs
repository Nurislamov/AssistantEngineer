using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;

namespace AssistantEngineer.Tests.Calculations.Validation.BuildingInput;

public sealed class BuildingInputValidationServiceTests
{
    private readonly BuildingInputValidationService _service = new();

    [Fact]
    public void ValidSimpleRoom_IsReadyOrReadyWithWarnings()
    {
        var request = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildValidSimpleRoom());

        var result = _service.Validate(request);

        Assert.Contains(
            result.ReadinessStatus,
            new[]
            {
                BuildingInputValidationReadinessStatus.Ready,
                BuildingInputValidationReadinessStatus.ReadyWithWarnings
            });
    }

    [Fact]
    public void ZeroAreaRoom_BlocksWithErrors()
    {
        var request = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildRoomZeroArea());

        var result = _service.Validate(request);

        Assert.Equal(BuildingInputValidationReadinessStatus.BlockedByErrors, result.ReadinessStatus);
        Assert.Contains(result.Diagnostics, item => item.Code == "BuildingInput.Geometry.RoomAreaNonPositive");
    }

    [Fact]
    public void MissingExternalWallOrientation_ProducesWarning()
    {
        var request = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildExternalWallMissingOrientation());

        var result = _service.Validate(request);

        Assert.Contains(result.Diagnostics, item =>
            item.Code == "BuildingInput.Envelope.ExternalWallOrientationMissing" &&
            item.Severity == BuildingInputValidationSeverity.Warning);
    }

    [Fact]
    public void InvalidWallUValue_BlocksValidation()
    {
        var request = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildInvalidWallUValue());

        var result = _service.Validate(request);

        Assert.Contains(result.Diagnostics, item =>
            item.Code == "BuildingInput.Envelope.WallUValueNonPositive" &&
            item.Severity == BuildingInputValidationSeverity.Error);
        Assert.Equal(BuildingInputValidationReadinessStatus.BlockedByErrors, result.ReadinessStatus);
    }

    [Fact]
    public void InvalidShgc_ProducesErrorAndSuggestedCorrection()
    {
        var request = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildInvalidShgcWindow());

        var result = _service.Validate(request);

        var diagnostic = Assert.Single(result.Diagnostics, item => item.Code == "BuildingInput.Openings.WindowShgcOutOfRange");
        Assert.Equal(BuildingInputValidationSeverity.Error, diagnostic.Severity);
        Assert.NotNull(diagnostic.SuggestedCorrection);
        Assert.Equal("BIV-CORR-WINDOW-SHGC-CLAMP", diagnostic.SuggestedCorrection!.CorrectionId);
    }

    [Fact]
    public void WindowAreaLargerThanWallArea_ProducesWarning()
    {
        var request = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildWindowAreaExceedsWall());

        var result = _service.Validate(request);

        Assert.Contains(result.Diagnostics, item => item.Code == "BuildingInput.Openings.WindowAreaExceedsRelatedExternalWallArea");
    }

    [Fact]
    public void MissingGroundMetadata_ProducesWarning()
    {
        var request = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildGroundBoundaryWithoutMetadata());

        var result = _service.Validate(request);

        Assert.Contains(result.Diagnostics, item => item.Code == "BuildingInput.Ground.MetadataMissingForGroundBoundary");
    }

    [Fact]
    public void ConstructionOptInMissingLayers_ProducesWarning()
    {
        var request = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildConstructionOptInWithoutLayers(),
            IsConstructionLayerMassOptInIntended: true);

        var result = _service.Validate(request);

        Assert.Contains(result.Diagnostics, item => item.Code == "BuildingInput.Construction.OptInMissingLayersFallbackWillBeUsed");
    }

    [Fact]
    public void Validation_DoesNotMutateInputData()
    {
        var building = BuildingInputValidationScenarioBuilder.BuildValidSimpleRoom();
        var room = building.Floors.First().Rooms.First();
        var originalArea = room.Area.SquareMeters;
        var originalHeight = room.HeightM;
        var originalWallCount = room.Walls.Count;
        var originalWindowCount = room.Windows.Count;

        _ = _service.Validate(new BuildingInputValidationRequest(building));

        Assert.Equal(originalArea, room.Area.SquareMeters);
        Assert.Equal(originalHeight, room.HeightM);
        Assert.Equal(originalWallCount, room.Walls.Count);
        Assert.Equal(originalWindowCount, room.Windows.Count);
    }
}
