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

## ED-22A Conversation State Machine

ED-22A adds a code-first Telegram conversation layer over the same deterministic bot facade. A normal Consumer no
longer needs to know `/diagnose`: they can send only a displayed code such as `H5`, `h5`, `ошибка H5`, or `Gree H5`.
The bot then narrows the runtime catalog candidates with reply-keyboard choices:

```text
Code -> Brand -> EquipmentType -> DisplayContext -> Result
```

The conversation asks only for dimensions that are actually ambiguous. If a code has one brand, one equipment type,
or one display context, that step is skipped. If all dimensions are unique, the bot returns the result immediately.
If the code is not found, the bot replies in Russian with guidance to check the code or include a brand, for example
`Gree H5`.

The session is stored in Postgres table `TelegramConversationSessions` and references `TelegramUsers`. Session rows
carry the current state, current code, selected brand/type/display context, serialized candidate options,
`UpdatedAt`, and a long `ExpiresAt` horizon for future cleanup. ED-22A does not aggressively expire active sessions.

The reply keyboard always includes `🔎 Новый код`. Sending that button, `Новый код`, `/new`, `/reset`, or `/cancel`
clears the current session and prompts for a new code. Sending a new diagnostic-looking text code also starts a new
scenario even when the previous session was waiting for a button selection.

Consumer replies continue to use the public-safe formatter. Owner, Admin, and Engineer get the same conversation
flow but final results use the technical formatter, and `/admin ...` commands bypass any active session.

Telegram contact messages are accepted from any state. The phone number is saved on `TelegramUsers`; if a diagnostic
session is waiting for a brand/type/display-context selection, the bot preserves the session and repeats the active
prompt.

## Security And Runtime Boundaries

- No committed token or application setting containing a token.
- No new public API endpoint.
- No runtime catalog promotion or manual-verification promotion.
- No diagnostic case history, CRM/ServiceLead creation, web admin UI, photo/OCR, AI, RAG, vector search, or manual-PDF
  access is added by ED-22A.
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

## Telegram Delivery Transport

The separately reviewed delivery shell keeps the adapter deterministic and disabled by default. Production can use
Telegram Bot API polling when provider edge traffic prevents webhook delivery; the webhook endpoint remains an
optional fallback. Transport configuration and deployment helpers are documented in
[telegram-webhook-deployment.md](telegram-webhook-deployment.md). No committed secret, queue, or audit log is
introduced.

## ED-17C Access Policy

The adapter applies explicit deny lists before allow lists. `DeniedChatIds` and `DeniedUsernames` therefore win
even when the same identity is allowed. `/id` and `/whoami` can return setup identity only while
`EnableChatIdDiscovery=true`; the option is false by default and these commands never call the diagnostic facade.
See [telegram-operations-checklist.md](telegram-operations-checklist.md).
