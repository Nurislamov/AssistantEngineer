using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Contracts.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class StandardReferenceDataFacade : IStandardReferenceDataFacade
{
    private readonly IInternalLoadStandardProvider _internalLoads;
    private readonly IDomesticHotWaterStandardProvider _domesticHotWaterStandards;
    private readonly ITb14ReferenceDataProvider _tb14;
    private readonly StandardTableCatalogService _standardTableCatalog;

    public StandardReferenceDataFacade(
        IInternalLoadStandardProvider internalLoads,
        IDomesticHotWaterStandardProvider domesticHotWaterStandards,
        ITb14ReferenceDataProvider tb14,
        StandardTableCatalogService standardTableCatalog)
    {
        _internalLoads = internalLoads;
        _domesticHotWaterStandards = domesticHotWaterStandards;
        _tb14 = tb14;
        _standardTableCatalog = standardTableCatalog;
    }

    public StandardTableCatalogResponse GetStandardTableCatalog() =>
        _standardTableCatalog.GetCatalog();

    public InternalLoadStandardLookupResponse GetInternalLoadStandard(
        RoomTypeDto roomType)
    {
        var row = _internalLoads.GetRow(roomType.ToDomain());

        return new InternalLoadStandardLookupResponse
        {
            TableKey = row.TableKey,
            Version = row.Version,
            RoomType = row.RoomType.ToContract(),
            SensibleHeatGainPerPersonW = row.SensibleHeatGainPerPersonW,
            LatentHeatGainPerPersonW = row.LatentHeatGainPerPersonW,
            EquipmentGainWPerM2 = row.EquipmentGainWPerM2,
            LightingGainWPerM2 = row.LightingGainWPerM2,
            MinimumVentilationLitersPerSecondM2 = row.MinimumVentilationLitersPerSecondM2,
            OccupantDensityPeoplePer100M2 = row.OccupantDensityPeoplePer100M2,
            Notes = row.Notes
        };
    }

    public DomesticHotWaterStandardLookupResponse GetDomesticHotWaterStandard(
        RoomTypeDto roomType)
    {
        var row = _domesticHotWaterStandards.GetRow(roomType.ToDomain());

        return new DomesticHotWaterStandardLookupResponse
        {
            TableKey = row.TableKey,
            Version = row.Version,
            RoomType = row.RoomType.ToContract(),
            LitersPerPersonDay = row.LitersPerPersonDay,
            ColdWaterTemperatureC = row.ColdWaterTemperatureC,
            HotWaterTemperatureC = row.HotWaterTemperatureC,
            DistributionLossFactor = row.DistributionLossFactor,
            StorageLossKWhPerDay = row.StorageLossKWhPerDay,
            CirculationLossKWhPerDay = row.CirculationLossKWhPerDay,
            Notes = row.Notes
        };
    }

    public Tb14VentilationStandardLookupResponse GetTb14VentilationStandard(
        RoomTypeDto roomType)
    {
        var row = _tb14.GetRow(roomType.ToDomain());

        return new Tb14VentilationStandardLookupResponse
        {
            TableKey = row.TableKey,
            Version = row.Version,
            RoomType = row.RoomType.ToContract(),
            OutdoorAirLitersPerSecondPerPerson = row.OutdoorAirLitersPerSecondPerPerson,
            OutdoorAirLitersPerSecondPerM2 = row.OutdoorAirLitersPerSecondPerM2,
            ExhaustAirChangesPerHour = row.ExhaustAirChangesPerHour,
            RecirculationAllowed = row.RecirculationAllowed,
            Notes = row.Notes
        };
    }
}