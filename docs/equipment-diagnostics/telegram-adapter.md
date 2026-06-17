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

## ED-22B Manual Service Phone Flow

ED-22B keeps the existing Telegram contact flow and adds a manual Consumer phone path for cases where the service
should call a different number than the Telegram account number. The main Consumer keyboard shows `🔎 Новый код`;
when no phone is saved it also shows `📞 Поделиться номером Telegram` with `request_contact=true` and
`✏️ Ввести другой номер`. When a phone is already saved, the main keyboard keeps `🔎 Новый код` and offers
`✏️ Изменить номер`.

Manual phone input uses the conversation state `WaitingForPhoneNumber`. The bot asks:

```text
Введите номер телефона для связи с сервисом.
Например: +998 90 123 45 67
```

Accepted input is normalized by removing spaces, parentheses, and hyphens, preserving a leading `+`, and requiring
7-15 digits. Invalid input keeps `WaitingForPhoneNumber` and asks for the `+998 90 123 45 67` format. `🔎 Новый код`,
`Новый код`, `/new`, `/reset`, or `/cancel` cancel phone input and start a fresh code prompt. If the user types a
diagnostic-looking code such as `H5` instead of a phone number, a new diagnostic scenario starts.

`TelegramUsers.PhoneNumberSource` records where the saved phone came from: `TelegramContact` for Telegram contact
messages and `Manual` for typed numbers. Telegram contact numbers are marked verified only when Telegram
`contact.user_id` matches `from.id`; manual numbers are saved as unverified. Phone numbers are not printed in
Consumer `/me`, `/admin users`, logs, or diagnostics. Admin lists may show only `phone=yes(manual)` or
`phone=yes(telegram)`.

Entering phone input does not delete the active diagnostic session. If the user was choosing brand, equipment type,
or display context, saving either a Telegram contact or manual phone restores the previous prompt when possible.
ED-22B still does not create ServiceLead/CRM records, web UI, photo/OCR, AI, RAG, vector search, or manual-PDF access.

## Telegram Command Menu

The adapter synchronizes the Telegram Bot API command menu on application startup when Telegram is enabled, a bot token is
configured, and `AssistantEngineer:EquipmentDiagnostics:Telegram:Commands:SyncOnStartup=true`. Startup continues if
Telegram rejects or times out the `setMyCommands` request; logs stay sanitized and must not include token, chat ID,
phone number, message text, or parameterized admin command text.

The global menu contains only safe public commands: `/start`, `/new`, `/phone`, `/me`, `/help`, `/history`, and
`/last`. It does not list `/admin_help`, `/admin users`, `/admin allow`, `/admin block`, `/admin role`, or any
parameterized admin command. `/new` uses the same flow as `🔎 Новый код` and asks
`Введите код ошибки, например: Gree H5.`. `/phone` opens the existing phone flow. `/admin_help` remains a hidden
manual/admin help command: Owner/Admin can reach it directly or from `/help`; Consumer and Engineer receive
`Команда недоступна.`.

This is a BotFather-style command menu only. It does not add a Telegram Mini App, web UI, CRM lead creation, AI,
RAG, vector search, photo/OCR, or manual-PDF access.

## ED-23A Diagnostic History

ED-23A stores a structured history case when the Telegram bot reaches a final diagnostic outcome. `Completed` cases
are created only after the final diagnostic result is shown. `NotFound` cases are created when the user enters a code
that is not found in the runtime catalog, so future knowledge-base work can see missed codes. Intermediate brand,
equipment-type, and display-context prompts do not create history cases.

History is private per Telegram user. `/history` shows the latest five cases for the current user only, and `/last`
shows that user's latest case. Consumer, Engineer, Owner, and Admin all see only their own history in ED-23A; global
admin case browsing is intentionally deferred.

Stored fields are structured and bounded: Telegram user database id, optional conversation session id, source,
status, role at creation, response mode, code, optional manufacturer/type/display context, short result summary,
candidate count, phone-saved flag, phone source, and timestamps. The store does not keep full Telegram message text,
full bot response text, phone number, raw chat id, Telegram user id, token, or webhook secret. `/last` uses the saved
short summary rather than a stored full response. For Consumer users, `/last` never prints a stored English technical
summary; it renders the public-safe Russian summary
`Сработала защита оборудования. Точное значение зависит от модели и места отображения ошибки.`. Engineer, Owner, and
Admin may see the saved short technical summary.

`CreatedAt` remains stored in UTC. Telegram history display converts `/history` and `/last` timestamps to
`AssistantEngineer:EquipmentDiagnostics:Telegram:DisplayTimeZone`, defaulting to `Asia/Tashkent`. Empty or invalid
time zone configuration falls back to `Asia/Tashkent`, logs a sanitized warning, and does not block bot startup or
history rendering. The relative labels `сегодня` and `вчера` are evaluated in the display time zone; older history
rows include local date and time, for example `15.06.2026 18:10`.

The main reply keyboard includes `🔎 Новый код` and `📋 История` after final or general bot replies. Choice prompts
for brand/type/display-context remain focused on choices plus `🔎 Новый код`. ED-23A does not add CRM/ServiceLead,
service tickets, engineer assignment, admin notifications, web UI, Mini App, photo/OCR, AI, RAG, vector search, or
manual-PDF access.

## Security And Runtime Boundaries

- No committed token or application setting containing a token.
- No new public API endpoint.
- No runtime catalog promotion or manual-verification promotion.
- No CRM/ServiceLead creation, web admin UI, photo/OCR, AI, RAG, vector search, or manual-PDF access is added by
  ED-23A.
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
