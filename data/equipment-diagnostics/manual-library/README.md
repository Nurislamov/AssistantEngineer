# Diagnostic manual library registry

`manuals.json` is the machine-readable manual coverage registry.

- It tracks local source filenames, review/import status, imported package references, counts, and future Telegram library policy.
- It is metadata only and does not add diagnostic codes.
- Local source paths are repository-relative references under the ignored `artifacts/manual-intake/sources/gree` directory.
- PDF, DOC, XLS, and XLSX source manuals must not be committed.
- Diagnostic imports remain manual-bound: one manual equals one source.

## Telegram file bindings

ED-24G.0 adds the first Telegram manual-library binding mechanism.

- Runtime bindings map `manualId` to a Telegram `file_id`.
- The default runtime binding path is `artifacts/operations/equipment-diagnostics-manual-bindings.json`.
- Override it only with `AssistantEngineer:EquipmentDiagnostics:Telegram:ManualLibrary:FileBindingsPath` when the
  deployment has a reviewed durable path.
- That runtime path is ignored and must contain real Telegram file IDs only on the server.
- `manual-file-bindings.sample.json` is a template only. It contains no real file IDs.
- If the runtime binding file is missing, `/manuals` still works and tells technical users that the known manual file is
  not connected yet.
- Consumer users cannot receive manuals. Installer, Engineer, Admin, and Owner can request eligible manuals.
- Only Admin and Owner can register bindings with `/manual_register <manualId>` attached to or replying to a Telegram
  document.
- Do not type or commit raw Telegram file IDs, chat IDs, user IDs, tokens, or secrets.

Human-readable coverage is documented in `docs/equipment-diagnostics/manual-coverage.md`.
