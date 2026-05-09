# Engineering Reports Foundation

## Purpose

Engineering report generation in this stage is an internal engineering implementation that aggregates already calculated outputs into deterministic report documents.
The report layer does not recalculate thermal physics.
It combines summaries, assumptions, warnings, diagnostics and optional calculation trace appendix data.

## Supported report kinds

- `CalculationSummary`
- `AnnualEnergy`
- `HeatingCoolingLoad`
- `DomesticHotWater`
- `SystemEnergy`
- `Validation`
- `FullEngineeringCore`
- `Generic`

## Supported formats

- `Json`
- `Markdown`
- `PlainText`
- `HtmlPreview` (optional placeholder)
- `Pdf` (future placeholder only in this stage)

## Report document schema

`EngineeringReportDocument` contains:

- `ReportId`
- `ReportKind`
- `Title`
- optional `ProjectId` and `BuildingId`
- `GeneratedTimestampUtc` with deterministic override support for tests
- `SchemaVersion`
- `Sections`
- `Summaries`
- `Warnings`
- `Diagnostics`
- `Assumptions`
- `SourceCalculationIds`
- optional `TraceAppendix`
- `Metadata`

## Section model

`EngineeringReportSection` is deterministic and ordered.
Supported section kinds include:

- `ExecutiveSummary`
- `InputSummary`
- `Assumptions`
- `Warnings`
- `ValidationDiagnostics`
- `WeatherAndSolar`
- `ThermalZones`
- `HeatingCoolingLoads`
- `NaturalVentilation`
- `GroundBoundaries`
- `DomesticHotWater`
- `SystemEnergy`
- `FinalEnergy`
- `PrimaryEnergyAndCarbon`
- `CalculationTraceAppendix`
- `Limitations`
- `Metadata`

Each section can contain key values, tables, assumptions, diagnostics and child sections.
Charts are placeholders in this stage unless explicitly implemented.

## Generation request model

`EngineeringReportGenerationRequest` accepts optional module summaries and diagnostics:

- heating/cooling summary
- multi-zone summary
- natural ventilation summary
- ground summary
- domestic hot water summary
- system energy summary
- validation diagnostics
- optional `CalculationTraceDocument`

Partial report generation is supported.
If a report kind requests a section but source data is missing, the report remains valid and adds deterministic diagnostics instead of failing.

## JSON export

`EngineeringReportJsonExporter` returns a stable JSON payload and does not write local files by default.
Schema version and deterministic ordering are preserved by the report builder and serializer input model.

## Markdown export

`EngineeringReportMarkdownExporter` renders deterministic headings and tables.
Assumptions, warnings, diagnostics and limitations are visible in output.
Trace appendix is compact in non-detailed mode and fuller in detailed mode.

## Trace appendix integration

When `IncludeTraceAppendix` is enabled and trace is provided:

- trace summary metadata is included
- selected trace steps are listed
- trace assumptions/warnings/diagnostics are merged into report-level collections

When trace is not provided:

- report remains valid
- a deterministic informational diagnostic is added

## Diagnostic aggregation

`EngineeringReportDiagnosticAggregator` merges diagnostics from calculation contracts and trace contracts.
It deduplicates and sorts diagnostics deterministically by severity/module/code/message/context.
Assumptions and warnings remain separate from diagnostics.

## Future path

This stage establishes report generation foundation for future API and frontend explainability.
Production PDF/HTML rendering can be layered later without changing calculation contracts.

## Known limitations

- reports summarize current internal engineering calculations only;
- report is not a legal compliance certificate;
- report is not external validation evidence;
- report does not prove full standard compliance;
- PDF/HTML production rendering is not the focus of this stage unless already supported;
- charts are placeholders unless actually implemented.

