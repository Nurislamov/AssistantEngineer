using AssistantEngineer.Modules.Buildings.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Buildings.Application.Options;

public sealed class BuildingArchetypeCatalogOptionsValidator : IValidateOptions<BuildingArchetypeCatalogOptions>
{
    public ValidateOptionsResult Validate(string? name, BuildingArchetypeCatalogOptions options)
    {
        var failures = new List<string>();

        if (options.Archetypes.Count == 0)
        {
            failures.Add("At least one building archetype must be configured.");
            return ValidateOptionsResult.Fail(failures);
        }

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < options.Archetypes.Count; index++)
        {
            var archetype = options.Archetypes[index];
            var path = $"Archetypes[{index}]";

            RequireNonEmpty(archetype.Code, $"{path}.Code", failures);
            RequireNonEmpty(archetype.DisplayName, $"{path}.DisplayName", failures);
            RequireDefinedEnum(archetype.Type, $"{path}.Type", failures);
            RequirePositive(archetype.RoomsCount, $"{path}.RoomsCount", failures);
            RequirePositive(archetype.RoomAreaM2, $"{path}.RoomAreaM2", failures);
            RequirePositive(archetype.RoomHeightM, $"{path}.RoomHeightM", failures);
            RequireFinite(archetype.IndoorTemperatureC, $"{path}.IndoorTemperatureC", failures);
            RequireNonNegative(archetype.PeopleCount, $"{path}.PeopleCount", failures);
            RequireNonNegative(archetype.EquipmentLoadWPerM2, $"{path}.EquipmentLoadWPerM2", failures);
            RequireNonNegative(archetype.LightingLoadWPerM2, $"{path}.LightingLoadWPerM2", failures);
            RequirePositive(archetype.ExternalWallAreaFactor, $"{path}.ExternalWallAreaFactor", failures);
            RequirePositive(archetype.ExternalWallUValue, $"{path}.ExternalWallUValue", failures);
            RequireNonNegative(archetype.WindowAreaM2Minimum, $"{path}.WindowAreaM2Minimum", failures);
            RequireNonNegative(archetype.WindowAreaFactor, $"{path}.WindowAreaFactor", failures);
            RequirePositive(archetype.WindowUValue, $"{path}.WindowUValue", failures);
            RequireRatio(archetype.WindowShgc, $"{path}.WindowShgc", failures);
            RequireDefinedEnum(archetype.OddRoomOrientation, $"{path}.OddRoomOrientation", failures);
            RequireDefinedEnum(archetype.EvenRoomOrientation, $"{path}.EvenRoomOrientation", failures);

            if (!string.IsNullOrWhiteSpace(archetype.Code) && !codes.Add(archetype.Code))
                failures.Add($"{path}.Code must be unique. Duplicate code: '{archetype.Code}'.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireNonEmpty(string value, string path, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
            failures.Add($"{path} is required.");
    }

    private static void RequireDefinedEnum<TEnum>(TEnum value, string path, List<string> failures)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            failures.Add($"{path} must be a defined {typeof(TEnum).Name} value.");
    }

    private static void RequirePositive(int value, string path, List<string> failures)
    {
        if (value <= 0)
            failures.Add($"{path} must be greater than zero.");
    }

    private static void RequireNonNegative(int value, string path, List<string> failures)
    {
        if (value < 0)
            failures.Add($"{path} cannot be negative.");
    }

    private static void RequirePositive(double value, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value <= 0)
            failures.Add($"{path} must be a finite value greater than zero.");
    }

    private static void RequireNonNegative(double value, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < 0)
            failures.Add($"{path} must be a finite non-negative value.");
    }

    private static void RequireFinite(double value, string path, List<string> failures)
    {
        if (!double.IsFinite(value))
            failures.Add($"{path} must be a finite value.");
    }

    private static void RequireRatio(double value, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < 0 || value > 1)
            failures.Add($"{path} must be between 0 and 1.");
    }
}
