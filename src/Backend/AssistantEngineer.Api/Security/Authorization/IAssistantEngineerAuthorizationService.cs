namespace AssistantEngineer.Api.Security.Authorization;

public interface IAssistantEngineerAuthorizationService
{
    AssistantEngineerAuthorizationDecision AuthorizePilotPermission(string requiredPermission);
}
