# Calculation Module Boundary Policy

## Purpose

This policy protects `AssistantEngineer.Modules.Calculations` while the calculation module is deepened.

## Hard boundaries

The calculations module must not depend on:

- EF Core persistence details;
- ASP.NET Core controllers;
- Infrastructure report generators;
- ClosedXML;
- frontend DTOs;
- database migrations;
- hidden environment-specific state.

## Allowed responsibilities

The calculations module may contain:

- calculation contracts;
- calculation input/output models;
- calculation engines;
- input factories from application/read-model shapes;
- diagnostics;
- formula documentation;
- internal deterministic fixtures;
- orchestration services that remain application-layer pure.

## Diagnostics rules

Mandatory invalid inputs should be errors.

Optional assumptions should be warnings.

Informational traceability should be info diagnostics.

Successful calculation results must not carry error diagnostics.

No fallback may be silent.

## Method disclosure rules

A simplified method must be labelled as simplified.

An ISO-inspired method must not be labelled as full ISO implementation.

A placeholder validation fixture must not be labelled as real EnergyPlus validation.

A real EnergyPlus comparison must remain tolerance-based and must not claim exact numerical parity.

## Reporting boundary

The Reporting module may consume calculation results.

The Calculations module must not know about Excel rendering, PDF rendering or ClosedXML.

## Future extensibility

New calculation methods should be introduced behind explicit method identifiers and contracts.

The application pipeline should select methods deliberately rather than through hidden defaults.
