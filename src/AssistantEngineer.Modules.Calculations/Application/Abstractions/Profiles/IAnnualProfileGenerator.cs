using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

public interface IAnnualProfileGenerator
{
    AnnualProfile Generate(AnnualProfileRequest request);
}