# API key authentication boundary

AssistantEngineer now has a minimal API key authentication boundary for the HTTP API.

This is intentionally small: it is not user management, tenant isolation, OAuth, OpenID Connect, or a production identity platform. It is a protective boundary so a network-reachable API is not accidentally open.

## Configuration

The setting lives under `Authentication:ApiKey`:

```json
{
  "Authentication": {
    "ApiKey": {
      "Enabled": true,
      "HeaderName": "X-AssistantEngineer-Api-Key"
    }
  }
}
```

The key itself must not be committed to `appsettings.json`.

Use an environment variable or user secret:

```powershell
$env:Authentication__ApiKey__Key = "replace-with-a-long-random-secret"
```

or:

```powershell
dotnet user-secrets set "Authentication:ApiKey:Key" "replace-with-a-long-random-secret" --project .\src\Backend\AssistantEngineer.Api\AssistantEngineer.Api.csproj
```

Clients must send:

```http
X-AssistantEngineer-Api-Key: replace-with-a-long-random-secret
```

## Local development and tests

`appsettings.Development.json` and `appsettings.Testing.json` explicitly disable API key auth so local developer workflows and integration tests remain frictionless.

Production-like deployments should set:

```powershell
$env:Authentication__ApiKey__Enabled = "true"
$env:Authentication__ApiKey__Key = "replace-with-a-long-random-secret"
```

## Current limitation

This is a P0 security boundary only. It does not provide per-user authorization, tenant isolation, audit logging, or role policies. Those belong to the later production-readiness phase.