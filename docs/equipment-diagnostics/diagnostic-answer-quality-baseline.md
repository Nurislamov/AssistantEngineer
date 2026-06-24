# Diagnostic Answer Quality Baseline

ED-24UX.1a defines the first Telegram answer-quality baseline for reviewed equipment diagnostics. It changes presentation rules, not manual-backed technical meanings.

## Answer Classes

Each reviewed answer should read consistently with its actual class:

- `ActiveFault`: active fault or emergency condition;
- `Protection`: protection state;
- `Warning`: warning that is not necessarily an active fault;
- `Status`: operating or controller status;
- `ParameterSetting`: configuration or parameter display;
- `ServiceReminder`: maintenance reminder;
- `DebugCommissioning`: commissioning, test, or debugging indication;
- `CommunicationFault`: communication or addressing fault;
- `PowerFault`: supply, phase, voltage, or power-bus fault;
- `SensorFault`: sensor or sensor-circuit fault;
- `BoardFault`: controller or circuit-board fault.

The class may be represented by existing reviewed signal, system-part, severity, title, and summary fields. ED-24UX.1a does not add a new persisted schema field.

## Quality Rules

1. Status and parameter-setting answers must not read like active emergency faults.
2. Warning and service-reminder answers should state that they are not active emergency faults when the reviewed manual-backed meaning supports that distinction.
3. A board-fault answer must not conclude that a board requires immediate replacement. Connection, power, related circuits, and available service checks come first.
4. Communication-fault checks prioritize wiring, terminals, addressing, and controller/display context.
5. Power-fault checks prioritize supply, phase, input, and voltage checks before board or inverter conclusions.
6. Generic filler is not acceptable in reviewed Telegram output, including phrases such as `код классифицирован по таблице`, `диагностический вывод должен оставаться`, and `если подробная процедура не добавлена`.
7. Reviewed Telegram output must not use generic protection wording such as `не обходить защиты` or `не отключать защиты`.
8. `Обратитесь в сервис` may follow concrete safe checks or appear in consumer-safe text, but it must not replace the whole diagnostic.
9. Canonical code casing comes from the selected JSON entry. Visually confusable codes may include a compact clarification.
10. Grouped same-meaning answers use a neutral title and next step. They must not present one series as the only applicable source.

## Current Baseline Examples

- GMV Mini `AJ`: service reminder/warning, not an active emergency fault.
- GMV Mini `n1`: parameter setting/status, not a fault.
- GMV Mini `C0`: communication fault.
- grouped GMV6/GMV Mini `C0`: neutral communication-fault answer with both applicable series.
- GMV6 `o1`: low power-bus voltage; `o1` is letter O plus digit 1 and is not numeric `01`.
- GMV6 `L1`: fan protection; `L1` is letter L plus digit 1.
- GMV6 `U3`: power/phase issue.
- GMV6 `U0`: compressor preheat warning/protection.
- GMV6 `H5`: inverter fan current protection.
- GMV6 `d1`: board fault without an immediate replacement conclusion.

These examples remain backed by the current repository entries. Future imports must apply the same rules without generalizing meanings across series or manuals.
