using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticBotRequestPolicyTests
{
    [Fact]
    public void ValidRequestIsTrimmedWithoutChangingMeaning()
    {
        var result = EquipmentDiagnosticBotRequestPolicy.ValidateAndNormalize(
            new EquipmentDiagnosticBotRequest(
                " Gree ",
                " H5 ",
                FreeText: " observed ",
                Series: " GMV ",
                PreferredLanguage: " en ",
                OperatorProvidedMeasurements: new Dictionary<string, string>
                {
                    [" pressure "] = " 2.1 MPa "
                }));

        Assert.True(result.IsValid);
        Assert.Equal("Gree", result.Request.Manufacturer);
        Assert.Equal("H5", result.Request.Code);
        Assert.Equal("observed", result.Request.FreeText);
        Assert.Equal("GMV", result.Request.Series);
        Assert.Equal("en", result.Request.PreferredLanguage);
        Assert.Equal("2.1 MPa", result.Request.OperatorProvidedMeasurements!["pressure"]);
    }

    [Theory]
    [InlineData(null, "H5", "Manufacturer")]
    [InlineData("Gree", null, "Code")]
    [InlineData(" ", "H5", "Manufacturer")]
    [InlineData("Gree", " ", "Code")]
    public void ManufacturerAndCodeAreRequired(string? manufacturer, string? code, string field)
    {
        var result = EquipmentDiagnosticBotRequestPolicy.ValidateAndNormalize(
            new EquipmentDiagnosticBotRequest(manufacturer, code));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors.Keys, key => key.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LimitsRejectInputWithoutTruncatingIt()
    {
        var manufacturer = new string('G', EquipmentDiagnosticBotRequestLimits.Manufacturer + 1);
        var result = EquipmentDiagnosticBotRequestPolicy.ValidateAndNormalize(
            new EquipmentDiagnosticBotRequest(manufacturer, "H5"));

        Assert.False(result.IsValid);
        Assert.Equal(manufacturer, result.Request.Manufacturer);
        Assert.Contains(nameof(EquipmentDiagnosticBotRequest.Manufacturer), result.Errors.Keys);
    }

    [Fact]
    public void EveryOptionalTextLimitIsEnforced()
    {
        var result = EquipmentDiagnosticBotRequestPolicy.ValidateAndNormalize(
            new EquipmentDiagnosticBotRequest(
                "Gree",
                "H5",
                FreeText: new string('F', EquipmentDiagnosticBotRequestLimits.FreeText + 1),
                Series: new string('S', EquipmentDiagnosticBotRequestLimits.Series + 1),
                ModelCode: new string('M', EquipmentDiagnosticBotRequestLimits.ModelCode + 1),
                PreferredLanguage: new string('L', EquipmentDiagnosticBotRequestLimits.PreferredLanguage + 1),
                SiteContext: new string('C', EquipmentDiagnosticBotRequestLimits.SiteContext + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(nameof(EquipmentDiagnosticBotRequest.FreeText), result.Errors.Keys);
        Assert.Contains(nameof(EquipmentDiagnosticBotRequest.Series), result.Errors.Keys);
        Assert.Contains(nameof(EquipmentDiagnosticBotRequest.ModelCode), result.Errors.Keys);
        Assert.Contains(nameof(EquipmentDiagnosticBotRequest.PreferredLanguage), result.Errors.Keys);
        Assert.Contains(nameof(EquipmentDiagnosticBotRequest.SiteContext), result.Errors.Keys);
    }

    [Fact]
    public void MeasurementCountAndFieldsAreValidated()
    {
        var measurements = Enumerable.Range(1, EquipmentDiagnosticBotRequestLimits.MeasurementCount + 1)
            .ToDictionary(index => $"measurement-{index}", _ => "value");
        measurements[new string('N', EquipmentDiagnosticBotRequestLimits.MeasurementName + 1)] =
            new string('V', EquipmentDiagnosticBotRequestLimits.MeasurementValue + 1);

        var result = EquipmentDiagnosticBotRequestPolicy.ValidateAndNormalize(
            new EquipmentDiagnosticBotRequest("Gree", "H5", OperatorProvidedMeasurements: measurements));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors.Keys, key => key.Contains("OperatorProvidedMeasurements", StringComparison.Ordinal));
    }

    [Fact]
    public void DisallowedControlCharactersAreRejected()
    {
        var result = EquipmentDiagnosticBotRequestPolicy.ValidateAndNormalize(
            new EquipmentDiagnosticBotRequest("Gree\u0000", "H5"));

        Assert.False(result.IsValid);
        Assert.Contains(nameof(EquipmentDiagnosticBotRequest.Manufacturer), result.Errors.Keys);
    }
}
