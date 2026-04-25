using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Contracts.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class StandardTableCatalogService
{
    private readonly IInternalLoadStandardProvider _internalLoads;
    private readonly IDomesticHotWaterStandardProvider _dhw;
    private readonly ITb14ReferenceDataProvider _tb14;

    public StandardTableCatalogService(
        IInternalLoadStandardProvider internalLoads,
        IDomesticHotWaterStandardProvider dhw,
        ITb14ReferenceDataProvider tb14)
    {
        _internalLoads = internalLoads;
        _dhw = dhw;
        _tb14 = tb14;
    }

    public StandardTableCatalogResponse GetCatalog() =>
        new()
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Items =
            [
                new StandardTableCatalogItemResponse
                {
                    TableKey = "internal-loads",
                    DisplayName = "Internal Loads",
                    Version = _internalLoads.Version,
                    Description = "Seed reference rows for people, equipment, lighting and minimum ventilation defaults by room type."
                },
                new StandardTableCatalogItemResponse
                {
                    TableKey = "dhw",
                    DisplayName = "Domestic Hot Water",
                    Version = _dhw.Version,
                    Description = "Seed domestic hot-water default rows by room type."
                },
                new StandardTableCatalogItemResponse
                {
                    TableKey = "tb14",
                    DisplayName = "TB14 Ventilation",
                    Version = _tb14.Version,
                    Description = "Seed ventilation/outdoor-air reference rows by room type."
                }
            ]
        };
}