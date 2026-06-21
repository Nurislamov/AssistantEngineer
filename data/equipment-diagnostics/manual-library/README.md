# Diagnostic manual library registry

`manuals.json` is the machine-readable manual coverage registry.

- It tracks local source filenames, review/import status, imported package references, counts, and future Telegram library policy.
- It is metadata only and is not loaded into runtime diagnostic responses.
- Local source paths are repository-relative references under the ignored `artifacts/manual-intake/sources/gree` directory.
- PDF, DOC, XLS, and XLSX source manuals must not be committed.
- Diagnostic imports remain manual-bound: one manual equals one source.

Human-readable coverage is documented in `docs/equipment-diagnostics/manual-coverage.md`.
