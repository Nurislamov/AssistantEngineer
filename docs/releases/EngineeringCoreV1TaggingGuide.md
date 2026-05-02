# Engineering Core V1 Tagging Guide

## Purpose

This guide describes how to tag Engineering Core V1 after the release readiness gate is green.

## Pre-tag requirements

Run:

    .\scripts\engineering-core\regenerate-engineering-core-v1-artifacts.ps1
    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

Then check:

    git status

Only tag when the working tree is clean or when all intended generated files are committed.

## Recommended tag names

Recommended:

    engineering-core-v1
    engineering-core-v1.0.0

Use one stable tag style consistently.

## Tag command

Annotated tag:

    git tag -a engineering-core-v1 -m "Engineering Core V1 closed as engineering formula gate"

Push tag:

    git push origin engineering-core-v1

## Release title

Recommended title:

    Engineering Core V1 - Closed as engineering formula gate

## Release description

Use:

    Engineering Core V1 is closed as an engineering formula gate with documented limitations.

Mention:

- formula gates closed;
- diagnostics and disclosure visible;
- EPW/PVGIS 8760 gates closed;
- annual true hourly 8760 rule;
- API/frontend/report contracts;
- release evidence;
- traceability matrix;
- validation registry as future planned validation.

## Required non-claims

Include:

- no exact EnergyPlus numerical parity claim;
- no exact pyBuildingEnergy numerical parity claim;
- no ASHRAE 140 validation coverage claim;
- no full ISO 52016 node/matrix solver parity claim;
- no latent/moisture/humidity support in V1.

## Links to include

- docs/releases/EngineeringCoreV1PublicReleaseNotes.md
- docs/releases/EngineeringCoreV1ReleaseManifest.md
- docs/releases/EngineeringCoreV1ReleaseReadinessChecklist.md
- docs/reports/EngineeringCoreV1ReleaseEvidence.md
- docs/traceability/EngineeringCoreV1TraceabilityMatrix.md
- docs/engineering-core/README.md

## After tagging

After pushing the tag:

    git status
    git log --oneline -5
    git tag --list "engineering-core-v1*"

Then continue with the next phase:

- first real EnergyPlus smoke fixture;
- validation report generation;
- future psychrometrics module planning;
- equipment part-load/performance curve planning.
