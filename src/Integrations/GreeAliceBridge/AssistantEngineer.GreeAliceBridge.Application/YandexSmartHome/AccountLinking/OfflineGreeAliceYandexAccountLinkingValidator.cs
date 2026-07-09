using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;

public sealed class OfflineGreeAliceYandexAccountLinkingValidator : IGreeAliceYandexAccountLinkingValidator
{
    private static readonly Regex MacLikePattern = new(
        "(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public GreeAliceYandexAccountLinkingValidationResult ValidateTemplate(GreeAliceYandexAccountLinkingTemplate? template)
    {
        List<GreeAliceYandexAccountLinkingValidationIssue> issues = [];

        if (template is null)
        {
            issues.Add(CreateIssue("template.null", "Account linking template is required.", "template"));

            return CreateResult(issues, failClosed: true);
        }

        issues.AddRange(ValidateSession(template.Session).Issues);
        issues.AddRange(ValidateBinding(template.ActiveBinding).Issues);
        issues.AddRange(ValidateScope(template.RegistryScope).Issues);
        ValidateUnlink(template.UnlinkRequest, template.UnlinkResult, issues);

        return CreateResult(issues, failClosed: issues.Count > 0);
    }

    public GreeAliceYandexAccountLinkingValidationResult ValidateSession(GreeAliceYandexAccountLinkingSession? session)
    {
        List<GreeAliceYandexAccountLinkingValidationIssue> issues = [];

        if (session is null)
        {
            issues.Add(CreateIssue("session.null", "Account linking session is required.", "session"));

            return CreateResult(issues, failClosed: true);
        }

        if (session.Mode != GreeAliceYandexAccountLinkingMode.OfflineTemplate)
        {
            issues.Add(CreateIssue("session.mode", "Only offline-template account linking mode is allowed.", "session.mode"));
        }

        ValidateRecordSafety(session.LinkingSessionId, session.IsMasked, session.IsDummyOrTemplate, "session.linkingSessionId", issues);
        ValidateMaskedYandexUser(session.YandexUserReference, "session.yandexUserReference", issues);
        ValidateDummyReference(session.BridgeAccountReference, "session.bridgeAccountReference", issues);
        ValidateDummyReference(session.RegistryScopeReference, "session.registryScopeReference", issues);

        return CreateResult(issues, failClosed: issues.Count > 0);
    }

    public GreeAliceYandexAccountLinkingValidationResult ValidateBinding(GreeAliceYandexUserBinding? binding)
    {
        List<GreeAliceYandexAccountLinkingValidationIssue> issues = [];
        bool failClosed = false;

        if (binding is null)
        {
            issues.Add(CreateIssue("binding.unknown", "Unknown Yandex user binding is fail-closed.", "binding"));

            return CreateResult(issues, failClosed: true);
        }

        ValidateMaskedYandexUser(binding.YandexUserReference, "binding.yandexUserReference", issues);
        ValidateDummyReference(binding.BridgeAccountReference, "binding.bridgeAccountReference", issues);
        ValidateDummyReference(binding.RegistryScopeReference, "binding.registryScopeReference", issues);

        if (!binding.IsMasked)
        {
            issues.Add(CreateIssue("binding.unmasked", "Yandex user binding must be masked.", "binding"));
        }

        if (!binding.IsDummyOrTemplate)
        {
            issues.Add(CreateIssue("binding.notTemplate", "Yandex user binding must be dummy/template data.", "binding"));
        }

        if (!binding.IsActive || binding.UnlinkedAtUtc is not null)
        {
            issues.Add(CreateIssue("binding.inactive", "Inactive or unlinked binding is fail-closed.", "binding"));
            failClosed = true;
        }

        return CreateResult(issues, failClosed || issues.Count > 0);
    }

    public GreeAliceYandexAccountLinkingValidationResult ValidateScope(GreeAliceBridgeAccountScope? scope)
    {
        List<GreeAliceYandexAccountLinkingValidationIssue> issues = [];

        if (scope is null)
        {
            issues.Add(CreateIssue("scope.missing", "Registry scope binding is required.", "scope"));

            return CreateResult(issues, failClosed: true);
        }

        ValidateDummyReference(scope.BridgeAccountReference, "scope.bridgeAccountReference", issues);
        ValidateDummyReference(scope.RegistryScopeReference, "scope.registryScopeReference", issues);

        if (!scope.IsMasked || !scope.IsDummyOrTemplate)
        {
            issues.Add(CreateIssue("scope.notTemplate", "Registry scope must be masked dummy/template data.", "scope"));
        }

        string[] scopeValues =
        [
            .. scope.AllowedHomeIds,
            .. scope.AllowedDeviceIds,
            .. scope.AllowedVrfGatewayIds,
            .. scope.AllowedVrfChildUnitIds
        ];

        if (scopeValues.Length == 0)
        {
            issues.Add(CreateIssue("scope.empty", "Registry scope must be explicit.", "scope"));
        }

        foreach (string value in scopeValues)
        {
            if (value is "*" or "all" or "global")
            {
                issues.Add(CreateIssue("scope.globalWildcard", "Registry scope must not use a global wildcard.", "scope"));
            }

            ValidateDummyReference(value, "scope.value", issues);
        }

        return CreateResult(issues, failClosed: issues.Count > 0);
    }

    private static void ValidateUnlink(
        GreeAliceYandexAccountUnlinkRequest request,
        GreeAliceYandexAccountUnlinkResult result,
        List<GreeAliceYandexAccountLinkingValidationIssue> issues)
    {
        ValidateMaskedYandexUser(request.YandexUserReference, "unlinkRequest.yandexUserReference", issues);
        ValidateDummyReference(request.BridgeAccountReference, "unlinkRequest.bridgeAccountReference", issues);
        ValidateDummyReference(request.RegistryScopeReference, "unlinkRequest.registryScopeReference", issues);

        if (!request.IsDummyOrTemplate || !result.IsDummyOrTemplate)
        {
            issues.Add(CreateIssue("unlink.notTemplate", "Unlink boundary must remain dummy/template data.", "unlink"));
        }

        if (result.DeletedSecrets || result.DeletedTokens || result.RealTokenStorageImplemented)
        {
            issues.Add(CreateIssue("unlink.realTokenState", "Unlink result must not claim real token or secret storage.", "unlinkResult"));
        }
    }

    private static void ValidateMaskedYandexUser(
        string value,
        string path,
        List<GreeAliceYandexAccountLinkingValidationIssue> issues)
    {
        if (!value.StartsWith("masked-yandex-user-", StringComparison.Ordinal))
        {
            issues.Add(CreateIssue("yandexUser.unmasked", "Yandex user reference must be masked.", path));
        }

        ValidateNoUnsafeMaterial(value, path, issues);
    }

    private static void ValidateDummyReference(
        string value,
        string path,
        List<GreeAliceYandexAccountLinkingValidationIssue> issues)
    {
        if (!IsDummyOrTemplateReference(value))
        {
            issues.Add(CreateIssue("reference.notTemplate", "Reference must use dummy/template material.", path));
        }

        ValidateNoUnsafeMaterial(value, path, issues);
    }

    private static void ValidateRecordSafety(
        string value,
        bool isMasked,
        bool isDummyOrTemplate,
        string path,
        List<GreeAliceYandexAccountLinkingValidationIssue> issues)
    {
        if (!isMasked || !isDummyOrTemplate)
        {
            issues.Add(CreateIssue("record.notTemplate", "Record must be masked dummy/template data.", path));
        }

        ValidateDummyReference(value, path, issues);
    }

    private static void ValidateNoUnsafeMaterial(
        string value,
        string path,
        List<GreeAliceYandexAccountLinkingValidationIssue> issues)
    {
        if (MacLikePattern.IsMatch(value))
        {
            issues.Add(CreateIssue("value.macLike", "Value must not contain hardware-like identifiers.", path));
        }

        if (ContainsSensitiveMarker(value))
        {
            issues.Add(CreateIssue("value.sensitive", "Value must not contain sensitive material.", path));
        }

        if (LooksLikeRealIdentifier(value))
        {
            issues.Add(CreateIssue("value.realLike", "Value must not contain real-looking user or account identifiers.", path));
        }
    }

    private static bool IsDummyOrTemplateReference(string value)
    {
        return value.StartsWith("dummy-", StringComparison.Ordinal)
            || value.StartsWith("template-", StringComparison.Ordinal)
            || value.StartsWith("masked-yandex-user-", StringComparison.Ordinal);
    }

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "cred" + "ential",
            "sec" + "ret",
            "tok" + "en",
            "auth" + "code",
            "client" + "Secret",
            "pass" + "word"
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeRealIdentifier(string value)
    {
        string[] markers =
        [
            "real-" + "yandex-user-",
            "real-" + "bridge-account-",
            "real-" + "account-",
            "real-" + "device-"
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static GreeAliceYandexAccountLinkingValidationResult CreateResult(
        IReadOnlyList<GreeAliceYandexAccountLinkingValidationIssue> issues,
        bool failClosed)
    {
        return new GreeAliceYandexAccountLinkingValidationResult(issues.Count == 0, failClosed, issues);
    }

    private static GreeAliceYandexAccountLinkingValidationIssue CreateIssue(string code, string message, string path)
    {
        return new GreeAliceYandexAccountLinkingValidationIssue(code, message, "error", path);
    }
}
