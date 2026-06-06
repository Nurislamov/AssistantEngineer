# Equipment Diagnostics Manual Code Book

The manual codebook is a source-controlled, non-runtime inventory of code occurrences found in locally reviewed manuals. It records where a code appears, how the manual presents it, and whether the occurrence has enough evidence to become a future staging candidate.

It is deliberately separate from:

- the runtime diagnostic catalog, which serves API responses;
- staging candidates, which contain reviewed diagnostic guidance proposed for later promotion;
- local PDF artifacts, which remain ignored under `artifacts/manual-intake/sources/gree/`.

A code occurrence is not automatically a diagnostic case. Error-indication table rows normally use `NeedsTroubleshootingEvidence`; status, query, setting, controller, commissioning-tool, and applicability references remain `ReferenceOnly`.

## Classification

- `Fault` and `Protection` describe explicit manual classifications.
- `Status`, `Debugging`, `Query`, `Setting`, and `DisplayPattern` must not be treated as normal faults.
- Controller and portable-tool functions use `ToolFunction` and document display/operation context only.
- Technical sales guides use `TechnicalGuideApplicability` and support family/model applicability, not troubleshooting.

## Promotion

Future staging candidates may be created only after the exact manual, page, section, equipment family, meaning, and troubleshooting evidence are reviewed. Promotion is manual and reviewed; the codebook never loads into the runtime catalog.

No PDFs are committed. No database, Telegram integration, AI, RAG, or vector search is part of this layer.
