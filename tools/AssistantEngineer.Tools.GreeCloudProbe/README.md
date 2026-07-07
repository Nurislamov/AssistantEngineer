# AssistantEngineer.Tools.GreeCloudProbe

Console tool for the GREE-ALICE cloud probe.

## Purpose

This tool checks whether a Gree+ Cloud account can be used for the future Alice / Yandex Smart Home bridge.

It validates:

- selected region / server URL;
- Gree+ Cloud login;
- homes;
- rooms;
- devices;
- split AC candidates;
- VRF gateway / child-unit candidates;
- masked diagnostic output.

The tool is intentionally isolated from production runtime. It does not touch `AssistantEngineer.Api`, Telegram bot, deployment files, runtime database, or migrations.

## Safe local run

Set credentials only in the current PowerShell session:

```powershell
cd D:\Project\AssistantEngineer

$env:GREE_ALICE_GREE_USERNAME = "your_gree_plus_login"
$env:GREE_ALICE_GREE_PASSWORD = "your_gree_plus_password"
$env:GREE_ALICE_GREE_REGION = "Ouzbekistan"

dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer"
```

Clear local variables after the run:

```powershell
Remove-Item Env:\GREE_ALICE_GREE_USERNAME -ErrorAction SilentlyContinue
Remove-Item Env:\GREE_ALICE_GREE_PASSWORD -ErrorAction SilentlyContinue
Remove-Item Env:\GREE_ALICE_GREE_REGION -ErrorAction SilentlyContinue
```

## Configuration-only run

Use this mode to verify local configuration and artifact writing without cloud login:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --repo-root "D:\Project\AssistantEngineer" `
  --configuration-only
```

## Region / server override

Default region for this project is:

```text
Ouzbekistan
```

The current validated mapping for this project is East South Asia Gree+ Cloud server.

If required, override the exact server URL:

```powershell
$env:GREE_ALICE_GREE_SERVER_URL = "https://hkgrih.gree.com"
```

or:

```powershell
dotnet run --project .\tools\AssistantEngineer.Tools.GreeCloudProbe\AssistantEngineer.Tools.GreeCloudProbe.csproj -- `
  --server-url "https://hkgrih.gree.com"
```

## Output

Default output directory:

```text
artifacts/gree-alice/probe/
```

The `artifacts/` folder is ignored by Git.

The report masks sensitive values by default.

## Supported environment variables

```text
GREE_ALICE_GREE_REGION
GREE_ALICE_GREE_SERVER_URL
GREE_ALICE_GREE_USERNAME
GREE_ALICE_GREE_PASSWORD
GREE_ALICE_OUTPUT_DIR
GREE_ALICE_TIMEOUT_SECONDS
GREE_ALICE_SAVE_RAW_RESPONSE
GREE_ALICE_MASK_SECRETS
```

## Current stage

`GREE-ALICE-03` adds real Gree+ Cloud login and device discovery to the probe tool.

The next stage should use the probe output to define the first internal device model for split and VRF candidates.

## Validated project region mapping

In the Gree+ app the Russian UI can show Uzbekistan, while the selected value can later be displayed as:

```text
Ouzbékistan
```

Validated cloud server for this account:

```text
East South Asia / https://hkgrih.gree.com
```
