namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation;

public class ExternalReferenceValidationDocumentationTests
{
    [Fact]
    public void ExternalReferenceValidationPlanExists()
    {
        var path = GetParityPlanPath();

        Assert.True(
            File.Exists(path),
            $"Energy calculation equivalence plan was not found: {path}");
    }

    [Fact]
    public void ExternalReferenceValidationPlanContainsCoreSections()
    {
        var text = File.ReadAllText(
            GetParityPlanPath());

        var requiredSections = new[]
        {
            "# Energy calculation equivalence plan",
            "## ¬ажное правило нейминга",
            "## „то такое equivalence",
            "## P0 Ч основа расчЄтного €дра",
            "## P1 Ч расширение до полного расчЄтного покрыти€",
            "## P3 Ч не входит в текущий scope",
            "## ѕор€док реализации",
            "## Fixture policy",
            "## Tolerance policy"
        };

        foreach (var requiredSection in requiredSections)
        {
            Assert.Contains(
                requiredSection,
                text,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ExternalReferenceValidationPlanContainsMatrixCoreCodes()
    {
        var text = File.ReadAllText(
            GetParityPlanPath());

        var requiredCodes = new[]
        {
            "ISO52010.CLIMATE_CONVERSION",
            "ISO52010.SURFACE_IRRADIANCE",
            "WEATHER.EPW",
            "ISO52016.HOURLY_HEATING_NEED",
            "ISO52016.HOURLY_COOLING_NEED",
            "ISO52016.MONTHLY_HEATING_COOLING_NEED",
            "ISO52016.INTERNAL_TEMPERATURE_HOURLY",
            "ISO52016.SENSIBLE_LOAD_HOURLY",
            "ISO52016.THERMAL_ZONES",
            "DHW.EN12831_3",
            "PRIMARY_ENERGY.EN15316_1"
        };

        foreach (var requiredCode in requiredCodes)
        {
            Assert.Contains(
                requiredCode,
                text,
                StringComparison.Ordinal);
        }
    }

    private static string GetParityPlanPath() =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "architecture",
            "ExternalReferenceValidationPlan.md");
}