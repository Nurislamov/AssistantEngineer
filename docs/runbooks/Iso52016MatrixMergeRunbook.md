# ISO 52016 Matrix merge runbook

Use this runbook before merging the `iso52016-matrix-only` branch.

## 1. Verify local state

```powershell
git status
git log --oneline -10
```

Expected:

```text
nothing to commit, working tree clean
```

## 2. Run release-ready gate

```powershell
.\scripts\iso52016\assert-iso52016-matrix-release-ready.ps1 -RequireCleanGit
```

This runs the Matrix verification chain, the full test project, generated-artifact checks and clean-git checks.

## 3. Generate merge summary

```powershell
.\scripts\iso52016\write-iso52016-matrix-merge-summary.ps1 -SkipVerification
```

Generated files:

```text
artifacts/iso52016/matrix-merge-summary/merge-summary.md
artifacts/iso52016/matrix-merge-summary/merge-summary.json
```

These files are generated outputs and should not be committed.

## 4. Check generated artifacts are not tracked

```powershell
git ls-files artifacts/iso52016/matrix-baselines
git ls-files artifacts/iso52016/matrix-merge-summary
```

Both commands should return no tracked files.

## 5. Merge policy

Do not merge if any of these fail:

- full test project;
- `assert-iso52016-matrix-release-ready.ps1`;
- CI workflow;
- generated artifact tracking check.