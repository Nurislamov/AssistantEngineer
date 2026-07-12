# Gree Plus API Contract Gaps

## Current conclusion

The current evidence is enough to maintain an evidence-backed candidate inventory, but not enough to implement a real authenticated Gree Plus API client. The live read-only probe must remain fail-closed with `IsReadOnlyContractConfirmed=false` and `NetworkAttempted=false` by default.

## Confirmed evidence

```text
Static APK inventory PASS for com.gree.greeplus 1.25.3.7.
Flutter/plugin contract extraction PASS.
libapp.so SHA-256: 43071232B93304D2F2249DE1CB2EC72D9BDD5F42451EBF1D17CDCA9E70E7A720.
Clean static application inventory: 77 /App/*, 8 /Stats/*, 1 /GreeAccess/*, total 86.
Runtime-observed HTTPS candidates: /App/QueryOnline and /App/OptHistory.
Command candidate: /GreeAccess/access/action is statically discovered and runtime-correlated.
Plugin bridge includes request, sendDataToDevice, MQTT callback, publish, subscribe, UserData, and Safety surfaces.
```

## Blocking unknowns

### Region-to-host resolution

Observed evidence includes `hkgrih.gree.com`, region/server hints, and other Gree domains, but the exact region selection request/response contract is incomplete.

### Authentication and session lifecycle

Token field names and session hints are visible, but login, token refresh, verification, expiry handling, cookie/session behavior, and account binding are not sufficiently proven for a client.

### Required headers

Required header names, values, signing-related headers, locale headers, device/app headers, and replay/timestamp requirements remain unknown.

### Signing/encryption

Safety bridge methods such as `encryptData`, `decryptData`, `jsonToHex`, `hexToJson`, and `restoreHexData` are confirmed, but the exact use in live HTTPS/MQTT contracts is not proven.

### Homes and rooms discovery request/response

`/App/GetHomes` and `/App/GetDevsInRoomsOfHomeV2` are known discovery paths, but the complete request method/body/header contract and response envelope are still incomplete.

### Device binding

The mapping from account/home/room data to a selected device alias, split unit, VRF gateway, or child indoor unit remains incomplete. Real identifiers must stay outside the repository.

### Read-only status endpoint contract

`/App/QueryOnline` was runtime-observed as a periodic poll candidate, but exact method, headers, body/query, response envelope, and non-mutation proof are missing.

### Command endpoint contract

`/GreeAccess/access/action` is a command endpoint candidate and must never be blind-probed. The exact method, authorization, request envelope, body shape, signing/encryption, response envelope, and rollback behavior remain unknown.

### MQTT authentication/topic contract

Prior capture and plugin evidence contain MQTT host/callback/publish/subscribe candidates. Authentication, topic names, subscriptions, state/control split, payload envelope, and QoS/session behavior remain unknown.

### Response envelopes and error model

Success/error envelopes, status codes, retry behavior, throttling, account/device permission errors, and localization behavior remain unconfirmed.

### VRF gateway/child addressing

The exact model for gateway device, child unit addressing, cloud-visible child state, and command target routing is not confirmed.

## Risk classification

```text
Read candidates: require exact contract and non-mutation proof before live use.
Runtime-observed candidates: require passive/focused capture before implementation.
Mutating candidates: DoNotBlindProbe.
Destructive candidates: DestructiveDoNotProbe.
Command candidates: DoNotBlindProbe and require separate explicit control approval.
Unknown candidates: UnknownNeedsReview.
```

## Safe next investigations

```text
Passive Wi-Fi gateway metadata capture.
Focused JSBridge argument capture using redacted/masked local evidence.
Approved TLS-key acquisition path if separately reviewed.
Static focused extraction of minified plugin object literals and dynamic payload assembly.
Documentation-only comparison of sanitized request/response shape evidence.
```

## Forbidden blind probes

Do not blindly call `Set*`, `Mod*`, `Add*`, `Create*`, `Update*`, `StartOrCancel*`, `Del*`, `Delete*`, `Clear*`, `Remove*`, `/GreeAccess/access/action`, MQTT publish, MQTT subscribe, or any unknown action/control endpoint.

## Exit criteria for first live read-only request

```text
Exact endpoint path confirmed.
Exact method confirmed.
Required headers documented with values redacted.
Request body/query shape documented with placeholders only.
Authentication/session lifecycle documented.
Response envelope documented.
Proof that no command/control/action path is called.
Single operator-approved device alias.
Credentials and identifiers supplied only from untracked local or environment sources.
Tests prove default behavior attempts no network.
```

## Exit criteria for future control pilot

```text
Separate explicit control approval exists.
Read-only path already works and is reviewed.
Command endpoint/channel selected from evidence, not guesses.
Exact command envelope, auth, signing/encryption, target addressing, response, and rollback behavior are documented.
Single-device allowlist and kill switch are in place.
Dry-run/fail-closed tests cover default behavior.
No production wiring, deployment, migrations, or Telegram/API integration are added without separate approval.
```
