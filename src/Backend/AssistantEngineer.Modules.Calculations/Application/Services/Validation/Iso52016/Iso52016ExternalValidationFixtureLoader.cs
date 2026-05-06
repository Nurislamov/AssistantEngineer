using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Validation.Iso52016;

public sealed class Iso52016ExternalValidationFixtureLoader
{
    private static readonly string[] RequiredClaimBoundaryLines =
    {
        "Validation/internal engineering anchors only.",
        "Manual independent reference fixtures only.",
        "No full ISO 52016 parity claim.",
        "No pyBuildingEnergy parity claim.",
        "No EnergyPlus parity claim.",
        "No ASHRAE 140 validation claim.",
        "ExternalParityCovered is not allowed in this stage."
    };

    private static readonly string[] ForbiddenPositiveClaims =
    {
        "full ISO52016 parity",
        "full ISO 52016 parity",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validated",
        "validated against pyBuildingEnergy",
        "validated against EnergyPlus",
        "ExternalParityCovered"
    };

    private static readonly string[] ForbiddenManualReferenceSources =
    {
        "pybuildingenergy",
        "energyplus"
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public IReadOnlyList<Iso52016ExternalValidationFixture> LoadFromDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Fixture directory path is required.", nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Fixture directory was not found: {directoryPath}");

        var fixtures = new List<Iso52016ExternalValidationFixture>();
        foreach (var file in Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly)
                     .Order(StringComparer.OrdinalIgnoreCase))
        {
            fixtures.Add(LoadFromFile(file));
        }

        if (fixtures.Count == 0)
            throw new InvalidOperationException($"No ISO52016 external validation fixtures were found in {directoryPath}.");

        return fixtures;
    }

    public Iso52016ExternalValidationFixture LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Fixture file path is required.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Fixture file was not found.", filePath);

        var fixture = JsonSerializer.Deserialize<Iso52016ExternalValidationFixture>(
            File.ReadAllText(filePath),
            SerializerOptions);

        if (fixture is null)
            throw new InvalidOperationException($"Fixture did not parse: {filePath}");

        ValidateFixture(fixture, filePath);
        return fixture;
    }

    private static void ValidateFixture(Iso52016ExternalValidationFixture fixture, string filePath)
    {
        if (string.IsNullOrWhiteSpace(fixture.Id))
            throw new InvalidOperationException($"Fixture id is required: {filePath}");

        if (fixture.ClaimBoundary.Count == 0)
            throw new InvalidOperationException($"Fixture claim boundary is required: {filePath}");

        foreach (var line in RequiredClaimBoundaryLines)
        {
            if (!fixture.ClaimBoundary.Any(value => string.Equals(value, line, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Fixture claim boundary is missing line '{line}': {filePath}");
        }

        if (fixture.SourceKind == Iso52016ExternalValidationFixtureSourceKind.ManualIndependent &&
            !fixture.ClaimBoundary.Any(value =>
                string.Equals(value, "Manual independent reference fixtures only.", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"ManualIndependent fixture must declare manual-only claim boundary line: {filePath}");
        }

        ValidateReference(fixture, filePath);

        if (fixture.Expected is null)
            throw new InvalidOperationException($"Fixture expected result is required: {filePath}");

        if (fixture.Tolerance is null)
            throw new InvalidOperationException($"Fixture tolerance is required: {filePath}");

        if (fixture.Tolerance.AbsoluteTolerance < 0 || fixture.Tolerance.RelativeTolerancePercent < 0)
            throw new InvalidOperationException($"Fixture tolerance values must be non-negative: {filePath}");

        if (fixture.Input.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new InvalidOperationException($"Fixture input payload is required: {filePath}");

        AssertNoForbiddenPositiveClaims(filePath, fixture.ClaimBoundary);
    }

    private static void ValidateReference(Iso52016ExternalValidationFixture fixture, string filePath)
    {
        if (fixture.Reference is null)
            throw new InvalidOperationException($"Fixture reference metadata is required: {filePath}");

        if (string.IsNullOrWhiteSpace(fixture.Reference.DerivationDocument))
            throw new InvalidOperationException($"Fixture reference.derivationDocument is required: {filePath}");

        if (string.IsNullOrWhiteSpace(fixture.Reference.DerivationKind))
            throw new InvalidOperationException($"Fixture reference.derivationKind is required: {filePath}");

        if (string.IsNullOrWhiteSpace(fixture.Reference.SourceDescription))
            throw new InvalidOperationException($"Fixture reference.sourceDescription is required: {filePath}");

        var repoRoot = ResolveRepositoryRoot(filePath);
        var derivationPath = Path.Combine(
            repoRoot,
            fixture.Reference.DerivationDocument.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(derivationPath))
            throw new InvalidOperationException(
                $"Fixture reference.derivationDocument does not exist: {fixture.Reference.DerivationDocument} ({filePath})");

        if (fixture.SourceKind == Iso52016ExternalValidationFixtureSourceKind.ManualIndependent)
        {
            if (!string.Equals(
                    fixture.Reference.DerivationKind,
                    "ManualIndependentArithmetic",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"ManualIndependent fixture must use derivationKind=ManualIndependentArithmetic: {filePath}");
            }

            var referenceText = $"{fixture.Reference.DerivationDocument} {fixture.Reference.SourceDescription}";
            foreach (var forbidden in ForbiddenManualReferenceSources)
            {
                if (referenceText.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"ManualIndependent fixture reference must not use {forbidden} as a source: {filePath}");
                }
            }
        }
    }

    private static void AssertNoForbiddenPositiveClaims(string filePath, IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            foreach (var claim in ForbiddenPositiveClaims)
            {
                if (!line.Contains(claim, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (LineNegatesClaim(line, claim))
                    continue;

                throw new InvalidOperationException(
                    $"Forbidden positive claim '{claim}' found in fixture claim boundary: {filePath}");
            }
        }
    }

    private static bool LineNegatesClaim(string line, string claim)
    {
        var lineLower = line.ToLowerInvariant();
        var claimLower = claim.ToLowerInvariant();
        var index = lineLower.IndexOf(claimLower, StringComparison.Ordinal);
        if (index < 0)
            return false;

        var prefix = lineLower[..index];
        var suffix = lineLower[(index + claimLower.Length)..];
        return prefix.Contains("not ", StringComparison.Ordinal) ||
               prefix.Contains("no ", StringComparison.Ordinal) ||
               prefix.Contains("without ", StringComparison.Ordinal) ||
               prefix.Contains("does not ", StringComparison.Ordinal) ||
               prefix.Contains("doesn't ", StringComparison.Ordinal) ||
               prefix.Contains("must not ", StringComparison.Ordinal) ||
               suffix.Contains("not allowed", StringComparison.Ordinal) ||
               suffix.Contains("not permitted", StringComparison.Ordinal);
    }

    private static string ResolveRepositoryRoot(string fixturePath)
    {
        var fixtureDirectory = Path.GetDirectoryName(Path.GetFullPath(fixturePath));
        var fromFixture = TryResolveRepositoryRoot(fixtureDirectory);
        if (fromFixture is not null)
            return fromFixture;

        var fromWorkingDirectory = TryResolveRepositoryRoot(Directory.GetCurrentDirectory());
        if (fromWorkingDirectory is not null)
            return fromWorkingDirectory;

        throw new InvalidOperationException($"Could not resolve repository root for fixture: {fixturePath}");
    }

    private static string? TryResolveRepositoryRoot(string? startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
            return null;

        var current = new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        return null;
    }
}
