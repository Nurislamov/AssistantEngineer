using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;

public interface IStandardCalculationDisclosureFactory
{
    StandardCalculationDisclosure CreateThermalZonesDisclosure();

    StandardCalculationDisclosure CreateNaturalVentilationEn16798Disclosure();

    StandardCalculationDisclosure CreateGroundIso13370Disclosure();

    StandardCalculationDisclosure CreateDomesticHotWaterIso12831Disclosure();

    StandardCalculationDisclosure CreateSystemEnergyEn15316Disclosure();
}
