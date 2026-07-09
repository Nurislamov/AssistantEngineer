using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

namespace AssistantEngineer.GreeAliceBridge.Application.Registry.Import;

public interface IGreeAliceRegistryImportValidator
{
    GreeAliceRegistryImportValidationResult Validate(GreeAliceRegistryImportDraft? draft);
}
