# AssistantEngineer.Tools.GreeCloudProbe

Scaffold console tool for the GREE-ALICE cloud probe.

## Purpose

This tool prepares the local diagnostic flow for checking Gree+ Cloud account, region, homes, rooms, devices, split AC candidates, and VRF candidates.

In `GREE-ALICE-02` it does not call Gree Cloud yet. It only validates configuration, masks sensitive values, and writes a local diagnostic report under ignored `artifacts/`.

## Usage

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --region "MiddleEast" `
  --username "user@example.com"
```

Password can be provided through an environment variable:

```powershell
$env:GREE_ALICE_GREE_PASSWORD = "..."
```

Supported environment variables:

```text
GREE_ALICE_GREE_REGION
GREE_ALICE_GREE_USERNAME
GREE_ALICE_GREE_PASSWORD
GREE_ALICE_OUTPUT_DIR
GREE_ALICE_TIMEOUT_SECONDS
GREE_ALICE_SAVE_RAW_RESPONSE
GREE_ALICE_MASK_SECRETS
```

## Output

Default output directory:

```text
artifacts/gree-alice/probe/
```

The `artifacts/` folder is ignored by Git.

## Current stage

`GREE-ALICE-02` is a scaffold-only stage.

The next stage should add actual Gree Cloud login and device discovery.
