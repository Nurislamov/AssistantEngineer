namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016LoadVectorCharacterizationTests
{
    private const double NumericTolerance = 1e-6;
    private const double RepeatTolerance = 1e-9;

    [Fact]
    public void RightHandSideVector_CurrentTermsRemainPinnedFiniteAndDeterministic()
    {
        var request = Iso52016MatrixSeamCharacterizationTestHelper.CreateTwoNodeRequest(
            outdoorTemperatureC: 8.0,
            airNodeGainW: 150.0,
            massNodeGainW: 0.0,
            initialAirTemperatureC: 21.0,
            initialMassTemperatureC: 21.0);

        var previous = request.Nodes.Select(node => node.InitialTemperatureC).ToArray();

        var first = Iso52016MatrixSeamCharacterizationTestHelper.BuildRightHandSide(request, previous, hvacLoadW: 0.0);
        var second = Iso52016MatrixSeamCharacterizationTestHelper.BuildRightHandSide(request, previous, hvacLoadW: 0.0);

        Assert.Equal(request.Nodes.Count, first.Length);
        Assert.Equal(request.Nodes.Count, second.Length);

        Assert.InRange(first[0], 7870.0 - NumericTolerance, 7870.0 + NumericTolerance);
        Assert.InRange(first[1], 46786.666666666664 - NumericTolerance, 46786.666666666664 + NumericTolerance);

        for (var i = 0; i < first.Length; i++)
        {
            Assert.False(double.IsNaN(first[i]));
            Assert.False(double.IsInfinity(first[i]));
            Assert.InRange(Math.Abs(first[i] - second[i]), 0.0, RepeatTolerance);
        }
    }

    [Fact]
    public void RightHandSideVector_PreservesGainAndHvacInjectionBehavior()
    {
        var baseRequest = Iso52016MatrixSeamCharacterizationTestHelper.CreateTwoNodeRequest(
            outdoorTemperatureC: 8.0,
            airNodeGainW: 0.0,
            massNodeGainW: 0.0);

        var gainsRequest = Iso52016MatrixSeamCharacterizationTestHelper.CreateTwoNodeRequest(
            outdoorTemperatureC: 8.0,
            airNodeGainW: 150.0,
            massNodeGainW: 0.0);

        var previous = baseRequest.Nodes.Select(node => node.InitialTemperatureC).ToArray();

        var noGains = Iso52016MatrixSeamCharacterizationTestHelper.BuildRightHandSide(baseRequest, previous, hvacLoadW: 0.0);
        var withGains = Iso52016MatrixSeamCharacterizationTestHelper.BuildRightHandSide(gainsRequest, previous, hvacLoadW: 0.0);
        var withHvac = Iso52016MatrixSeamCharacterizationTestHelper.BuildRightHandSide(baseRequest, previous, hvacLoadW: 500.0);

        Assert.InRange(withGains[0] - noGains[0], 150.0 - NumericTolerance, 150.0 + NumericTolerance);
        Assert.InRange(withGains[1] - noGains[1], 0.0 - NumericTolerance, 0.0 + NumericTolerance);

        Assert.InRange(withHvac[0] - noGains[0], 500.0 - NumericTolerance, 500.0 + NumericTolerance);
        Assert.InRange(withHvac[1] - noGains[1], 0.0 - NumericTolerance, 0.0 + NumericTolerance);
    }
}
