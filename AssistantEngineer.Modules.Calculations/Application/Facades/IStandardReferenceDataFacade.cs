using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IStandardReferenceDataFacade
{
    StandardTableCatalogResponse GetStandardTableCatalog();

    InternalLoadStandardLookupResponse GetInternalLoadStandard(
        RoomTypeDto roomType);

    DomesticHotWaterStandardLookupResponse GetDomesticHotWaterStandard(
        RoomTypeDto roomType);

    Tb14VentilationStandardLookupResponse GetTb14VentilationStandard(
        RoomTypeDto roomType);
}