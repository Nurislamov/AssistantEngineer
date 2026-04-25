using AssistantEngineer.Modules.Buildings.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Buildings.Application.Options;

public sealed class BuildingArchetypeCatalogOptionsValidator : IValidateOptions<BuildingArchetypeCatalogOptions>
{
    private const int SupportedFormatVersion = 1;
    private const string SectionPath = "Buildings:ArchetypeCatalog";

    public ValidateOptionsResult Validate(string? name, BuildingArchetypeCatalogOptions options)
    {
        var failures = new List<string>();

        if (options.FormatVersion != SupportedFormatVersion)
        {
            failures.Add(
                $"{SectionPath}:FormatVersion must be {SupportedFormatVersion}. Actual value: {options.FormatVersion}.");
        }

        if (options.Archetypes.Count == 0)
        {
            failures.Add($"{SectionPath}:Archetypes must contain at least one building archetype.");
            return ValidateOptionsResult.Fail(failures);
        }

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < options.Archetypes.Count; index++)
        {
            var archetype = options.Archetypes[index];
            var path = $"{SectionPath}:Archetypes[{index}]";

            RequireNonEmpty(archetype.Code, $"{path}.Code", failures);
            RequireNonEmpty(archetype.DisplayName, $"{path}.DisplayName", failures);
            RequireDefinedEnum(archetype.Type, $"{path}.Type", failures);
            RequireInRange(archetype.RoomsCount, 1, 10_000, $"{path}.RoomsCount", failures);
            RequireInRange(archetype.RoomAreaM2, 0.1, 10_000, $"{path}.RoomAreaM2", failures);
            RequireInRange(archetype.RoomHeightM, 1, 20, $"{path}.RoomHeightM", failures);
            RequireInRange(archetype.IndoorTemperatureC, -50, 80, $"{path}.IndoorTemperatureC", failures);
            RequireInRange(archetype.PeopleCount, 0, 10_000, $"{path}.PeopleCount", failures);
            RequireInRange(archetype.EquipmentLoadWPerM2, 0, 5_000, $"{path}.EquipmentLoadWPerM2", failures);
            RequireInRange(archetype.LightingLoadWPerM2, 0, 5_000, $"{path}.LightingLoadWPerM2", failures);
            RequireInRange(archetype.ExternalWallAreaFactor, 0.01, 100, $"{path}.ExternalWallAreaFactor", failures);
            RequireInRange(archetype.ExternalWallUValue, 0.01, 10, $"{path}.ExternalWallUValue", failures);
            RequireInRange(archetype.WindowAreaM2Minimum, 0, 1_000, $"{path}.WindowAreaM2Minimum", failures);
            RequireRatio(archetype.WindowAreaFactor, $"{path}.WindowAreaFactor", failures);
            RequireInRange(archetype.WindowUValue, 0.01, 10, $"{path}.WindowUValue", failures);
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

    private static void RequireInRange(int value, int minimum, int maximum, string path, List<string> failures)
    {
        if (value < minimum || value > maximum)
            failures.Add($"{path} must be between {minimum} and {maximum}. Actual value: {value}.");
    }

    private static void RequireInRange(double value, double minimum, double maximum, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
            failures.Add($"{path} must be between {minimum} and {maximum}. Actual value: {value}.");
    }

    private static void RequireRatio(double value, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < 0 || value > 1)
            failures.Add($"{path} must be between 0 and 1.");
    }
}
