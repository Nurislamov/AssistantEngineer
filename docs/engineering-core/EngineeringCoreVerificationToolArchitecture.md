# Engineering Core Verification Tool Architecture

## Purpose

The Engineering Core V1 verification profile must be owned by a C# tool.

The PowerShell script `scripts/engineering-core/verify-engineering-core-v1.ps1` must remain a thin wrapper.

## Responsibilities

The C# verification tool owns:

- frontend build invocation;
- Engineering Core guard test ordering;
- generated evidence refresh calls;
- validation harness checks;
- optional full backend test suite invocation.

## Non-responsibilities

The wrapper script must not contain the verification step list.

The wrapper script must not contain release, validation or generation logic.

## Current tool

```text
tools/AssistantEngineer.Tools.EngineeringCoreVerification
```

## Internal structure

The entrypoint `Program.cs` stays thin and delegates orchestration to focused components:

- `EngineeringCoreVerificationCommandRouter` for command parsing and verification step routing;
- `EngineeringCoreVerificationCommandHandler` for per-step execution and exit-code propagation;
- `EngineeringCoreVerificationReportWriter` and `EngineeringCoreVerificationDiagnosticsFormatter` for deterministic console/report formatting;
- `EngineeringCoreVerificationFileSystem` for repository root discovery and text-file traversal;
- `EngineeringCoreVerificationProcessRunner` for process launching and Windows command shim resolution;
- `EngineeringCoreVerificationPolicyGuards` for forbidden-claim and external-comparison foundation checks.
