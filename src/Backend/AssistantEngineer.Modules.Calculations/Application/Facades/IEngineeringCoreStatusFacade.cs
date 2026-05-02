using AssistantEngineer.Modules.Calculations.Application.Contracts.CoreStatus;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IEngineeringCoreStatusFacade
{
    Result<EngineeringCoreV1StatusResponse> GetEngineeringCoreV1Status();

    Result<EngineeringCoreV1DiagnosticsCatalogResponse> GetEngineeringCoreV1DiagnosticsCatalog();
}
