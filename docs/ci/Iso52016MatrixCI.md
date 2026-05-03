# ISO 52016 Matrix CI gate

The workflow is:

```text
.github/workflows/iso52016-matrix-release-ready.yml
```

It runs the ISO 52016 Matrix release-ready gate on pull requests, pushes to key branches, and manual dispatch.

## Main CI command

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -RequireCleanGit
```

The command runs:

- the full ISO 52016 Matrix verification chain;
- the full `AssistantEngineer.Tests` test project;
- the generated-artifact git tracking check;
- clean working tree check.

## Triggered paths

The workflow runs when changes touch:

- calculation module source;
- API source;
- tests;
- ISO 52016 Matrix docs/manifests;
- ISO 52016 scripts;
- the workflow file itself.

## Generated artifacts

Generated files under:

```text
artifacts/iso52016/matrix-baselines/
```

must not be tracked by git. The release-ready script enforces this.