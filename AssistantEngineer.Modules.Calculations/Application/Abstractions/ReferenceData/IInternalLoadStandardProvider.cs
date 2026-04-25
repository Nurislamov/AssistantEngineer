using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;

public interface IInternalLoadStandardProvider
{
    InternalLoadStandardRow GetRow(RoomType roomType);
    IReadOnlyList<InternalLoadStandardRow> GetAll();
    string Version { get; }
}