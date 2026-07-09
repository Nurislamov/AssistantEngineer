# Yandex Account Linking Boundary

## Purpose

This stage adds an offline-only boundary for future Yandex account linking.

Alice receives devices after account linking. A Yandex user must be mapped to a bridge account, and the bridge account must map to an explicit registry scope before `/devices` can return user devices.

## Current Status

The boundary exists as contracts, a dummy/template linking fixture, an offline validator, an offline scoped registry resolver, and documentation.

Real OAuth is not implemented in this stage. Real Yandex production app credentials are not stored in repo. Real tokens are not issued.

## Out of Scope

This stage does not add real OAuth endpoints, production provider registration, authorization callbacks, token exchange, token storage, token revocation, live Gree+ Cloud integration, MQTT, device control, production wiring, deployment changes, runtime configuration, migrations, or real user/account data.

## Why Account Linking Is Needed

The bridge must not return a global device registry to every Yandex user.

The future flow is:

```text
Yandex user
    ↓ account linking
Bridge account
    ↓ registry scope
Approved registry devices
    ↓
Yandex /devices
```

## Future User Flow In Alice

The user selects the AssistantEngineer/Gree integration in the Alice app, completes future bridge authorization, and then Yandex calls `/devices`.

The bridge resolves the Yandex user binding to a registry scope and returns only approved/exposed devices in that scope.

## Bridge Account Model

A bridge account represents the bridge-side owner of a registry scope.

The current template uses only `dummy-bridge-account-001`.

## Yandex User Binding Model

The binding maps a masked Yandex user reference to a bridge account and registry scope.

The current template uses `masked-yandex-user-001`, `dummy-bridge-account-001`, and `dummy-registry-scope-001`.

Unknown/unlinked users must receive no devices or fail-closed behavior.

## Registry Scope Mapping

Registry scope mapping is explicit. It lists allowed homes, split AC devices, VRF/GMV gateways, and VRF/GMV child units.

The dummy/template scope includes:

```text
dummy-home-001
dummy-gree-ac-001
dummy-vrf-gateway-001
dummy-vrf-child-living-001
dummy-vrf-child-bedroom-001
```

The bridge must never return the global registry by default.

## Unlink Behavior

The offline unlink boundary represents future unlink behavior without touching real storage.

Template unlink marks the binding as unlinked, revokes access to the registry scope, and explicitly reports that no real tokens or secrets were deleted because real token storage is not implemented.

Existing `/unlink` behavior remains offline-only and stable.

## Token And Credential Policy

Real Yandex credentials/tokens are forbidden in repo.

Real OAuth authorization codes, production client identifiers, client secrets, access tokens, refresh tokens, and token storage artifacts must not be committed.

## Dummy/Template Mode

Account linking mode is `offline-template`.

Account linking status is `not-approved`.

All fixtures must use dummy/template or masked references only.

## Validation Rules

Validation checks:

```text
offline-template mode only
masked Yandex user reference required
dummy/template bridge account reference required
dummy/template registry scope reference required
explicit registry scope required
global wildcard scope rejected
inactive/unlinked binding fails closed
unknown binding fails closed
hardware-like identifiers rejected
real-looking Yandex user identifiers rejected
real-looking bridge account identifiers rejected
sensitive material rejected
```

## Forbidden Data

No real credentials, tokens, passwords, device keys, MAC addresses, account identifiers, real Yandex user IDs, real bridge account IDs, OAuth client secrets, OAuth access tokens, OAuth refresh tokens, PCAP, CSV real data artifacts, runtime env config, or production import artifacts are added by this stage.

No live Gree+ Cloud integration in this stage.

No MQTT.

No device control.

No production wiring.

## Future Implementation Stages

Future stages may add provider readiness, real OAuth design, token storage design, and production registration review. Each must be separately approved.

Any future implementation must keep account linking, registry scope resolution, and device exposure separate.

## Next Stage

GREE-ALICE-49 should add the Yandex provider readiness package. It should not enable real OAuth, live Gree+ Cloud integration, MQTT, device control, or production wiring.
