using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;

namespace AssistantEngineer.Tests.Calculations.StandardsFoundation;

public sealed class AnnualProfileShapeValidatorTests
{
    private readonly AnnualProfileShapeValidator _validator = new();

    [Fact]
    public void Accepts8760FiniteHourlyValues()
    {
        var values = Enumerable.Repeat(1.0, 8760).ToArray();

        var result = _validator.ValidateHourlyNonLeapProfile(values);

        Assert.True(result.IsValid);
        Assert.Equal(8760, result.ExpectedCount);
        Assert.Equal(8760, result.ActualCount);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData(8759)]
    [InlineData(8761)]
    public void RejectsHourlyProfileWithInvalidLength(int count)
    {
        var values = Enumerable.Repeat(0.0, count).ToArray();

        var result = _validator.ValidateHourlyNonLeapProfile(values);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "Standards.ProfileShape.CountMismatch");
    }

    [Fact]
    public void RejectsNonFiniteValuesWithDiagnostics()
    {
        var values = Enumerable.Repeat(1.0, 8760).ToArray();
        values[10] = double.NaN;
        values[25] = double.PositiveInfinity;

        var result = _validator.ValidateHourlyNonLeapProfile(values);

        Assert.False(result.IsValid);
        Assert.True(result.Diagnostics.Count(diagnostic =>
            diagnostic.Code == "Standards.ProfileShape.NonFiniteValue") >= 2);
    }

    [Fact]
    public void AcceptsMonthlyProfileWith12Values()
    {
        var result = _validator.ValidateMonthlyProfile(Enumerable.Repeat(0.5, 12).ToArray());

        Assert.True(result.IsValid);
        Assert.Equal(12, result.ExpectedCount);
        Assert.Equal(12, result.ActualCount);
    }

    [Fact]
    public void AcceptsDailyProfileWith24Values()
    {
        var result = _validator.ValidateDailyProfile(Enumerable.Repeat(0.5, 24).ToArray());

        Assert.True(result.IsValid);
        Assert.Equal(24, result.ExpectedCount);
        Assert.Equal(24, result.ActualCount);
    }

    [Fact]
    public void NonNegativeModeRejectsNegativeValues()
    {
        var values = Enumerable.Repeat(0.0, 12).ToArray();
        values[3] = -0.1;

        var result = _validator.ValidateMonthlyProfile(values, requireNonNegative: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "Standards.ProfileShape.NegativeValue");
    }
}
