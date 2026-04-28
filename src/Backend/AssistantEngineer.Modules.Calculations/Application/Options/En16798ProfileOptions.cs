using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class En16798ProfileOptions
{
    public bool UseStandardProfilesWhenMissingSchedules { get; init; } = true;
    public En16798ProfileCategory DefaultCategory { get; init; } = En16798ProfileCategory.II;
}