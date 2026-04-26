using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Mappers;

public static class CalculationsContractEnumMapper
{
    public static CoolingLoadCalculationMethod ToDomain(this CoolingLoadCalculationMethodDto method) =>
        method switch
        {
            CoolingLoadCalculationMethodDto.Simplified => CoolingLoadCalculationMethod.Simplified,
            CoolingLoadCalculationMethodDto.Iso52016 => CoolingLoadCalculationMethod.Iso52016,
            _ => throw UnsupportedEnumValue(method)
        };

    public static CoolingLoadCalculationMethodDto ToContract(this CoolingLoadCalculationMethod method) =>
        method switch
        {
            CoolingLoadCalculationMethod.Simplified => CoolingLoadCalculationMethodDto.Simplified,
            CoolingLoadCalculationMethod.Iso52016 => CoolingLoadCalculationMethodDto.Iso52016,
            _ => throw UnsupportedEnumValue(method)
        };

    public static HeatingLoadCalculationMethod ToDomain(this HeatingLoadCalculationMethodDto method) =>
        method switch
        {
            HeatingLoadCalculationMethodDto.En12831 => HeatingLoadCalculationMethod.En12831,
            _ => throw UnsupportedEnumValue(method)
        };

    public static HeatingLoadCalculationMethodDto ToContract(this HeatingLoadCalculationMethod method) =>
        method switch
        {
            HeatingLoadCalculationMethod.En12831 => HeatingLoadCalculationMethodDto.En12831,
            _ => throw UnsupportedEnumValue(method)
        };

    private static ArgumentOutOfRangeException UnsupportedEnumValue<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        new(nameof(value), value, $"Unsupported {typeof(TEnum).Name} value.");
}
