# Equipment Diagnostics Bot Application Flow

ED-15A adds the deterministic application contract. ED-15B exposes it through one backend HTTP endpoint:

```http
POST /api/v1/equipment-diagnostics/bot/diagnose
```

The endpoint is a thin adapter over `IEquipmentDiagnosticBotFacade`. It does not add Telegram, a web chat UI, persistence, external calls, AI, RAG, or vector search.

## ED-15C Request Hardening

The HTTP endpoint requires explicit `manufacturer` and `code`; current free text is context only and is not an identity parser. Inputs are trimmed and validated before facade delegation. Limits are: manufacturer 80, code 32, free text 500, series/model 120 each, site context 300, preferred language 16, and up to 20 operator-provided name/value measurements with names up to 80 and values up to 120 characters.

Missing required identity, overlong input, and disallowed control characters return a deterministic validation problem. The endpoint rejects invalid input without truncation and does not log or persist the raw request.

## Runtime-Only Answer Boundary

`IEquipmentDiagnosticBotService` searches the approved runtime catalog through `IEquipmentDiagnosticsService`. A final `Answer` is returned only when one runtime diagnostic match and its runtime diagnostic case are available.

The bot service does not read or diagnose from:

- production staging candidates;
- manual codebook occurrences;
- generated staging preview artifacts;
- local PDF/manual files.

Those sources support review and future promotion workflows only. They are not runtime diagnostic facts.

## Deterministic Decision Flow

1. Normalize manufacturer and displayed code.
2. Require explicit manufacturer and code; free text is retained as context but is not used for inference.
3. Search the runtime catalog using available series, model, category, equipment-side, and display-context filters.
4. Return `Answer` for one runtime match with an available diagnostic case.
5. Return `ClarificationRequired` with stable options when multiple runtime contexts match.
6. Return `ReferenceOnly` for known status, debug, query, or setting patterns that have no runtime diagnostic case.
7. Return `Unsupported` when a controller model name such as CE41, CE42, or CE52 is supplied as a fault code.
8. Return `NotFound` when no runtime diagnostic case exists.

## Confidence And Safety

Seed or unverified runtime entries may produce an `Answer`, but the response sets `verificationRequired` and clearly warns that the exact installed equipment service manual must be checked. A manual-verified response is possible only when an already-approved runtime case explicitly carries that evidence and confidence.

Every response includes a safety boundary. Electrical, refrigerant-circuit, compressor, and protected-equipment checks require a qualified technician, and safety protections must remain active.

The service never returns long manual text, never promotes knowledge, and never writes runtime or staging files.

## Clarification

Clarification options identify the manufacturer, series, category, equipment side, inferred display context, and code. Their order is deterministic. Typical ambiguous inputs include a shared code used by indoor, outdoor, and chiller families.

## Future Adapter

A future reviewed adapter may consume this endpoint. Any adapter must preserve the runtime-only answer boundary, reference-only handling, verification warnings, deterministic output, and safety rules.
