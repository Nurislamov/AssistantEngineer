# Equipment Diagnostics Bot Beta Backend Readiness

## ED-15C Scope

The beta backend exposes the existing deterministic endpoint:

```http
POST /api/v1/equipment-diagnostics/bot/diagnose
```

ED-15C hardens this endpoint without adding adapters, persistence, external calls, AI, RAG, vector search, or new routes.

## Request Validation

The endpoint requires an explicit `manufacturer` and displayed `code`. Free text is accepted only as context; it is not used to infer missing equipment identity.

| Field | Limit |
| --- | ---: |
| manufacturer | 80 |
| code | 32 |
| freeText | 500 |
| series | 120 |
| modelCode | 120 |
| siteContext | 300 |
| preferredLanguage | 16 |
| operatorProvidedMeasurements | 20 items |
| measurement name | 80 |
| measurement value | 120 |

Inputs are trimmed. Overlong values, missing required identity, and disallowed control characters return a deterministic HTTP 400 validation problem. Invalid input is rejected rather than silently truncated. The current public measurement contract is a name/value dictionary; a unit-specific limit is reserved for a future structured measurement contract.

## Runtime Boundary

Only the approved runtime catalog may produce a final `Answer`. Staging candidates, manual codebook occurrences, generated previews, local manual files, and verification artifacts remain non-runtime review inputs and are not user-facing diagnostic facts.

Reference-only status/debug/query/setting patterns do not become final diagnostic answers. Seed knowledge retains verification, provenance, confidence, and qualified-technician safety warnings.

## Local Smoke

Start the API separately, then run:

```powershell
.\scripts\equipment-diagnostics\smoke-bot-diagnostic-endpoint.ps1 -BaseUrl http://localhost:5000
```

The script sends one minimal runtime request and fails when HTTP is not 200 or the JSON response lacks the expected status fields.

## Ready For Beta Backend

- deterministic request validation and stable response contracts;
- runtime-only final answer boundary;
- clarification, reference-only, and not-found behavior;
- safety/provenance warnings and regression guards;
- local developer smoke script.

## Not Ready Or Not Claimed

- Telegram adapter or web chat UI;
- production authentication, roles, or endpoint-specific rate limiting;
- audit log and operator feedback loop;
- database-backed administration;
- broad manual-backed runtime promotion.

The endpoint remains classified according to the broader API security setup. Operator-facing diagnostic guidance requires a trained, qualified technician.
