using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IProtectedEndpointPermissionEvaluator
{
    ProtectedEndpointPermissionEvaluationResult Evaluate(Permission permission);
}
