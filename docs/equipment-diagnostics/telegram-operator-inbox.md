# Telegram Operator Inbox

ED-24OPS.2 adds a closed Telegram bridge between private user messages and an Owner-only operator group.

## Configuration

Default state is disabled:

```env
TELEGRAM_OPERATOR_INBOX_ENABLED=false
TELEGRAM_OPERATOR_CHAT_ID=
TELEGRAM_OPERATOR_LOG_DIAGNOSTICS=false
```

Docker Compose maps these to:

- `AssistantEngineer__EquipmentDiagnostics__Telegram__OperatorInbox__Enabled`
- `AssistantEngineer__EquipmentDiagnostics__Telegram__OperatorInbox__ChatId`
- `AssistantEngineer__EquipmentDiagnostics__Telegram__OperatorInbox__LogDiagnostics`

To discover the group chat id, add the bot to the intended group and have an Owner send `/operator_chat_id` or
`/chatid` in that group. Non-Owner users receive `Доступ ограничен.`. In private chat the bot replies that the command
must be sent in the operator group. The command never writes or mutates environment settings.

## User-To-Operator Flow

When the inbox is enabled and a user sends a private free-text/support message that is not handled as a normal
command, diagnostic answer, library delivery, inline button click, polling internal, `/start`, `/history`, `/last`, or
active `/manual_bind` upload, the bot creates or reuses an open thread and sends an operator card:

```text
📩 Обращение #<ThreadId>

Пользователь: <DisplayName> (@username)
Роль: <Role>
ChatId: <chatId>
Доступ к библиотеке: да/нет
Время: <UTC readable>

Сообщение:
<text>
```

The card deliberately omits Telegram `file_id`, `file_unique_id`, internal source paths, secrets, tokens, full bot
diagnostic answers, and manual metadata. Photo, video, document, and voice messages may be mirrored to the operator
group with Telegram `copyMessage` only from the user's private chat to the configured operator group. `forwardMessage`
is not used. If copy fails, the group receives a short `Вложение не удалось скопировать.` note.

## Owner Reply Bridge

Owner replies in the configured operator group by using Telegram reply on the operator card or copied mirrored
message. The bot checks:

- chat id equals `TELEGRAM_OPERATOR_CHAT_ID`;
- sender resolves to an enabled, unblocked `Owner` in `TelegramUsers`;
- the replied-to operator message is linked to an inbox thread;
- the Owner reply is text.

If valid, the bot sends the original private user:

```text
Ответ специалиста:
<owner text>
```

It then persists the Owner reply, marks the thread `Answered`, updates `LastOwnerReplyAt`, and confirms in the
operator group: `Ответ отправлен пользователю.`

Unknown reply targets get `Не удалось определить получателя. Ответьте reply-сообщением на карточку обращения.`
Non-text replies get `Пока поддерживается только текстовый ответ.`

## Persistence

Migration `AddTelegramOperatorInbox` adds:

- `TelegramOperatorInboxThreads`
- `TelegramOperatorInboxMessages`

Indexes cover thread lookup, user message lookup, operator message/reply lookup, and creation time. Production DI
uses `EfTelegramOperatorInboxStore`; module tests and non-infrastructure wiring use `InMemoryTelegramOperatorInboxStore`.

## Security Boundaries

- Only the configured operator group is handled as the operator bridge.
- Operator group messages are not treated as ordinary user diagnostic sessions.
- Only Owner can discover the group id through the command or send bridged replies.
- Successful diagnostics are not mirrored unless `TELEGRAM_OPERATOR_LOG_DIAGNOSTICS=true`, and then only a short event
  is mirrored.
- Library/manual document delivery to users still uses protected `sendDocument(file_id)` and does not use
  `copyMessage`.
