# Equipment Diagnostics Telegram Adapter Skeleton

## ED-17A Scope

ED-17A adds a deterministic Telegram-like adapter skeleton over the existing `IEquipmentDiagnosticBotFacade`.
It is an internal application adapter, not a production Telegram deployment.

The flow is intentionally narrow:

```text
Telegram-like update -> deterministic parser -> bot facade -> plain-text formatter -> Telegram-like response
```

The adapter does not read runtime catalog files directly and cannot read staging, manual codebook, generated
preview, or verification artifacts. Only the existing bot facade can produce diagnostic decisions.

## Supported Messages

- `Gree H5`
- `H5` when the configured default manufacturer is `Gree`
- `Gree C5 outdoor`
- `Gree C5 indoor`
- `Gree F5 ODU`
- `Gree A0`
- `n6`
- `db`
- `/diagnose Gree H5`
- `/start`
- `/help`

The parser recognizes a small explicit set of equipment-side and display-context hints. It does not perform
natural-language inference. Controller model names `CE41`, `CE42`, and `CE52` are treated as unsupported model
identifiers rather than diagnostic fault codes.

## Configuration Boundary

`EquipmentDiagnosticTelegramOptions` is registered with transport disabled by default:

- `IsEnabled = false`
- optional allowed chat IDs and usernames
- maximum input/output message length
- optional default manufacturer and preferred language
- free-text forwarding disabled by default
- optional explicit-manufacturer requirement

No bot token option or secret is stored by this skeleton. Direct handler tests can enable the adapter with
in-memory options. The allowed-chat policy is a preliminary boundary only and is not claimed as complete
production authorization.

## Response Behavior

Responses are deterministic plain text with no Markdown parse mode:

- `Answer` keeps verification, source, confidence, safety, and bounded next steps visible.
- `ClarificationRequired` lists deterministic context options.
- `ReferenceOnly` explains that the indication is not a confirmed fault diagnosis.
- `NotFound` asks the operator to verify manufacturer, family, display context, and exact service manual.
- unsupported or out-of-scope input returns a safe help boundary.

The formatter prioritizes the safety boundary and applies deterministic message shortening.

## Security And Runtime Boundaries

- No production webhook endpoint.
- No long-polling or hosted transport service.
- No external Telegram network calls.
- No committed token or application setting containing a token.
- No new public API endpoint.
- No runtime catalog promotion or manual-verification promotion.
- Safety protections remain active during diagnosis.

Runtime catalog data remains the only final diagnosis source. Review-only sources remain non-runtime.

## Verification

Focused adapter tests:

```powershell
dotnet test AssistantEngineer.sln --filter EquipmentDiagnosticTelegram
```

The tests cover parsing, facade delegation, disabled and allowed-chat behavior, deterministic formatting,
message limits, safety/provenance/verification visibility, and internal-source disclosure guards.

## ED-17B Future Work

A separately reviewed ED-17B may add one production transport choice, secret management, deployment
configuration, stronger authorization, rate controls, and an audit/logging policy. None of those capabilities
are active in ED-17A.

## ED-17B Webhook Transport

ED-17B adds the separately reviewed, disabled-by-default webhook shell and outbound send abstraction. The endpoint,
secret-header boundary, safe configuration, and deployment helper are documented in
[telegram-webhook-deployment.md](telegram-webhook-deployment.md). No long polling, committed secret, database,
queue, or audit log is introduced.
