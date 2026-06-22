# Error Knowledge V2 Localization and Repository Workflow

## Purpose

ED-24B separates diagnostic source material from user-facing localized text. ED-24C makes the localized error
knowledge repository-backed JSON under `data/equipment-diagnostics/error-knowledge/`. Telegram output remains Russian.

This workflow does not add dynamic translation, `/language`, a large import, attachments, runtime editing, NoSQL, or
a database-backed knowledge editor.

## Model

Each entry JSON file represents one `ErrorKnowledgeEntryV2` and identifies the diagnostic case, taxonomy, and source:

- `id`, `manufacturer`, `equipmentFamily`, `equipmentType`, `series`, `models`, and `code`;
- `signalType`, `displaySource`, `systemPart`, `severity`;
- `requiresQualifiedService`, nullable `canCustomerContinueOperation`, and `packageId`;
- source language, type, name, exact source meaning, and reference;
- optional `sourceReferences[]` for additional manuals/sources that confirm the same diagnostic meaning;
- confidence and verification status;
- creation/update timestamps.

Its `texts` array contains one `ErrorKnowledgeTextV2` per localized audience view:

- locale and audience;
- title, summary, and safety note;
- possible causes and bounded check steps;
- advice that must not be given to a customer;
- recommended action and source note;
- machine-translation/review flags and timestamps.

Current locale:

- `ru` for Telegram output.

The model accepts additional locale keys such as `en` and `uz`, but no user language preference or `/language`
command is exposed.

Current audiences:

- `Consumer`;
- `Installer`;
- `Engineer`.

Owner and Admin reuse the Engineer view. Audience selection does not change the ED-23R service/admin permission
matrix.

## Source and output language

The existing embedded runtime diagnostic catalog remains unchanged. The localized error-knowledge JSON is a separate
audience/output layer. Source language can be English, but raw English summaries, labels, safety paragraphs, and steps
are not copied directly into Russian Telegram messages.

The Telegram formatter selects `ru` text for the current audience. If no matching localized text exists, it renders
a controlled Russian fallback:

```text
Суть:
Техническое описание для этого кода пока не локализовано.
```

The fallback uses a generic Russian safety boundary. It does not print the raw English source summary, steps, safety
paragraphs, confidence/source labels, internal source identifiers, or source file names.

## GMV6 manual-backed catalog

ED-24E.1 replaces the initial generic GMV H5 seed with a manual-bound GMV6 catalog from `GC202001-I`. It contains
60 Indoor, 120 Outdoor, 37 Debugging, and 36 Status entries. Every entry retains the exact English table wording in
`sourceMeaning` and has separate Russian Consumer, Installer, and Engineer views.

H5 is manually verified as inverter-fan over-current protection. Its manual-derived cable, fan, blade/shaft, and
fan-drive-board checks are bounded to qualified service; Consumer text does not expose repair or live-electrical steps.

## Repository format and loading

The source-of-truth directory is:

```text
data/equipment-diagnostics/error-knowledge/{manufacturer}/{series}/{category}/{code}.json
```

The corrected H5 entry is `gree/gmv6/outdoor/h5.json`. JSON uses camelCase. Arrays such as `models`, `possibleCauses`, `checkSteps`,
and `doNotAdvise` must be present even when empty. Locale and audience values are case-sensitive.

`sourceReferences[]` is optional for backward compatibility. When present, it must be non-empty and each item must include:

- `sourceName`;
- optional `documentCode`;
- `sourceReference`;
- `sourceType`;
- `sourceLanguage`;
- `verificationStatus`;
- `confidence`;
- optional `manualId`;
- optional `packageId`;
- optional `notes`.

Existing single-source fields remain required. New imports should use `sourceReferences[]` only to preserve multiple reviewed source/manual references for the same diagnostic answer. They must not create duplicate production entries when code, equipment type, and meaning are the same.

ED-24F.1b uses this pattern for `GC202004-X` (`Service Manual - Multi Variable Air Conditioners Indoor Units`): 38 existing GMV6 indoor entries keep their original `GC202001-I` single-source fields and now carry two deterministic `sourceReferences[]` items, first the original GMV6 manual and then the GMV IDU manual. This does not change package or entry counts and does not ask Telegram users to choose a manual/source.

## ED-24F.1d diagnostic message quality

ED-24F.1d is a quality-only pass over existing GMV6 JSON and Telegram formatting. It does not add packages, entries,
manual sources, Telegram manual delivery, database tables, migrations, or environment variables.

Technical Telegram output must lead with diagnostic meaning, not metadata. For localized entries, the stable sections
are:

- `Суть:`
- `Что проверить:`
- `Важно:`
- `Ограничения вывода:`
- `Дальше:`

The formatter must not use `Категория`, `Уверенность`, `Источник`, package names, manual IDs, or source file paths as
the visible explanation when a localized entry exists. Metadata can support selection and validation, but it must not
replace the meaning of the diagnostic code.

Localized GMV6 text must avoid generic filler such as:

- `классифицирован`
- `диагностический вывод должен оставаться`
- `если подробная процедура не добавлена`
- `сообщение наладки`
- `штатную процедуру как основное содержание`

Do not write customer or technician guidance as "do not bypass/disable protection" when the message can instead state a
positive safe boundary such as no repeated starts before inspection. The output should not train users to think of
protection defeat as an available path.

U3 is the control example for this stage. It must describe GMV6 outdoor-unit power phase-sequence protection from the
reviewed GMV6 service manual: check the three-phase supply, wrong connection, phase loss, reverse phase/phase sequence,
and the detection circuit. It must not mention water-system causes or use metadata as the diagnostic meaning.

## ED-24G.0 Russian terminology polish

ED-24G.0 keeps the ED-24F.1d Telegram structure and polishes Russian technical terminology in the improved GMV6 batch.
User-facing Russian text should use normal Russian terms where the context is clear:

- `indoor PCB` -> `плата управления внутреннего блока`;
- `outdoor PCB` -> `плата управления наружного блока`;
- `IDU` -> `внутренний блок`;
- `ODU` -> `наружный блок`;
- `wired controller IDU` -> `проводной пульт внутреннего блока` or `проводной пульт` when the context is already clear.

Do not replace `GMV6`, diagnostic codes such as `U0`, `U3`, `C0`, `d1`, `o1`, `H5`, or electrical terminal/phase labels
such as `U/V/W` when they come from the manual.

## Taxonomy

Taxonomy prevents a future bulk catalog from becoming one flat code list. Choose the narrowest value supported by the
source; use `Unknown` rather than inferring unsupported detail.

- `equipmentFamily`: product platform — `VRF`, `SemiIndustrial`, `Split`, `Chiller`, `Controller`, or
  `EnergyMonitoring`.
- `equipmentType`: physical or logical equipment that owns the indication — `OutdoorUnit`, `IndoorUnit`,
  `WiredRemote`, `CentralController`, `Gateway`, `EnergyMeter`, `Chiller`, or `Unknown`.
- `signalType`: what the indication represents — `Fault`, `Protection`, `Warning`, `Status`, `Debug`,
  `Commissioning`, `Maintenance`, `Communication`, or `RemoteDisplay`.
- `displaySource`: where the code is observed — `OutdoorBoard`, `IndoorUnit`, `WiredRemote`, `CentralController`,
  `Gateway`, `Software`, or `Unknown`.
- `systemPart`: affected diagnostic area — `PowerSupply`, `Communication`, `Compressor`, `Inverter`,
  `RefrigerantCircuit`, `ProtectionCircuit`, `Sensor`, `Fan`, `WaterCircuit`, `Controller`, `Metering`, or `Unknown`.
- `severity`: operational consequence supported by evidence — `Info`, `Low`, `Medium`, `High`, `Critical`, or
  `Unknown`. Severity is not confidence and must not be inflated because a code looks alarming.
- `requiresQualifiedService`: whether the guidance requires a qualified service role.
- `canCustomerContinueOperation`: `true`, `false`, or `null` when the source does not establish safe continued
  operation.

Gree GMV6 H5 is classified from the manual as VRF / OutdoorUnit / Protection / OutdoorBoard / Fan / High. It is
`High` confidence and `ManualVerified`; the code does not by itself prove which component must be replaced.

## Package manifests

Package manifests live under:

```text
data/equipment-diagnostics/error-knowledge/packages/{packageId}.json
```

A manifest records batch identity, manufacturer/family/series compatibility, source and review context, intended
signal/equipment/display classifications, and optional `entryCountExpected`. It does not duplicate audience text.
Every entry references exactly one existing `packageId`.

The validator rejects duplicate package IDs, missing package references, manufacturer/family/series mismatches,
classifications outside a package's intended lists, and expected-count drift. Entry duplicate keys include
manufacturer, family, equipment type, series, sorted models, code, signal type, and display source. Runtime matching
still uses manufacturer/code plus optional series/model, so `Gree H5` behavior is unchanged.

To add a package:

1. Define the source/review boundary narrowly enough for one reviewer to assess coherently.
2. Add the package manifest before its entries.
3. Keep intended taxonomy lists explicit; an empty list means the package does not constrain that dimension.
4. Set `entryCountExpected` when the reviewed batch size is known.
5. Add entries in small reviewed groups and run both repository validation and publish smoke.

Current GMV6 package splits:

- `gree-gmv6-indoor-fault-codes`;
- `gree-gmv6-outdoor-fault-protection-codes`;
- `gree-gmv6-debugging-codes`;
- `gree-gmv6-status-codes`.

The EquipmentDiagnostics project embeds these files at build time. `JsonErrorKnowledgeLocalizationSource` lazily loads
the embedded resources, validates the complete set, and exposes it through `IErrorKnowledgeLocalizationSource`.
Invalid files or duplicate keys fail deterministically; they are never skipped. Owner and Admin continue to select
the Engineer audience.

The current allowed values are:

- locales: `ru`, `en`, `uz` (`uz` is accepted by the format but is not required or exposed yet);
- audiences: `Consumer`, `Installer`, `Engineer`;
- source types: `Manual`, `ManufacturerDocumentation`, `ServiceManual`, `CrossCheckedManuals`, `FieldObservation`, `SeededEngineeringKnowledge`;
- confidence: `Low`, `Medium`, `High`, `ManualVerified`;
- verification status: `UnverifiedSeed`, `PendingReview`, `Reviewed`, `Verified`, `ManualVerified`.

## Validation

Run:

```powershell
dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- verify-knowledge
.\scripts\equipment-diagnostics\verify-published-error-knowledge.ps1
```

The validator reports the repository-relative file and problem. It blocks:

- missing identity, source language, confidence, verification status, timestamps, or localized fields;
- empty or incomplete `sourceReferences[]` items when the array is present;
- missing or unknown taxonomy fields and missing service/package classification;
- invalid or incompatible package references, optional source-reference package links, duplicate package IDs, and package entry-count drift;
- missing Russian Consumer, Installer, or Engineer text;
- invalid locale, audience, confidence, or verification status;
- duplicate taxonomy entry keys and duplicate localized `locale/audience` views for the same taxonomy key;
- English UI labels in Russian text;
- unsafe Consumer instructions, using a documented initial English/Russian denylist;
- phone-number-like values, raw chat/platform user IDs, token/webhook-secret-like values, and callback payloads.

`doNotAdvise` is deliberately excluded from the unsafe-instruction scan because that field records explicit
prohibitions. All user-visible Consumer instructions remain subject to the denylist.

The publish smoke builds the API publish output, loads the EquipmentDiagnostics assembly in a separate process, and
verifies that the embedded Gree GMV6 H5 resource can be deserialized there. The backend Dockerfile must copy the
repository `data/equipment-diagnostics/error-knowledge/` directory into its build context before `dotnet publish`.

If knowledge loading or diagnostic formatting still fails at runtime, Telegram sends the controlled Russian fallback
`Диагностика временно недоступна. Попробуйте позже.`. Safe logs include update id, chat type, exception type, an
allowlisted/redacted exception message, and method-only stack context. Message text, phone, raw Telegram identifiers,
tokens, secrets, and callback payloads are not logged.

## Adding or updating an error code

1. Create one file under the manufacturer/series path, using camelCase fields and the existing H5 file as the template.
2. Select or create a narrow package manifest and classify family/type/signal/display/system part/severity without
   overclaiming.
3. Preserve the original source language and provenance. Never claim an exact meaning without a verified source.
4. Add Russian Consumer, Installer, and Engineer text. Consumer text must remain safe and must not instruct electrical,
   refrigerant, compressor, inverter, or protection work.
5. Set confidence and verification status conservatively. Mark unverified entries clearly.
6. Keep Owner/Admin behavior out of JSON; they reuse Engineer diagnostics by policy.
7. Run `verify-knowledge`, publish smoke, focused EquipmentDiagnostics tests, and the full repository quality gates.
8. Review the JSON diff like code. Runtime Telegram editing is not supported.

## Persistence decision

No EF migration is used. Production diagnostic knowledge is currently embedded static JSON, so a separate
database-backed localization table would introduce a second ownership/update path and new backup/restore surface.

If online editing becomes necessary, PostgreSQL `jsonb` or normalized tables should be considered before NoSQL.
