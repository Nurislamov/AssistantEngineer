# AssistantEngineer

AssistantEngineer is a standalone C# engineering calculation platform for HVAC and building-energy workflows.

- Backend: .NET 10, ASP.NET Core, EF Core, PostgreSQL.
- Frontend: React, Vite, TypeScript, MUI, React Query.
- Reports: Excel generation through ClosedXML in `AssistantEngineer.Infrastructure.Integrations.Reports`.

## Current MVP Capabilities

- Project, building, floor, room, wall, window, thermal-zone, ventilation, ground-contact, and equipment-catalog CRUD.
- Project selection in the frontend without relying on a default project environment variable.
- Building and room heating/cooling calculations.
- Building energy-balance calculation with annual/monthly/peak values when available.
- Cooling JSON report, heating JSON report, cooling Excel report, and energy-balance Excel report.
- Room equipment selection from the active catalog.
- Development-only deterministic demo data seed.
- Deterministic standard-reference fixtures and analytical validation anchors for internal engineering workflows.

## Not Implemented Yet

- Authentication, users, roles, and tenant isolation.
- Telegram bot integration.
- PDF reports.
- Diagram editor.
- Full external validation proof. Existing fixtures are deterministic reference fixtures unless a test names a documented external source and tolerance workflow.

## Architecture Overview

The backend is a modular monolith. `AssistantEngineer.Api` composes public module facades. Domain and application modules stay independent from EF Core, ASP.NET Core, UI, and Infrastructure implementation details. Persistence and report integrations live under `AssistantEngineer.Infrastructure`.

Important constraints:

- `AssistantEngineer.Modules.*` projects must not reference `AssistantEngineer.Infrastructure`.
- `AssistantEngineer.Api` should not contain persistence details.
- `AssistantEngineer.Modules.Reporting` must not reference ClosedXML directly.
- ClosedXML belongs only in `AssistantEngineer.Infrastructure.Integrations.Reports`.
- Equipment selection must not depend on Buildings or Calculations implementation internals.

## Validation Position

AssistantEngineer implements calculation logic directly in C# using standard-based engineering methods, including EN ISO 52010, EN ISO 52016, EN 16798, EN 12831-3, EN 15316, and ISO 13370. Validation is handled through analytical reference cases, internal engineering anchors, EnergyPlus comparison workflows, and ASHRAE 140 / BESTEST-style validation anchors where applicable.

## Real Application Pipeline

The backend calculation endpoints use the Energy Calculation equivalence application pipeline through public facades:

- Room heating/cooling routes assemble room component inputs and call `RoomLoadCalculationEngine`.
- Floor and building heating/cooling routes aggregate room load results with `LoadAggregationEngine`.
- Building energy balance maps true hourly simulation records into `AnnualEnergyBalanceEngine` when available; otherwise it uses the documented MonthlyBalanceAdapter fallback with source diagnostics and `isTrueHourly8760 = false`.
- DHW demand uses the deterministic DHW service path.
- Heating/cooling system energy uses `SystemEnergyEngine` so useful, final and primary energy remain distinct.
- Room equipment selection uses actual room load, separate project heating/cooling safety factors, `EquipmentSizingEngine`, and the active equipment catalog.
- Cooling, heating and energy-balance reports consume facade results from the same pipeline. Excel rendering stays in Infrastructure.

## Backend Setup

Prerequisites:

- .NET SDK 10.
- PostgreSQL 16+ or 17+.
- Docker, if you want a local PostgreSQL container or Docker-based EnergyPlus.

Restore and build:

```powershell
dotnet restore
dotnet build AssistantEngineer.sln
```

## PostgreSQL Setup

Start PostgreSQL locally with Docker:

```powershell
docker run --name assistantengineer-postgres `
  -e POSTGRES_DB=AssistantEngineerDb `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -d postgres:17
```

Set the connection string for local commands:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=AssistantEngineerDb;Username=postgres;Password=postgres"
```

## Migrations

Install or update the EF CLI if needed:

```powershell
dotnet tool update --global dotnet-ef --version 10.*
```

Apply migrations:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=AssistantEngineerDb;Username=postgres;Password=postgres"
dotnet ef database update `
  --project src/Backend/AssistantEngineer.Infrastructure `
  --startup-project src/Backend/AssistantEngineer.Infrastructure
```

## API Run

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=AssistantEngineerDb;Username=postgres;Password=postgres"
dotnet run --project src/Backend/AssistantEngineer.Api --launch-profile http
```

Default HTTP URL: `http://localhost:5194`.

Interactive API examples are in `src/Backend/AssistantEngineer.Api/AssistantEngineer.Api.http`.

## Demo Data

After migrations and API startup, seed deterministic MVP demo data in Development:

```powershell
Invoke-RestMethod -Method Post http://localhost:5194/api/v1/development/demo-data/seed
```

This endpoint is intentionally unavailable outside `Development`. The seed includes a climate zone, annual/hourly weather, project, building, floor, room, wall, window, ventilation parameters, ground-contact metadata, and two active equipment catalog items.

## Frontend Run

```powershell
cd src/Frontend
npm ci
copy .env.example .env
npm run dev
```

Default Vite URL: `http://localhost:5173`.

The frontend uses `VITE_API_BASE_URL=http://localhost:5194` by default. Project selection is handled in the UI.

## Import EPW/PVGIS

EPW import:

```powershell
curl.exe -X POST "http://localhost:5194/api/v1/climate-zones/1/annual-climate-data/epw" `
  -F "year=2020" `
  -F "sourceFile=@C:\weather\Tashkent.epw"
```

PVGIS TMY import:

```powershell
curl.exe -X POST "http://localhost:5194/api/v1/climate-zones/1/annual-climate-data/pvgis" `
  -H "Content-Type: application/json" `
  -d "{ \"latitude\": 41.3111, \"longitude\": 69.2797, \"year\": 2020 }"
```

## Tests

```powershell
dotnet restore
dotnet test AssistantEngineer.sln

cd src/Frontend
npm ci
npm run build
```

If a lint script is added later, run:

```powershell
npm run lint
```

## Reports

Current MVP report routes:

- `GET /api/v1/reports/buildings/{buildingId}/cooling`
- `GET /api/v1/reports/buildings/{buildingId}/cooling/excel`
- `GET /api/v1/reports/buildings/{buildingId}/heating`
- `GET /api/v1/reports/buildings/{buildingId}/energy-balance/excel`

Excel files are generated in Infrastructure. The Reporting module uses public contracts and does not reference ClosedXML.

## EnergyPlus Benchmark

Build the existing EnergyPlus Docker image:

```powershell
docker build -t assistant-engineer-energyplus:24.1 -f docker/energyplus/Dockerfile docker/energyplus
```

Run the API with Docker-based EnergyPlus:

```powershell
$env:EnergyPlus__UseDocker = "true"
$env:EnergyPlus__DockerImage = "assistant-engineer-energyplus:24.1"
dotnet run --project src/Backend/AssistantEngineer.Api --launch-profile http
```

Benchmark routes:

- `POST /api/v1/benchmarks/energyplus/buildings/{buildingId}/model`
- `POST /api/v1/benchmarks/energyplus`
- `POST /api/v1/benchmarks/buildings/{buildingId}/verify?method=Iso52016`
- `GET /api/v1/benchmarks/iso52016/reference-cases`

## Current API Routes

Core project/building CRUD:

- `GET/POST /api/v1/projects`
- `GET/PUT/DELETE /api/v1/projects/{id}`
- `GET/POST /api/v1/projects/{projectId}/buildings`
- `GET/PUT/DELETE /api/v1/buildings/{id}`
- `GET/POST /api/v1/buildings/{buildingId}/floors`
- `GET/PUT/DELETE /api/v1/floors/{id}`
- `GET/POST /api/v1/rooms`
- `GET /api/v1/buildings/{buildingId}/rooms`
- `GET/PUT/DELETE /api/v1/rooms/{id}`

Envelope and zones:

- `GET/POST /api/v1/rooms/{roomId}/walls`
- `PUT/DELETE /api/v1/rooms/{roomId}/walls/{wallId}`
- `GET/POST /api/v1/rooms/{roomId}/windows`
- `PUT/DELETE /api/v1/rooms/{roomId}/windows/{windowId}`
- `GET/POST /api/v1/buildings/{buildingId}/thermal-zones`
- `GET/PUT/DELETE /api/v1/thermal-zones/{id}`

Ventilation, ground, and readiness:

- `GET/PUT/DELETE /api/v1/rooms/{roomId}/ventilation-parameters`
- `GET /api/v1/rooms/{roomId}/ventilation-parameters/defaults`
- `POST /api/v1/rooms/{roomId}/ventilation-parameters/apply-defaults`
- `POST /api/v1/rooms/{roomId}/natural-ventilation/preview`
- `GET/PUT/DELETE /api/v1/rooms/{roomId}/ground-contact`
- `GET /api/v1/buildings/{buildingId}/readiness`
- `GET /api/v1/buildings/{buildingId}/validation`
- `POST /api/v1/buildings/{buildingId}/validation/autocorrect-preview`
- `POST /api/v1/buildings/{buildingId}/validation/autocorrect-apply`

Calculations and reports:

- `GET /api/v1/buildings/{buildingId}/load-calculations/cooling-load`
- `GET /api/v1/buildings/{buildingId}/load-calculations/heating-load`
- `GET /api/v1/buildings/{buildingId}/load-calculations/energy-balance`
- `GET /api/v1/floors/{floorId}/load-calculations/cooling-load`
- `GET /api/v1/floors/{floorId}/load-calculations/heating-load`
- `GET /api/v1/rooms/{roomId}/load-calculations/cooling-load`
- `GET /api/v1/rooms/{roomId}/load-calculations/heating-load`
- `GET /api/v1/reports/buildings/{buildingId}/cooling`
- `GET /api/v1/reports/buildings/{buildingId}/cooling/excel`
- `GET /api/v1/reports/buildings/{buildingId}/heating`
- `GET /api/v1/reports/buildings/{buildingId}/energy-balance/excel`

Equipment and climate:

- `GET/POST /api/v1/equipment-catalog`
- `GET/PUT/DELETE /api/v1/equipment-catalog/{id}`
- `POST /api/v1/rooms/{roomId}/equipment-selection`
- `POST /api/v1/climate-zones/{climateZoneId}/annual-climate-data/epw`
- `POST /api/v1/climate-zones/{climateZoneId}/annual-climate-data/pvgis`

Development:

- `POST /api/v1/development/demo-data/seed`

## Known Limitations

- Heating/cooling and energy-balance calculations are MVP-level workflows, not full external standards certification.
- EnergyPlus integration depends on Docker configuration and available weather/model artifacts.
- Excel report coverage is limited to current cooling and energy-balance routes.
- Development seed data is deterministic but intentionally small.

## MVP User Flow

1. Start PostgreSQL and apply migrations.
2. Start the API in `Development`.
3. Optionally seed demo data with `POST /api/v1/development/demo-data/seed`.
4. Start the frontend.
5. Create or select a project.
6. Create a building, floor, and room.
7. Add wall and window envelope data.
8. Set ventilation and ground-contact data.
9. Run cooling, heating, and energy-balance calculations.
10. Download Excel reports.
11. Add active equipment catalog items.
12. Run room equipment selection and inspect the selected/recommended unit.

## MVP Verification Checklist

- [ ] Create project
- [ ] Create building
- [ ] Create floor
- [ ] Create room
- [ ] Add wall/window
- [ ] Set ventilation
- [ ] Set ground contact
- [ ] Run cooling/heating calculation
- [ ] Run energy balance
- [ ] Download Excel report
- [ ] Add equipment
- [ ] Run equipment selection

---

## Engineering Core V1

Engineering Core V1 is the closed calculation-core layer for HVAC engineering calculations.

Current status:

    Engineering Core V1 = ClosedV1 as an engineering formula gate

Covered in V1:

- design-point heating load;
- design-point cooling load;
- transmission heat transfer;
- ventilation and infiltration sensible loads;
- internal sensible gains;
- simplified SHGC window solar gains;
- ISO52010-inspired solar position and isotropic surface irradiance;
- room/floor/building aggregation;
- simplified hourly heat balance;
- single-zone calculation path;
- EPW/PVGIS 8760 weather import gates;
- annual true hourly 8760 integration;
- simplified ground heat transfer;
- simplified adjacent boundary handling;
- simplified DHW;
- simplified system final/primary energy;
- equipment capacity sizing.

Out of scope for V1:

- latent load;
- moisture balance;
- detailed psychrometrics;
- detailed HVAC plant simulation;
- exact EnergyPlus numerical equivalence;
- exact StandardReference numerical equivalence;
- ASHRAE 140 / BESTEST-style validation anchor coverage;
- full ISO 52016 node/matrix solver equivalence.

Main verification command:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1

Full local verification profile:

    dotnet restore AssistantEngineer.sln
    dotnet build AssistantEngineer.sln --no-restore
    dotnet test AssistantEngineer.sln
    npm --prefix .\src\Frontend ci
    npm --prefix .\src\Frontend run build
    .\scripts\engineering-core\verify-engineering-core-v1.ps1
    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

Fast local profile:

    .\scripts\engineering-core\verify-engineering-core-v1-smoke.ps1

Contracts profile:

    .\scripts\engineering-core\verify-engineering-core-v1-contracts.ps1

Release readiness gate:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

Emergency frontend override (manual use only; not a normal gate path):

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -SkipFrontend

Key documentation:

- Engineering Core V1 documentation index: docs/engineering-core/README.md
- Engineering Core V1 release manifest: docs/releases/EngineeringCoreV1ReleaseManifest.md
- Engineering Core V1 release readiness checklist: docs/releases/EngineeringCoreV1ReleaseReadinessChecklist.md
- Engineering Core V1 release evidence: docs/reports/EngineeringCoreV1ReleaseEvidence.md
- Engineering Core V1 traceability matrix: docs/traceability/EngineeringCoreV1TraceabilityMatrix.md
- EnergyPlus / ASHRAE 140-style validation registry: docs/validation/EnergyPlusValidationCaseRegistry.md

Recommended release wording:

    Engineering Core V1 is closed as an engineering formula gate with documented limitations.

Forbidden release wording:

    EnergyPlus comparison workflow achieved.
    ASHRAE 140 / BESTEST-style validated.
    Full ISO 52016 implemented.
