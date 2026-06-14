# Equipment Diagnostics Bot Beta Backend Readiness

ED-20A adds a consolidated closed-beta report; see `beta-readiness-report.md`. This remains a closed beta only, not production or public release.

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

## ED-16A Internal UI

The internal frontend now includes `/equipment-diagnostics`, a deterministic panel over the existing backend endpoint. It supports answers, clarification, reference-only, not-found, unsupported, validation, and network-error states while preserving the runtime-only final-answer boundary.

Frontend verification:

```powershell
cd src/Frontend
npm test
npm run build
```

## ED-16B Field Acceptance

The beta boundary now includes eight deterministic field scenarios accepted through the module facade, core HTTP integration, and frontend state tests. The optional local runner is:

```powershell
.\scripts\equipment-diagnostics\run-bot-scenario-smoke.ps1 -BaseUrl http://localhost:5000
```

Passing these scenarios means the existing runtime-only bot behavior is ready to be consumed by a future reviewed adapter. It does not mean Telegram, audit, authentication/roles, feedback, database administration, or broad manual-backed coverage is complete.

## ED-17A Telegram Adapter Skeleton

ED-17A adds a disabled-by-default, deterministic Telegram-like parser, formatter, and update handler over the
existing bot facade. It has no Telegram package, token, webhook, long polling, hosted service, external call, or
new public endpoint. Direct tests use fake updates and in-memory options.

The skeleton preserves the runtime-only diagnosis boundary and formats answer, clarification, reference-only,
not-found, unsupported, and validation states as bounded plain text. See
[telegram-adapter.md](telegram-adapter.md).

## Not Ready Or Not Claimed

- production Telegram transport or external/public chat UI;
- production authentication, roles, or endpoint-specific rate limiting;
- audit log and operator feedback loop;
- database-backed administration;
- broad manual-backed runtime promotion.

## ED-17B Webhook Transport

The beta transport now includes one disabled-by-default Telegram webhook endpoint with a required secret header
when enabled and an outbound `HttpClient` abstraction. Tests replace outbound delivery with fakes. Production
deployment still requires HTTPS, secret-store configuration, allowed-chat review, monitoring, and an audit/rate
control decision. See [telegram-webhook-deployment.md](telegram-webhook-deployment.md).

## ED-17C Operations Readiness

Telegram access now supports deny-first chat/username policy and explicitly controlled `/id` and `/whoami`
discovery for initial allowlist setup. Discovery and the webhook transport remain disabled by default. There is
still no audit log, admin UI, database persistence, or endpoint-specific rate-limit claim.

## ED-18A Deployment Scaffold

The provider-neutral Docker Compose and Caddy examples prepare a future VPS deployment without selecting a
provider, committing secrets, or enabling Telegram. Production deployment, monitoring, backups, audit logging,
and provider-specific hardening remain future work.

## ED-18B Deployment Hardening Checklist

Static environment/scaffold validators, hardened smoke checks, and production release/rollback checklists now
prepare an operator-reviewed deployment. They do not perform a real deploy or implement monitoring, backups,
audit logging, provider-specific infrastructure, or automatic Telegram activation.

The endpoint remains classified according to the broader API security setup. Operator-facing diagnostic guidance requires a trained, qualified technician.
