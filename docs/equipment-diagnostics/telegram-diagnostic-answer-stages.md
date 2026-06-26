# Telegram Diagnostic Answer Stages

ED-24DG.1 defines the staged shape expected from Telegram equipment-diagnostic answers. This is a presentation contract for future answer writing and test coverage; it does not add diagnostic cards or change source meanings.

## Core Stages

1. Identification
   - Show manufacturer, code, and known series, family, equipment side, or display context.
   - Preserve official code casing and add compact clarification for visually confusable codes when needed.

2. Meaning
   - Explain briefly what the code indicates.
   - Avoid unsupported root-cause certainty when the source only identifies a protection, state, or symptom.

3. Safety
   - Say whether operation should stop or remain paused.
   - Warn against repeated resets when the condition persists.
   - State when qualified service is required.

4. Safe User Checks
   - Keep user-facing checks visual and non-invasive.
   - Acceptable checks include recording the code, noting when it appears, checking visible dirt, ice, blockage, controller state, or model/context labels when safe.
   - Do not tell consumers to open cabinets, disassemble equipment, measure live circuits, bypass protections, short contacts, replace boards or components, or force operation.

5. Installer Checks
   - Qualified installation or service checks may include communication wiring, addressing, settings, installation conditions, and power-off connector checks.
   - Frame these as checks for qualified people, not consumer instructions.

6. Engineer Or Service Checks
   - Advanced checks may include measurements, phases, sensors, boards, motors, compressor circuits, or refrigerant-side procedures.
   - Always frame them as qualified service work under the applicable service procedure.

7. Next Action
   - Tell the user what to do next: create or continue a service request, provide model/context, check the exact manual, or call qualified service.
   - The next action should follow the meaning, safety boundary, and checks instead of replacing them.

## Current Telegram Section Mapping

Technical Telegram answers use these section labels:

- `Диагностика ...` and the title line cover identification.
- `Суть:` covers meaning.
- `Что проверить:` covers installer and service checks.
- `Важно:` covers safety.
- `Ограничения вывода:` captures limits and non-actions when present.
- `Дальше:` covers next action.

Consumer Telegram answers use equivalent plain-language sections:

- `Код оборудования:` and the title line cover identification.
- `Возможное значение:` covers meaning.
- `Что можно сделать безопасно:` covers safety and user-safe checks.
- `Рекомендованное действие:` and `Для сервиса:` cover next action and handoff.

Future answer changes should preserve these stages or a clearly equivalent section structure.
