# Equipment Diagnostics

Equipment Diagnostics is an early backend module for deterministic diagnostic support around equipment error codes, likely causes, required measurements, safety notes, and manual references.

## Purpose

- Provide a clean domain and application foundation for HVAC equipment error-code diagnostics.
- Keep diagnostic knowledge separate from equipment catalog and equipment selection concerns.
- Expose a public facade that the API layer can compose without depending on module internals.
- Start with deterministic JSON seed data before persistence, import, or search infrastructure is introduced.

## Boundaries

- The module owns equipment diagnostic concepts: manufacturers, series, models, error codes, diagnostic cases, diagnostic steps, required measurements, manual references, and confidence.
- The module does not own room/building load calculation, equipment sizing, catalog persistence, report rendering, authentication, or UI.
- Domain and application code must not depend on EF Core, ASP.NET Core, Infrastructure, UI, or calculation implementation details.
- ED-00 has no database migration and no Infrastructure adapter.

## Why This Is Separate From Equipment

`AssistantEngineer.Modules.Equipment` is responsible for equipment catalog and selection workflows. Error-code diagnostics have different data, safety, provenance, and future search requirements. Keeping diagnostics in `AssistantEngineer.Modules.EquipmentDiagnostics` avoids mixing catalog sizing concerns with service troubleshooting knowledge.

## MVP Scope

ED-00 includes:

- Domain model records and enums for diagnostic concepts.
- Application contracts for searching error codes and retrieving diagnostic cases.
- A public facade for API composition.
- Deterministic in-memory seed examples:
  - Gree / GMV / H5
  - Gree / GMV / C7
  - Gree / Chiller / E6
- Tests for search behavior, normalization, diagnostic-case content, confidence claims, and module dependency boundaries.

ED-01 adds read-only API endpoints in `AssistantEngineer.Api`:

- `GET /api/v1/equipment-diagnostics/error-codes`
- `GET /api/v1/equipment-diagnostics/cases`

The API layer is a thin adapter over `IEquipmentDiagnosticsFacade`. Search normalization stays in the application service.

ED-02 adds a knowledge catalog layer:

- `Application/Knowledge/IEquipmentDiagnosticsKnowledgeSource.cs`
- `Application/Knowledge/InMemoryEquipmentDiagnosticsKnowledgeSource.cs`
- `Application/Knowledge/EquipmentDiagnosticsKnowledgeCatalog.cs`
- `Application/Knowledge/EquipmentDiagnosticsKnowledgeEntry.cs`

`InMemoryEquipmentDiagnosticsService` now depends on `IEquipmentDiagnosticsKnowledgeSource`. The service owns query behavior, normalization, filtering, and DTO mapping. The deterministic seeded diagnostic entries live in the knowledge catalog instead of inside the service.

ED-03 moves seeded entries into structured JSON files embedded in the module:

- `Knowledge/equipment-diagnostics.schema.json`
- `Knowledge/gree/gree-gmv.json`
- `Knowledge/gree/gree-chiller.json`

`EquipmentDiagnosticsJsonKnowledgeSource` reads embedded JSON resources through `EquipmentDiagnosticsKnowledgeJsonLoader`. The loader performs lightweight module validation without external schema-validator dependencies. The catalog remains deterministic and local. It is not persistence, not a manual-ingestion workflow, and not an AI/RAG search layer.

## JSON Catalog

Each JSON file contains an `entries` array. Each entry has:

- `manufacturer`, `seriesName`, `modelCode`, `category`, `code`, `title`, `meaning`, `severity`, `confidence`
- `likelyCauses[]`
- `diagnosticSteps[]` with `order`, `title`, `instruction`, `expectedResult`, `ifFailedAction`
- `requiredMeasurements[]` with `name`, `unit`, `description`, `requiredBeforeConclusion`
- `safetyNotes[]`
- `manualReferences[]`
- `tags[]`

To add a new manufacturer, series, or code:

1. Add or extend a JSON file under `Knowledge/{manufacturer}/`.
2. Keep arrays present even when a future entry has no optional content.
3. Use only defined `EquipmentCategory` and `DiagnosticConfidence` enum names.
4. Keep `confidence` below `ManualVerified` until exact manual evidence and page references are present in the repository.
5. Include safety notes and required measurements for every diagnostic case.
6. Run `dotnet test AssistantEngineer.sln`; JSON loader tests validate required fields, enum values, safety text, embedded resources, and seeded behavior.

Confidence levels:

- `Unknown`: insufficient knowledge to classify confidence.
- `Low`: preliminary deterministic seed, not manually verified.
- `Medium`: curated but still not exact manual-page verified.
- `High`: strong curated evidence, still below explicit manual verification.
- `ManualVerified`: reserved for future entries with explicit manual title/version/page evidence and audit rules.

## API Routes

### Search Error Codes

`GET /api/v1/equipment-diagnostics/error-codes`

Query parameters:

- `manufacturer` required
- `code` optional
- `series` optional
- `modelCode` optional
- `category` optional

Unknown manufacturers or codes return `200 OK` with an empty array.

Example:

```http
GET /api/v1/equipment-diagnostics/error-codes?manufacturer=Gree&code=H5
```

Example response:

```json
[
  {
    "manufacturer": "Gree",
    "seriesName": "GMV",
    "modelCode": null,
    "code": "H5",
    "title": "GMV protection alarm H5",
    "meaning": "Preliminary diagnostic entry for a Gree GMV H5 alarm. Verify the exact meaning against the service manual for the installed model before concluding.",
    "severity": "Service attention required",
    "category": 0,
    "confidence": 1,
    "sourceManual": {
      "manufacturer": "Gree",
      "manualTitle": "Gree service manual for the matching series and model",
      "manualVersion": null,
      "page": null,
      "notes": "ED-03 deterministic JSON seed only; exact manual page is not verified in this repository."
    }
  }
]
```

### Get Diagnostic Case

`GET /api/v1/equipment-diagnostics/cases`

Query parameters:

- `manufacturer` required
- `code` required
- `series` optional
- `modelCode` optional

If a case is not found, the endpoint returns `404 NotFound` with AssistantEngineer problem-details metadata.

Example:

```http
GET /api/v1/equipment-diagnostics/cases?manufacturer=Gree&series=GMV&code=H5
```

Example response excerpt:

```json
{
  "errorCode": {
    "manufacturer": "Gree",
    "seriesName": "GMV",
    "code": "H5",
    "confidence": 1
  },
  "likelyCauses": [
    "Outdoor unit protection condition reported by the control board."
  ],
  "diagnosticSteps": [
    {
      "order": 1,
      "title": "Confirm installed equipment identity",
      "instruction": "Record the outdoor unit model, GMV series, serial plate data, and controller-displayed error code.",
      "expectedResult": "Manufacturer, series, model, and displayed H5 code are confirmed before diagnosis continues.",
      "ifFailedAction": "Stop classification and obtain the exact model information."
    }
  ],
  "requiredMeasurements": [
    {
      "name": "Supply voltage",
      "unit": "V",
      "requiredBeforeConclusion": true
    }
  ],
  "safetyNotes": [
    "Electrical, compressor, inverter, refrigerant, and chiller protection checks must be performed by a qualified technician."
  ],
  "confidence": 1
}
```

## Non-Goals

- No EF Core persistence or migrations.
- No Telegram bot integration.
- No AI, RAG, vector search, or semantic search.
- No calculation-physics changes.
- No protection-defeat or safeguard-deactivation instructions.

## Safety Notes

- Electrical, compressor, inverter, refrigerant, and chiller protection checks must be performed by qualified technicians.
- Diagnostics must keep safety switches, protection inputs, current protection, pressure protection, flow protection, and controller safeguards active during diagnosis.
- Seed entries are preliminary unless an exact manual reference exists in the repository. They must not claim `ManualVerified` confidence without explicit source evidence.
- ED-01 remains deterministic seeded knowledge only. It does not claim full manual verification, and it does not add Telegram, RAG/vector search, or persistence.
- ED-02 keeps seeded knowledge in a deterministic in-memory catalog. Entries remain low-confidence unless explicit source evidence is added later.
- ED-03 keeps seeded knowledge in deterministic embedded JSON files. It does not add persistence, Telegram, RAG/vector search, AI search, or full manual verification claims.

## Future Stages

- ED-04: catalog expansion for Gree GMV/chiller real manuals with explicit provenance and page references.
- ED-05: persistence/admin import through a dedicated Infrastructure adapter and migration.
- ED-06: Telegram or assistant UX on top of the existing facade and API, without moving diagnostics into the Equipment catalog module.
- ED-07: RAG/manual evidence search only if deterministic source-backed data and safety/provenance rules justify it.
- Add audit and confidence rules for manual-backed content before any `ManualVerified` claim is allowed.
