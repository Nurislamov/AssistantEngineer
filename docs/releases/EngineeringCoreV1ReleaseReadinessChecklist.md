# Engineering Core V1 Release Readiness Checklist

## Commands

Run release readiness scripts.

Default release gate (frontend checks enabled):

    dotnet restore AssistantEngineer.sln
    dotnet build AssistantEngineer.sln --no-restore
    dotnet test AssistantEngineer.sln
    npm --prefix .\src\Frontend ci
    npm --prefix .\src\Frontend run build
    .\scripts\engineering-core\verify-engineering-core-v1.ps1
    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

Emergency-only override (manual/local fallback, not release path):

    .\scripts\engineering-core\verify-engineering-core-v1.ps1 -SkipFrontend

## Core closure

Closed formula gates are documented.

## Weather and annual energy

Weather and annual 8760 checks are present.

## User visibility

Frontend/report visibility is present.

## Generated contracts

API/report contract snapshots are present.

## Future validation

EnergyPlus validation is planned.

## Required non-claims

EnergyPlus comparison workflow achieved: no.
ASHRAE 140 / BESTEST-style validated: no.
Full ISO 52016 implemented: no.

## Decision

Engineering Core V1 is closed as an engineering formula gate with documented limitations.
