using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.BenchmarkFixtures;

internal static class EnergyBenchmarkComparison
{
    public static EnergyBenchmarkComparisonResult CompareNumeric(
        string fixtureName,
        string fieldPath,
        double expected,
        double actual,
        EnergyBenchmarkTolerance tolerance)
    {
        if (tolerance.Absolute is null && tolerance.RelativePercent is null)
            throw new InvalidOperationException(
                $"Benchmark fixture '{fixtureName}' has no tolerance for expected field '{fieldPath}'.");

        var absoluteDifference = Math.Abs(actual - expected);
        var relativeDifferencePercent = CalculateRelativeDifferencePercent(expected, actual);
        var passedAbsolute = tolerance.Absolute is not null &&
                             absoluteDifference <= tolerance.Absolute.Value;
        var passedRelative = tolerance.RelativePercent is not null &&
                             relativeDifferencePercent <= tolerance.RelativePercent.Value;
        var passed = passedAbsolute || passedRelative;

        return new EnergyBenchmarkComparisonResult(
            fixtureName,
            fieldPath,
            expected,
            actual,
            absoluteDifference,
            relativeDifferencePercent,
            tolerance.Absolute,
            tolerance.RelativePercent,
            passed,
            BuildMessage(
                fixtureName,
                fieldPath,
                expected,
                actual,
                absoluteDifference,
                relativeDifferencePercent,
                tolerance,
                passed));
    }

    public static IReadOnlyList<EnergyBenchmarkComparisonResult> CompareExpectedNumericFields(
        EnergyBenchmarkFixture fixture,
        object actual)
    {
        var expectedValues = GetExpectedNumericValues(fixture);
        var results = new List<EnergyBenchmarkComparisonResult>(expectedValues.Count);

        foreach (var expectedValue in expectedValues)
        {
            if (!TryGetNumericValue(actual, expectedValue.Key, out var actualValue, out var failure))
            {
                results.Add(new EnergyBenchmarkComparisonResult(
                    fixture.FixtureName,
                    expectedValue.Key,
                    expectedValue.Value,
                    double.NaN,
                    double.NaN,
                    double.NaN,
                    fixture.Tolerances.Resolve(expectedValue.Key).Absolute,
                    fixture.Tolerances.Resolve(expectedValue.Key).RelativePercent,
                    false,
                    $"Fixture '{fixture.FixtureName}' field '{expectedValue.Key}' could not be read from actual result: {failure}"));
                continue;
            }

            results.Add(CompareNumeric(
                fixture.FixtureName,
                expectedValue.Key,
                expectedValue.Value,
                actualValue,
                fixture.Tolerances.Resolve(expectedValue.Key)));
        }

        return results;
    }

    public static IReadOnlyDictionary<string, double> GetExpectedNumericValues(
        EnergyBenchmarkFixture fixture)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        FlattenExpectedNumericValues(fixture.Expected, prefix: null, values);
        return values;
    }

    public static IReadOnlyDictionary<string, bool> GetExpectedBooleanValues(
        EnergyBenchmarkFixture fixture)
    {
        var values = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        FlattenExpectedBooleanValues(fixture.Expected, prefix: null, values);
        return values;
    }

    public static bool TryGetNumericValue(
        object source,
        string fieldPath,
        out double value,
        out string failure)
    {
        value = 0;

        if (!TryGetValue(source, fieldPath, out var raw, out failure))
            return false;

        if (raw is null)
        {
            failure = "actual value is null";
            return false;
        }

        if (!IsNumeric(raw))
        {
            failure = $"actual value type '{raw.GetType().Name}' is not numeric";
            return false;
        }

        value = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
        return true;
    }

    public static bool TryGetBooleanValue(
        object source,
        string fieldPath,
        out bool value,
        out string failure)
    {
        value = false;

        if (!TryGetValue(source, fieldPath, out var raw, out failure))
            return false;

        if (raw is not bool boolValue)
        {
            failure = raw is null
                ? "actual value is null"
                : $"actual value type '{raw.GetType().Name}' is not boolean";
            return false;
        }

        value = boolValue;
        return true;
    }

    private static void FlattenExpectedNumericValues(
        JsonElement element,
        string? prefix,
        IDictionary<string, double> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var path = string.IsNullOrWhiteSpace(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";

                    FlattenExpectedNumericValues(property.Value, path, values);
                }

                break;

            case JsonValueKind.Number:
                if (string.IsNullOrWhiteSpace(prefix))
                    throw new InvalidOperationException("Expected numeric value has no field path.");

                values[prefix] = element.GetDouble();
                break;
        }
    }

    private static void FlattenExpectedBooleanValues(
        JsonElement element,
        string? prefix,
        IDictionary<string, bool> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var path = string.IsNullOrWhiteSpace(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";

                    FlattenExpectedBooleanValues(property.Value, path, values);
                }

                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                if (string.IsNullOrWhiteSpace(prefix))
                    throw new InvalidOperationException("Expected boolean value has no field path.");

                values[prefix] = element.GetBoolean();
                break;
        }
    }

    private static bool TryGetValue(
        object source,
        string fieldPath,
        out object? value,
        out string failure)
    {
        value = source;
        failure = string.Empty;

        foreach (var segment in fieldPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (value is null)
            {
                failure = $"segment '{segment}' was reached after a null value";
                return false;
            }

            var property = value
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => string.Equals(
                    candidate.Name,
                    segment,
                    StringComparison.OrdinalIgnoreCase));

            if (property is null)
            {
                failure = $"property '{segment}' was not found on type '{value.GetType().Name}'";
                return false;
            }

            value = property.GetValue(value);
        }

        return true;
    }

    private static double CalculateRelativeDifferencePercent(
        double expected,
        double actual)
    {
        if (Math.Abs(expected) <= double.Epsilon)
            return Math.Abs(actual) <= double.Epsilon ? 0 : double.PositiveInfinity;

        return Math.Abs(actual - expected) / Math.Abs(expected) * 100.0;
    }

    private static bool IsNumeric(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static string BuildMessage(
        string fixtureName,
        string fieldPath,
        double expected,
        double actual,
        double absoluteDifference,
        double relativeDifferencePercent,
        EnergyBenchmarkTolerance tolerance,
        bool passed)
    {
        var result = passed ? "passed" : "failed";
        var relativeDifference = double.IsPositiveInfinity(relativeDifferencePercent)
            ? "Infinity"
            : relativeDifferencePercent.ToString("0.######", CultureInfo.InvariantCulture);
        var absoluteTolerance = tolerance.Absolute?.ToString("0.######", CultureInfo.InvariantCulture) ?? "none";
        var relativeTolerance = tolerance.RelativePercent?.ToString("0.######", CultureInfo.InvariantCulture) ?? "none";

        return
            $"Fixture '{fixtureName}' field '{fieldPath}' {result}: " +
            $"expected {expected.ToString("0.######", CultureInfo.InvariantCulture)}, " +
            $"actual {actual.ToString("0.######", CultureInfo.InvariantCulture)}, " +
            $"absolute difference {absoluteDifference.ToString("0.######", CultureInfo.InvariantCulture)} " +
            $"(tolerance {absoluteTolerance}), relative difference {relativeDifference}% " +
            $"(tolerance {relativeTolerance}%).";
    }
}
