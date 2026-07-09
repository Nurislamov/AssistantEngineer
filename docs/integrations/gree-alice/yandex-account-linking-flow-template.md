# Yandex Account Linking Flow Template

This is a text flow/spec template only. It contains no real OAuth client IDs, client secrets, tokens, production callback URLs, or production readiness claims.

All examples use dummy/template values only.

## Step 1: User Selects AssistantEngineer/Gree Integration In Alice App

Example user reference after masking:

```text
masked-yandex-user-001
```

## Step 2: Alice Opens Future Authorization URL

Future authorization must use a reviewed non-production template before any production registration.

Example placeholder:

```text
template-auth-url
```

## Step 3: User Signs In To Future Bridge Account

Example bridge account reference:

```text
dummy-bridge-account-001
```

## Step 4: Future Bridge Returns Authorization Decision

Example linking session:

```text
LinkingSessionId: dummy-link-session-001
Mode: offline-template
Status: not-approved
YandexUserReference: masked-yandex-user-001
BridgeAccountReference: dummy-bridge-account-001
RegistryScopeReference: dummy-registry-scope-001
```

## Step 5: Future Token Exchange Occurs Outside This Stage

No token exchange is implemented here.

No real access tokens, refresh tokens, client secrets, or production OAuth artifacts are used in this template.

## Step 6: Yandex Calls /devices

Future `/devices` handling must resolve the Yandex user binding before returning devices.

## Step 7: Bridge Resolves Yandex User Binding To Registry Scope

Example binding:

```text
YandexUserReference: masked-yandex-user-001
BridgeAccountReference: dummy-bridge-account-001
RegistryScopeReference: dummy-registry-scope-001
IsActive: true
IsMasked: true
IsDummyOrTemplate: true
```

## Step 8: Bridge Returns Only Approved/Exposed Devices

Example scope:

```text
AllowedHomeIds: dummy-home-001
AllowedDeviceIds: dummy-gree-ac-001
AllowedVrfGatewayIds: dummy-vrf-gateway-001
AllowedVrfChildUnitIds: dummy-vrf-child-living-001, dummy-vrf-child-bedroom-001
```

Unknown or unlinked users receive no devices or fail-closed behavior.

## Step 9: User Can Unlink Integration

Example unlink result:

```text
YandexUserReference: masked-yandex-user-001
BridgeAccountReference: dummy-bridge-account-001
RegistryScopeReference: dummy-registry-scope-001
WasLinked: true
IsNowUnlinked: true
RevokedAccessToRegistryScope: true
DeletedSecrets: false
DeletedTokens: false
RealTokenStorageImplemented: false
Reason: offline-template-unlink
```

Unlink does not claim deleting real tokens or secrets because real token storage is not implemented in this stage.
