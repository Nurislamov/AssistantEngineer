# Yandex Provider Manual Smoke Plan

## Purpose

Define a local/offline manual smoke plan for future Yandex provider readiness review.

## Scope

This plan covers only offline build, tests, static scans, and local contract checks.

## Prerequisites

Use a clean local repository checkout with no real credentials, tokens, account identifiers, device identifiers, production URLs, or live provider registration data.

## Commands

```powershell
dotnet restore .\AssistantEngineer.sln
dotnet build .\AssistantEngineer.sln --no-restore
dotnet test .\AssistantEngineer.sln --no-build
git diff --check
git status --short
```

## Expected Results

All commands pass. No production wiring, MQTT, live Gree+ Cloud control, live Yandex calls, real OAuth, or real credential material is present.

## /devices Offline Check

Verify the offline `/devices` contract returns dummy split AC and reviewed VRF child units only.

## /query Offline Check

Verify `/query` returns offline fixture state or controlled unknown state.

## /action Fail-Closed Check

Verify `/action` remains dry-run fail-closed and sends nothing to Gree Cloud, MQTT, or devices.

## /unlink Offline Check

Verify `/unlink` remains offline-only and does not touch production data.

## Account Linking Dummy/Template Check

Verify the account-linking boundary remains offline-template and uses only masked/dummy/template references.

## Scoped Registry Dummy/Template Check

Verify known dummy binding resolves only explicit scoped devices and unknown/unlinked users fail closed.

## VRF Child-Unit Exposure Check

Verify VRF gateway remains internal and child units are exposed only as reviewed Yandex user devices.

## Unknown User/Device Fail-Closed Check

Verify unknown users receive no scoped devices and unknown devices return controlled offline unknown/fail-closed behavior.

## Safety Scans

Run static scans for no secrets, no MQTT, no live HTTP implementation, no device control, no production wiring, and no real import artifacts.

## Evidence Format

Record commit, command output summary, test count, smoke observations, security review status, operator, timestamp, and final decision.

## What Must Not Be Tested Yet

Do not test real Yandex provider registration, real OAuth, production callback URLs, real credentials/tokens, live calls to Yandex, live Gree+ Cloud control, MQTT, device control, production deployment, or production runtime configuration.
