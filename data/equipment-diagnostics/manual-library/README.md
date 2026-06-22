# Diagnostic manual library registry

`manuals.json` is the machine-readable manual coverage registry.

- It tracks local source filenames, review/import status, imported package references, counts, and future Telegram library policy.
- It is metadata only and does not add diagnostic codes.
- Local source paths are repository-relative references under the ignored `artifacts/manual-intake/sources/gree` directory.
- PDF, DOC, XLS, and XLSX source manuals must not be committed.
- Diagnostic imports remain manual-bound: one manual equals one source.

## Telegram file bindings

ED-24G.1 hardens the first Telegram manual-library binding mechanism.

- Runtime bindings map `manualId` to a Telegram `file_id`.
- The default runtime binding path is `artifacts/operations/equipment-diagnostics-manual-bindings.json`.
- Production Docker Compose persists that path through `/opt/assistantengineer/artifacts/operations/` on the host
  mounted to `/app/artifacts/operations/` in the API container.
- Override it only with `AssistantEngineer:EquipmentDiagnostics:Telegram:ManualLibrary:FileBindingsPath` when the
  deployment has a reviewed durable path.
- That runtime path is ignored and must contain real Telegram file IDs only on the server.
- `manual-file-bindings.sample.json` is a template only. It contains no real file IDs.
- If the runtime binding file is missing, `/manuals` still works and tells technical users that the known manual file is
  not connected yet.
- If only some referenced manuals are connected, `/manuals` sends the connected documents and lists the missing ones.
- Consumer users cannot receive manuals. Installer, Engineer, Admin, and Owner can request eligible manuals.
- Only Admin and Owner can register bindings with `/manual_register <manualId>` attached to or replying to a Telegram
  document.
- Only Admin and Owner can unregister bindings with `/manual_unregister <manualId>` or view safe connection state with
  `/manual_bindings`.
- Binding lists expose only safe manual display names, document codes, connection state, and safe original filenames.
  They must not expose Telegram `file_id`, chat/user identifiers, tokens, package IDs, or local paths.
- Do not type or commit raw Telegram file IDs, chat IDs, user IDs, tokens, or secrets.

Human-readable coverage is documented in `docs/equipment-diagnostics/manual-coverage.md`.
