# Error Knowledge V2 Localization Foundation

## Purpose

ED-24B separates diagnostic source material from user-facing localized text. Existing runtime knowledge remains in
the embedded JSON catalog, where manufacturer/manual source material may be English. Telegram output remains Russian.

This stage does not add dynamic translation, `/language`, a large import pipeline, attachments, or a database-backed
knowledge editor.

## Model

`ErrorKnowledgeEntryV2` identifies the diagnostic case and its source:

- manufacturer, equipment type, series, model, and code;
- source language, type, name, and reference;
- confidence and verification status;
- creation/update timestamps.

`ErrorKnowledgeTextV2` contains one localized audience view:

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

The existing embedded runtime catalog is the source/original layer. Its English title, meaning, safety notes, and
steps are retained for verification and future import work. They are not copied directly into Russian Telegram
messages.

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

## Persistence decision

No EF migration is used. Production diagnostic knowledge is currently embedded static JSON, so a separate
database-backed localization table would introduce a second ownership/update path before an import workflow exists.
The in-code v2 source models the intended split while preserving the current catalog boundary.

ED-24C or a later stage should define the reviewed import/update workflow for large localized error-code sets and
decide whether persistence moves to database tables or versioned generated artifacts.
