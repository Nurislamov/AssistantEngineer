using AssistantEngineer.Modules.Buildings.Application.Models.StandardDefaults;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public static class RoomDefaultSuggestionFormatter
{
    public static string BuildPeopleMessage(RoomStandardDefaults defaults) =>
        $"Suggested default people count is {defaults.SuggestedPeopleCount} based on room type reference data.";

    public static string BuildEquipmentMessage(RoomStandardDefaults defaults) =>
        $"Suggested default equipment load is {defaults.EquipmentLoadWatts:F1} W based on room type reference data.";

    public static string BuildLightingMessage(RoomStandardDefaults defaults) =>
        $"Suggested default lighting load is {defaults.LightingLoadWatts:F1} W based on room type reference data.";
}