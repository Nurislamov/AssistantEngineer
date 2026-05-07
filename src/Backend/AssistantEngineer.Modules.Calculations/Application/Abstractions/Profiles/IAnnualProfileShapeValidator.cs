using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

public interface IAnnualProfileShapeValidator
{
    AnnualProfileShapeValidationResult ValidateHourlyNonLeapProfile(
        IReadOnlyList<double> values,
        bool requireNonNegative = false,
        string? source = null);

    AnnualProfileShapeValidationResult ValidateMonthlyProfile(
        IReadOnlyList<double> values,
        bool requireNonNegative = false,
        string? source = null);

    AnnualProfileShapeValidationResult ValidateDailyProfile(
        IReadOnlyList<double> values,
        bool requireNonNegative = false,
        string? source = null);
}
