# GREE-ALICE minimal production pilot decision record template

Decision status: NOT APPROVED

This template is a manual decision record for a possible future minimal production pilot. It is not approval by itself.

Real operator, account, home, device, and VRF child-unit values must stay outside the repository. Credentials, tokens, passwords, keys, MAC addresses, PCAP, CSV, and raw artifacts must stay outside the repository. Use masked identifiers in this document.

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

## Pilot type

```text
blocked
```

## Exact operator

```text
masked-operator-001
```

## Exact account scope

```text
masked-account-001
```

## Exact home scope

```text
masked-home-001
```

## Exact device / VRF child-unit scope

```text
masked-device-or-child-unit-001
```

## Read-only-first confirmation

```text
Read-only-first accepted: yes/no
```

## Control approval reference if any

```text
none
```

## Production wiring review

```text
Production wiring remains disabled: yes/no
```

## Audit/logging confirmation

```text
Audit event format approved: yes/no
Masked identifiers only: yes/no
Raw secrets logged: no
```

## Monitoring confirmation

```text
Monitoring plan documented: yes/no
```

## Kill-switch confirmation

```text
Kill-switch plan documented: yes/no
```

## Rollback confirmation

```text
Rollback plan documented: yes/no
```

## Evidence location

```text
masked-external-evidence-location
```

## Masked identifiers confirmation

```text
All account, home, device, child-unit, and operator identifiers in repository docs/tests are masked: yes/no
```

## Credential storage confirmation

```text
Credential material is stored only outside the repository: yes/no
```

## Approval notes

```text
notes
```

## Final decision

```text
Decision status: NOT APPROVED
Minimal production pilot: blocked
Production wiring: disabled
Live read-only pilot: disabled unless separately approved
Live control: disabled
MQTT: blocked
```
