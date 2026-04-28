using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;

public interface ITb14ReferenceDataProvider
{
    Tb14VentilationStandardRow GetRow(RoomType roomType);
    IReadOnlyList<Tb14VentilationStandardRow> GetAll();
    string Version { get; }
}