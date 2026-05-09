# Calculation Trace Foundation

## Purpose

Calculation trace is an internal engineering implementation for explainability of deterministic calculation paths.
It captures selected inputs, assumptions/defaults, intermediate values, formulas/conventions, warnings/diagnostics, final outputs, and participating modules.

This foundation is intended for future API/report/frontend explainability and engineering audit trail workflows.

## Trace Document Structure

`CalculationTraceDocument` includes:

- `TraceId`, optional `CalculationId`, `CalculationType`
- optional deterministic `CreatedTimestampUtc`
- `RootModule`
- ordered `Steps`
- `Summary`
- top-level `Assumptions`, `Warnings`, `Diagnostics`
- `Metadata`
- `SchemaVersion` (current `1.0`)

`CalculationTraceStep` includes:

- `StepId`, `ModuleKind`, `StepName`, deterministic `Sequence`
- `InputValues`, `IntermediateValues`, `OutputValues`
- optional `FormulaOrConventionLabel`
- `Assumptions`, `Warnings`, `Diagnostics`
- optional nested `ChildSteps`
- optional `DurationMilliseconds`

## Detail Levels

Supported `CalculationTraceDetailLevel` values:

- `None`: trace is disabled.
- `Summary`: module-level summary outputs and diagnostics.
- `Standard`: summary plus inputs, assumptions, warnings.
- `Detailed`: standard plus intermediate values and formula/convention labels.
- `Debug`: detailed plus extra internal debug fields when available.

## Supported Modules

Current foundation adapters cover:

- `Weather`
- `Solar`
- `ThermalTopology`
- `Iso52016`
- `MultiZone`
- `Ventilation`
- `Ground`
- `DomesticHotWater`
- `SystemEnergy`
- `Validation`
- `Reporting`
- `Generic`

## Assumptions, Warnings, Diagnostics Convention

- Existing diagnostics are mapped through compatibility mapper services.
- Existing warning strings are mapped into trace warnings.
- Existing assumptions are mapped into trace assumptions.
- Structured diagnostics keep severity/code/message/context/source when available.
- Diagnostics are deduplicated and deterministically ordered during sanitization.

## Value and Unit Convention

`CalculationTraceValue` is compact and serialization-friendly:

- `Key`, `Label`, `Value`, optional `Unit`
- `ValueKind` (`Input`, `Output`, `Intermediate`, `Assumption`, `Default`, `Coefficient`, `Formula`, `Diagnostic`, `Warning`, `Error`)
- optional `Source`, `DisplayFormat`, `Tags`

`CalculationTraceUnit` supports:

- `Symbol`
- optional `QuantityKind`
- optional `DisplayName`

## Sanitization and Compact Mode

`CalculationTraceSanitizer` performs deterministic normalization:

- removes null/empty noise
- trims and normalizes fields
- deduplicates and sorts diagnostics
- stabilizes numeric precision
- redacts path-like sensitive values by default
- summarizes overly large arrays for compact summary traces

## JSON Export

`CalculationTraceJsonExporter` provides stable JSON shape for future API/report/frontend integration:

- deterministic object schema for contracts
- explicit schema version field
- no generated trace artifacts are committed as part of foundation stage

## Known Limitations

- Calculation trace explains current internal engineering calculations only.
- Calculation trace is not a legal compliance certificate.
- Calculation trace is not external validation evidence.
- Calculation trace does not prove full standard compliance.
- Debug trace may be incomplete for modules not yet fully integrated.
- This stage does not provide PDF/HTML rendering unless an existing report system already adds it (no PDF/HTML rendering by default).
