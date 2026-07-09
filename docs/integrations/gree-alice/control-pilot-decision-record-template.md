# GREE-ALICE control pilot decision record template

Decision status: NOT APPROVED

This template is a manual decision record for a possible future single-device control pilot. It is not approval by itself.

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

## Approved command list

```text
not-approved
```

## Approved command limits

```text
not-approved
```

## Approved temperature range

```text
18..30 C candidate range only; not-approved
```

## Approved mode list

```text
auto, cool, heat, dry, fan candidate list only; not-approved
```

## Approved fan/swing limits

```text
fan auto/low/medium/high candidate list only; swing not-approved
```

## Audit/logging confirmation

```text
Audit event format approved: yes/no
Raw credentials logged: no
Masked identifiers only: yes/no
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
All account and device identifiers in repository docs/tests are masked: yes/no
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
Live control: blocked
Control adapter: fail-closed
Single-device pilot: not approved
MQTT: blocked
Production wiring: blocked
```
