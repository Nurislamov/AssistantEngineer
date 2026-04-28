using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;

public interface IDomesticHotWaterStandardProvider
{
    DomesticHotWaterStandardRow GetRow(RoomType roomType);
    IReadOnlyList<DomesticHotWaterStandardRow> GetAll();
    string Version { get; }
}