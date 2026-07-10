# Yandex OAuth Provider Pilot Contract

## Purpose

Design the first real Yandex/Alice pilot contract for account linking and Provider Adapter API access before any runtime OAuth implementation is added.

## Current status

Internal/offline RC1 is CLOSED.
Real Yandex/Alice production release remains NOT READY.
This document only designs the pilot contract.

## Pilot goal

Alice through a real private Yandex Smart Home skill/provider can complete account linking and see dummy/offline devices through `GreeAliceBridge`. The pilot must keep `/action` dry-run fail-closed and must not read or control live Gree+ devices.

## Official Yandex constraints

- Yandex Smart Home quick-start requires an OAuth 2.0 authorization service.
- Yandex Smart Home quick-start requires Provider Adapter API.
- The provider must describe devices in Yandex Smart Home device format.
- Requests include `X-Request-Id` and logging should preserve it for incident investigation.
- The Smart Home skill must be registered in Yandex Dialogs as a smart-home skill.
- Production/public publication requires moderation.
- A private skill can be used for pilot testing before public publication.
- Backend Endpoint URL must use HTTPS.
- Backend response time budget is 3 seconds.
- OAuth token and refresh token length must not exceed 2048 characters.
- `expires_in` must be a number in the supported Yandex range.

## Out of scope

- Runtime OAuth implementation.
- Real Yandex provider registration data.
- Real credentials or tokens.
- Production callback wiring.
- Production endpoint configuration.
- VPS deployment.
- Live Gree+ Cloud read-only adapter.
- Live Gree+ Cloud control adapter.
- MQTT.
- Device control.
- Admin UI.
- New runtime config committed to the repository.
- Migrations.
- Telegram or `AssistantEngineer.Api` wiring.

## Pilot flow

1. User opens private Yandex Smart Home skill account linking.
2. Yandex redirects user to bridge authorization endpoint.
3. Bridge shows dev-only login/approval page or returns dev-only approval flow, depending on implementation decision in PILOT-1B.
4. Bridge issues short-lived authorization code for a test bridge account.
5. Yandex exchanges authorization code for access token.
6. Yandex calls Provider Adapter API with Authorization header.
7. Bridge maps token to bridge account and registry scope.
8. Bridge returns dummy/offline devices.
9. `/query` returns offline fixture state.
10. `/action` remains dry-run fail-closed.

## Required endpoints

| Endpoint | Purpose | Pilot mode behavior | Authentication requirement | Allowed data | Forbidden data | Expected response category |
|---|---|---|---|---|---|---|
| `GET /oauth/authorize` | Start account linking | Dev-only approval for test bridge account | Yandex OAuth request parameters, validated in pilot mode | Placeholder client reference, allowed redirect URI, state/nonce | Real secrets, tokens, broad account data | Authorization code or controlled error |
| `POST /oauth/token` | Exchange code/refresh token | Dev-only token issue/refresh in PILOT-1B | Client authentication from secret store/env only | Short-lived code, masked client reference | Committed client secret, logged tokens | OAuth token JSON |
| `GET /oauth/callback` | Documented callback route if implementation needs one | Dev-only callback landing/approval continuation | Valid state/nonce | Placeholder redirect data | Real account identifiers | Controlled callback response |
| `GET /v1.0/user/devices` | Provider Adapter API discovery | Return reviewed dummy/offline devices | `Authorization: Bearer <access_token>` in real pilot mode | Dummy split AC and exposed VRF child units | Gateway, unreviewed devices, live Gree data | Device list |
| `POST /v1.0/user/devices/query` | Provider Adapter API state query | Return offline fixture state | Bearer token in real pilot mode | Dummy/offline device IDs | Live Gree state, secrets | Query response |
| `POST /v1.0/user/devices/action` | Provider Adapter API action | Dry-run fail-closed | Bearer token in real pilot mode | Dummy/offline device IDs | Actual command execution | Fail-closed action response |
| `POST /v1.0/user/unlink` | Account unlink | Revoke dev-only token/account mapping in result/model | Bearer token in real pilot mode | Masked account reference | Real token logs, production deletes | Offline unlink response |
| `GET /health` | Health check | Report local/pilot readiness without secrets | None or local operator access depending on host | Mode flags | Secrets, tokens | Health response |

## Required configuration

The pilot implementation stage must use configuration keys only. No real values may be committed.

```text
GreeAliceBridge:Yandex:PilotMode
GreeAliceBridge:Yandex:ProviderMode
GreeAliceBridge:Yandex:PublicBaseUrl
GreeAliceBridge:Yandex:ClientId
GreeAliceBridge:Yandex:ClientSecret
GreeAliceBridge:Yandex:AllowedRedirectUris
GreeAliceBridge:Yandex:AuthorizationCodeLifetimeSeconds
GreeAliceBridge:Yandex:AccessTokenLifetimeSeconds
GreeAliceBridge:Yandex:RefreshTokenLifetimeSeconds
GreeAliceBridge:Yandex:RequireHttpsPublicBaseUrl
GreeAliceBridge:Yandex:EnableDevOnlyInMemoryTokenStore
```

Rules:

- No real values in repository.
- Only placeholders/examples are allowed.
- `ClientSecret` must never be committed.
- `PublicBaseUrl` must be HTTPS for real Yandex testing.
- Localhost remains allowed only for local smoke, not for Yandex production callback.

## Secret handling rules

Real client ID, client secret, access tokens, refresh tokens, authorization codes, account identifiers, and device identifiers must be supplied outside the repository through approved secret store or environment injection in a later stage. They must not be logged, committed, pasted into evidence, or written to docs.

## Token model

Token model fields:

```text
AuthorizationCode
AccessToken
RefreshToken
ExpiresAtUtc
IssuedAtUtc
RevokedAtUtc
BridgeAccountId
MaskedYandexUserId
Scope
ClientIdReference
RedirectUri
Nonce/State
```

Rules:

- Authorization codes must be short-lived.
- Access tokens must be bearer tokens but never stored in repository.
- Refresh tokens must never be logged.
- Token length must stay under Yandex documented limit.
- `expires_in` must be valid.
- PILOT-1B may use dev-only in-memory token store.
- Production token store remains NOT IMPLEMENTED in PILOT-1A.

## Token store mode

PILOT-1B may use a dev-only in-memory token store for private skill testing. Production token storage remains not implemented and must require a separate security/deployment approval package.

## Account mapping

```text
Yandex user/account
→ masked Yandex subject/user reference
→ Bridge account
→ explicit registry scope
→ exposed devices
```

Rules:

- Unknown token fails closed.
- Unknown bridge account fails closed.
- Account without registry scope returns no devices or controlled authorization failure.
- Gateway remains hidden.
- Only reviewed/exposed split AC and VRF child units can be returned.

## Yandex request authentication model

Provider Adapter API requests must require `Authorization: Bearer <access_token>` in real pilot mode. Local/offline smoke may continue to use existing local-only mode. No basic auth with real credentials. No query-string tokens. No tokens in logs.

## X-Request-Id logging

When `X-Request-Id` is present, the bridge must capture and preserve it in structured logs for incident investigation. Logs must not include bearer tokens, refresh tokens, real credentials, raw account identifiers, or real device identifiers.

## Device exposure model

The private skill pilot may expose only dummy/offline devices already reviewed by the registry boundary:

- dummy split AC device;
- exposed VRF/GMV child units;
- no gateway exposure by default;
- no auto-exposed Gree Cloud discovery data.

## Action behavior

`/action` remains dry-run fail-closed for PILOT-1. `SentToGreeCloud`, `SentToMqtt`, and `SentToDevice` must remain false. No device command execution is allowed.

## Security boundaries

- Design-only contract in PILOT-1A.
- No runtime OAuth endpoints implemented.
- No real credentials/tokens in repository.
- No live Gree+ Cloud read.
- No live Gree+ Cloud control.
- No MQTT.
- No production deployment.
- No device control.
- Production Yandex release remains NOT READY.

## Pass criteria

- Contract documents the private skill pilot flow.
- Official Yandex constraints are recorded.
- Required endpoint/config/token/account mapping contracts are documented.
- Example config contains placeholders only.
- Production Yandex release remains NOT READY.

## Fail criteria

- Any real credential/token/account/device identifier is added.
- Runtime OAuth endpoints are implemented in PILOT-1A.
- Live Gree+ Cloud, MQTT, device control, production deploy, or production config is added.
- Docs imply public production readiness.

## Next implementation stage

GREE-ALICE-PILOT-1B — implement dev-only Yandex OAuth/provider skeleton.
