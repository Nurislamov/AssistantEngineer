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

## ED-23B Service Requests

ED-23B adds a deliberately small service-request foundation, not a CRM. `🛠 Нужен мастер` and hidden `/request`
create a PostgreSQL-backed `TelegramServiceRequests` row from the current user's latest diagnostic case when a phone
is saved. `/requests` is in the global command menu and shows the latest five requests owned by the current user.
Active `New` or `InProgress` requests are unique per diagnostic case.

The optional service group is configured with
`AssistantEngineer:EquipmentDiagnostics:Telegram:ServiceRequests:NotificationChatId`, or
`TELEGRAM_SERVICE_REQUESTS_CHAT_ID` in Docker Compose. Request creation succeeds when the group is absent or message
delivery fails. Notifications contain the diagnostic title, safe username label, phone-saved state, phone source,
local time, and Russian status. They do not contain the full phone number, raw chat id, Telegram user id, incoming
text, or bot response.

ED-23B intentionally excludes assignment, engineer status buttons, a global admin queue, CRM workflow, web UI,
Mini App, and photo/OCR.

## ED-23C Service Queue Actions

ED-23C adds text-command queue operations in the configured service Telegram group. It does not add inline callback
buttons. The supported group commands are `/queue`, `/take <id>`, `/assign <id> @username`, `/done <id>`,
`/cancel_request <id>`, `/request_status <id>`, and `/contact <id>`. Telegram group suffixes such as
`/take@EquipmentBot 2` are accepted. These commands are intentionally excluded from the global BotFather menu.

An engineer must first open the bot in a private chat and send `/start`. Owner/Admin then grants the `Engineer`
role. Group commands resolve the operator by Telegram sender identity, not by the service group's chat id. Engineer,
Admin, and Owner can view the queue and take work. Only Owner/Admin can assign another operator. Engineer can close,
cancel, or request contact only for a request assigned to that engineer; Owner/Admin can perform those actions for
any request.

Assignment and status updates persist `AssignedTelegramUserId`, assignment time and actor, status-update time and
actor, `UpdatedAt`, and `ClosedAt` for terminal states. Taking or assigning a request sends the customer a private
status update and attempts to deliver the saved phone privately to the assigned operator. Resolution and
cancellation also notify the customer privately. Telegram delivery failure never rolls back the committed request
state.

The full customer phone is available only through an authorized private bot message to the assigned Engineer or
Owner/Admin. Group responses show only `Телефон: сохранён` or `Телефон: не сохранён`. Logs remain sanitized and do
not include phone values, raw chat ids, Telegram user ids, incoming command text, or outgoing message text.

ED-23C still excludes Mini App, web UI, inline buttons, comments, photos/OCR, SLA, priorities, and full CRM workflow.

## ED-23D Inline Service Actions

ED-23D adds Telegram inline keyboards to new service-request notifications and active `/queue` results. New request
cards show `Взять в работу`, `Назначить`, `Статус`, `Контакт`, and `Отменить`. Queue output remains readable text and
adds compact action rows for up to five active requests. Existing ED-23C text commands remain supported as fallback.

Telegram `callback_query` updates are accepted by polling and webhook delivery, pass through the same durable update
offset, and are acknowledged with `answerCallbackQuery` so the Telegram spinner is cleared. Callback data uses short
deterministic values such as `sr:t:12` and `sr:as:12:34`; it contains no phone, username, chat id, token, or secret and
stays within Telegram's 64-byte limit. Unknown or malformed callback data is acknowledged and handled safely.

`Назначить` is available only to Owner/Admin and opens an inline list of enabled, non-blocked `Engineer`, `Admin`, and
`Owner` users from `TelegramUsers`. Selecting a user performs the same assignment, customer notification, and private
contact delivery as `/assign`. All other button actions reuse ED-23C authorization rules. Button visibility never
grants access.

Full customer phone values are sent only to an authorized private chat. Service-group messages and callback payloads
never include them. ED-23D does not edit old request cards or store Telegram notification message ids; those are
deferred. It also does not add Mini App, web UI, button-based user/role administration, comments, photos/OCR, SLA, or
priorities.

## ED-23D.1 Live Service Request Cards

ED-23D.1 stores `NotificationChatId`, `NotificationMessageId`, `NotificationSentAt`, and `NotificationUpdatedAt` on
each service request. The service-group card is rendered from current database state and updated with
`editMessageText` after take, assignment, resolution, cancellation, status refresh, and the equivalent fallback text
commands. A successful edit does not post a separate group status message.

Buttons follow the current state. `New` offers take, assign, contact, status, and cancel. `InProgress` offers contact,
close, assign, status, and cancel. Terminal cards retain only status. The assign action edits the same card into an
engineer picker with a `Назад` button; Back restores the current request card.

Every callback is acknowledged with a short `answerCallbackQuery` message. Contact delivery remains private and never
puts the full phone in the service group. If Telegram cannot edit a stored message, committed request state is kept and
the bot sends the current safe card as fallback, storing the replacement message id when Telegram returns one.
`message is not modified` is treated as an already-current card, not as a fallback condition.

The `AddTelegramServiceRequestNotificationMessage` migration is required. No new environment variable is introduced.
Commands remain operational fallback. ED-23D.1 does not include button-based user/role administration; that remains
outside this change.

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
