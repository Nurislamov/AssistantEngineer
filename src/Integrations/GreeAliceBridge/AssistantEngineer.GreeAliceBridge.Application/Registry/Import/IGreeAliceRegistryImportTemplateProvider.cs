using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

namespace AssistantEngineer.GreeAliceBridge.Application.Registry.Import;

public interface IGreeAliceRegistryImportTemplateProvider
{
    GreeAliceRegistryImportDraft GetTemplateDraft();
}
