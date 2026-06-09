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

## ED-14B Coverage And Readiness

Run:

```powershell
dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- codebook-coverage --repo-root .
```

The command writes ignored JSON and Markdown reports under `artifacts/verification/equipment-diagnostics/`. It compares runtime, production staging, codebook, and manual registry validation context.

- Runtime and matching staging coverage take precedence.
- Fault/protection occurrences backed only by an error-indication table require a troubleshooting section.
- A troubleshooting-backed fault may be recommended for a future reviewed staging candidate.
- Status, debugging, query, setting, parameter, controller, and tool occurrences remain reference-only.
- Same-context conflicts must be resolved before staging or runtime work.

The report is analysis and recommendation only. It does not create production staging files, alter the runtime catalog, or grant `ManualVerified` confidence.

## ED-14C Evidence Rules And Preview

`TroubleshootingSection` evidence from a registered primary troubleshooting source is required before a codebook occurrence can enter the generated staging preview. Error-indication tables, owner manuals, technical guides, controllers, and commissioning tools are supporting/reference evidence only. Status, debugging, query, and setting entries remain reference-only.

The preview uses `DraftPreview`, preserves exact source anchors, and is generated only in ignored artifacts. A preview candidate still requires engineering review. ED-14D may convert reviewed preview candidates into real staging files through a separate explicit change.

The current corpus includes exact GMV6 service-manual troubleshooting occurrences for E1, E3, E4, and F5. E1, E3, and E4 are excluded from generated preview because production staging already covers them. F5 is the first generated preview candidate; it remains an artifact-only draft and is not runtime or production staging knowledge.
