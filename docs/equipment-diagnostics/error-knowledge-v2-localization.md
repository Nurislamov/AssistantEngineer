# Error Knowledge V2 Localization and Repository Workflow

## Purpose

ED-24B separates diagnostic source material from user-facing localized text. ED-24C makes the localized error
knowledge repository-backed JSON under `data/equipment-diagnostics/error-knowledge/`. Telegram output remains Russian.

This workflow does not add dynamic translation, `/language`, a large import, attachments, runtime editing, NoSQL, or
a database-backed knowledge editor.

## Model

Each JSON file represents one `ErrorKnowledgeEntryV2` and identifies the diagnostic case and its source:

- `id`, `manufacturer`, `equipmentType`, `series`, `models`, and `code`;
- source language, type, name, and reference;
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
Техническое описание пока не локализовано. Источник: <безопасная метка источника>.
```

The fallback uses a generic Russian safety boundary and Russian confidence/source labels. It does not print the raw
English source summary, steps, safety paragraphs, or internal source identifiers.

## Initial localized entry

The foundation includes Russian Consumer, Installer, and Engineer views for Gree GMV H5. The technical text treats
H5 as a preliminary protection signal, not as proof of a compressor, inverter, or board failure. It requires:

- confirmation of the installed model, GMV series, controller, and displayed code;
- qualified power-supply checks;
- bounded visual inspection of protection wiring, connectors, and sensors;
- verification against the exact installed-model service manual;
- no protection bypass or unsupported forced operation.

The entry remains low-confidence and unreviewed because the current source is `SeededEngineeringKnowledge /
UnverifiedSeed`.

## Repository format and loading

The source-of-truth directory is:

```text
data/equipment-diagnostics/error-knowledge/{manufacturer}/{series}/{code}.json
```

The initial entry is `gree/gmv/h5.json`. JSON uses camelCase. Arrays such as `models`, `possibleCauses`, `checkSteps`,
and `doNotAdvise` must be present even when empty. Locale and audience values are case-sensitive.

The EquipmentDiagnostics project embeds these files at build time. `JsonErrorKnowledgeLocalizationSource` lazily loads
the embedded resources, validates the complete set, and exposes it through `IErrorKnowledgeLocalizationSource`.
Invalid files or duplicate keys fail deterministically; they are never skipped. Owner and Admin continue to select
the Engineer audience.

The current allowed values are:

- locales: `ru`, `en`, `uz` (`uz` is accepted by the format but is not required or exposed yet);
- audiences: `Consumer`, `Installer`, `Engineer`;
- confidence: `Low`, `Medium`, `High`, `ManualVerified`;
- verification status: `UnverifiedSeed`, `PendingReview`, `Reviewed`, `Verified`.

## Validation

Run:

```powershell
dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- verify-knowledge
```

The validator reports the repository-relative file and problem. It blocks:

- missing identity, source language, confidence, verification status, timestamps, or localized fields;
- missing Russian Consumer, Installer, or Engineer text;
- invalid locale, audience, confidence, or verification status;
- duplicate `manufacturer/equipmentType/series/code/locale/audience` keys;
- English UI labels in Russian text;
- unsafe Consumer instructions, using a documented initial English/Russian denylist;
- phone-number-like values, raw chat/platform user IDs, token/webhook-secret-like values, and callback payloads.

`doNotAdvise` is deliberately excluded from the unsafe-instruction scan because that field records explicit
prohibitions. All user-visible Consumer instructions remain subject to the denylist.

## Adding or updating an error code

1. Create one file under the manufacturer/series path, using camelCase fields and the existing H5 file as the template.
2. Preserve the original source language and provenance. Never claim an exact meaning without a verified source.
3. Add Russian Consumer, Installer, and Engineer text. Consumer text must remain safe and must not instruct electrical,
   refrigerant, compressor, inverter, or protection work.
4. Set confidence and verification status conservatively. Mark unverified entries clearly.
5. Keep Owner/Admin behavior out of JSON; they reuse Engineer diagnostics by policy.
6. Run `verify-knowledge`, focused EquipmentDiagnostics tests, and the full repository quality gates.
7. Review the JSON diff like code. Runtime Telegram editing is not supported.

## Persistence decision

No EF migration is used. Production diagnostic knowledge is currently embedded static JSON, so a separate
database-backed localization table would introduce a second ownership/update path and new backup/restore surface.

If online editing becomes necessary, PostgreSQL `jsonb` or normalized tables should be considered before NoSQL.
