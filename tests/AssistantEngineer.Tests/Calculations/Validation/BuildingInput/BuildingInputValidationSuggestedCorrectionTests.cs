using AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;

namespace AssistantEngineer.Tests.Calculations.Validation.BuildingInput;

public sealed class BuildingInputValidationSuggestedCorrectionTests
{
    private readonly BuildingInputValidationService _service = new();

    [Fact]
    public void KnownIssues_EmitSuggestedCorrections()
    {
        var orientationRequest = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildExternalWallMissingOrientation());
        var shgcRequest = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildInvalidShgcWindow());
        var constructionRequest = new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildConstructionOptInWithoutLayers(),
            IsConstructionLayerMassOptInIntended: true);

        var orientationResult = _service.Validate(orientationRequest);
        var shgcResult = _service.Validate(shgcRequest);
        var constructionResult = _service.Validate(constructionRequest);

        Assert.Contains(orientationResult.SuggestedCorrections, item => item.CorrectionId == "BIV-CORR-WALL-ORIENTATION-REVIEW");
        Assert.Contains(shgcResult.SuggestedCorrections, item => item.CorrectionId == "BIV-CORR-WINDOW-SHGC-CLAMP");
        Assert.Contains(constructionResult.SuggestedCorrections, item => item.CorrectionId == "BIV-CORR-CONSTRUCTION-LAYERS-ADD");
    }

    [Fact]
    public void Corrections_AreReviewOrientedAndDoNotAutoMutate()
    {
        var result = _service.Validate(new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildInvalidShgcWindow()));

        Assert.NotEmpty(result.SuggestedCorrections);
        Assert.All(result.SuggestedCorrections, correction =>
        {
            Assert.True(correction.RequiresUserReview);
        });
    }
}
