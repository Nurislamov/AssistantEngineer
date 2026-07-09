# GREE-ALICE live read-only pilot decision record template

Decision status: NOT APPROVED

This template is a manual decision record for a possible future live read-only pilot. It is not approval by itself.

Real account values, real device values, credentials, tokens, passwords, keys, MAC addresses, PCAP, CSV, and raw artifacts must stay outside the repository. Use masked identifiers in this document.

## Date

```text
YYYY-MM-DD
```

## Operator

```text
masked-operator-name-or-role
```

## Repository commit

```text
commit-sha
```

## Validation result

```text
dotnet restore: PASS/FAIL
dotnet build: PASS/FAIL
dotnet test: PASS/FAIL
git diff --check: PASS/FAIL
```

## Exact test account scope

```text
masked-account-scope-only
```

## Exact test device scope

```text
masked-device-scope-only
```

## Evidence location

```text
masked-external-evidence-location
```

## Masked identifiers confirmation

```text
All account and device identifiers in repository docs/tests are masked: yes/no
```

## Credential storage confirmation

```text
Credential material is stored only outside the repository: yes/no
```

## Kill-switch plan

```text
Describe how live read-only access will be disabled immediately.
```

## Rollback plan

```text
Describe how the bridge returns to offline fixture behavior without production deployment or migration rollback.
```

## Approval notes

```text
notes
```

## Final decision

```text
Decision status: NOT APPROVED
Live read-only pilot: blocked
Live control: blocked
MQTT: blocked
Production wiring: blocked
```
