# Naming Conventions

## Purpose

This document defines architecture naming conventions used in AssistantEngineer.
It exists to keep module boundaries explicit and prevent accidental naming drift.

## Suffix Glossary

- `Calculator`: pure deterministic calculation without IO.
- `Engine`: internal stateless composition of calculators.
- `Service`: application operation with IO, state access, or external dependencies.
- `Facade`: public module entrypoint.
- `Orchestrator`: coordination of use cases or workflow steps.
- `Builder`: assembly of complex request, model, or result objects.
- `Provider`: reference, external, or configuration data provider.
- `Mapper`: structural transformation without business decisions.
- `Policy`: decision rules.
- `Adapter`: bridge between standards, integrations, or model shapes.

## Standard Acronym Naming

- ISO 52016 interfaces use `ISo52016*`.
- Do not introduce `IIso52016*`.
- Existing normalized `ISo52016*` names are the official convention.

## Contracts vs Models vs Abstractions

- `Contracts`: public module boundary DTOs, requests, and results.
- `Models`: internal computation models.
- `Abstractions`: application interfaces.

## Service Naming Rule

- Avoid introducing new vague `*Service` names when a more precise suffix from this document applies.
- Use `Service` only when the type truly represents an application operation with IO/state/external dependencies.
