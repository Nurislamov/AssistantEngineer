# OwnershipBackfill CLI Parser Simplification (P8-04)

## Purpose

Reduce `OwnershipBackfillCommandLineParser` size and responsibility while preserving existing CLI semantics and safety boundaries.

## Scope

- Internal parser decomposition for command descriptors and argument reading.
- Behavior-lock test expansion for parser routing and parse error safety.
- Governance/documentation updates for P8-04.

## Non-claims

- No ownership backfill execution claim.
- No production apply enabled claim.
- No staging apply execution claim.
- No DB write-path enabled claim.
- No public API route change claim.
- No calculation physics change claim.
- No production security certification claim.

## Current parser responsibilities

Before P8-04, command routing and per-command parse orchestration were concentrated in one large parser class with repeated top-level command-dispatch blocks.

## Simplification approach

- Introduce `OwnershipBackfillCommandDescriptor` and `OwnershipBackfillCommandDescriptorCatalog` for command metadata.
- Introduce `OwnershipBackfillArgumentReader` for shared `--key value` read behavior and safe parse-error shape.
- Keep per-command parse semantics and validations unchanged in parser command-specific branches.
- Move top-level command dispatch to descriptor-catalog lookup plus command-type parser map.

## Command descriptor model

Each descriptor captures:

- command name and command type;
- required, optional, and flag arguments;
- help support;
- usage summary;
- apply-enabled metadata (remains disabled for `apply`).

## Preserved CLI contracts

- Command names unchanged.
- Existing argument names unchanged.
- Exit-code meanings unchanged (`0`, `1`, `2`).
- Help output semantics preserved (`--help` and `<command> --help`).
- Apply-disabled message and disabled boundary preserved.

## Redaction/exit-code compatibility

- Parser/runtime errors continue to route through `OwnershipBackfillConsoleRedactor`.
- Secret-like values are not printed in parser/CLI error output.
- Validation vs invalid-input exit-code boundaries are unchanged.

## Apply disabled compatibility

- `apply` command remains parseable but execution-disabled.
- Disabled message remains:
  `Apply mode is designed but disabled in P6-04. No ownership metadata was written.`
- No DB write-path enablement is introduced.

## Files changed

- `tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillCommandLineParser.cs`
- `tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillHelpText.cs`
- `tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillCommandDescriptor.cs`
- `tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillCommandDescriptorCatalog.cs`
- `tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillArgumentReader.cs`
- `tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillParsedArguments.cs`
- `tools/AssistantEngineer.Tools.OwnershipBackfill/Cli/OwnershipBackfillCommandParseError.cs`
- `tests/AssistantEngineer.Tests/Tools/OwnershipBackfill/OwnershipBackfillCommandLineParserCharacterizationTests.cs`
- `tests/AssistantEngineer.Tests/Tools/OwnershipBackfill/OwnershipBackfillCommandDescriptorCatalogTests.cs`
- `tests/AssistantEngineer.Tests/Tools/OwnershipBackfill/OwnershipBackfillArgumentReaderTests.cs`
- `tests/AssistantEngineer.Tests/Tools/OwnershipBackfill/OwnershipBackfillCommandLineParserSimplificationTests.cs`
- `tests/AssistantEngineer.Tests/Architecture/P8OwnershipBackfillCliParserSimplificationTests.cs`

## Remaining parser responsibilities

- Per-command option validation and options model construction remain in `OwnershipBackfillCommandLineParser`.
- Command-specific validation logic remains intentionally unchanged for semantics stability.

## Deferred items

- Optional future split of command-specific parse branches into per-command parser types after additional characterization.

## Verification

- `dotnet build AssistantEngineer.sln -c Debug`
- `dotnet test AssistantEngineer.sln -c Debug`
- `dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- --help`
- Disabled apply guard check with fake connection string (must exit non-zero and redact secret-like values).
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1`
