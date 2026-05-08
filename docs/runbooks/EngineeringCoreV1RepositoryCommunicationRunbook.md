# Engineering Core V1 Repository Communication Runbook

## Purpose

This runbook defines how Engineering Core V1 is presented at repository level.

It protects wording in README, release notes, announcement drafts and tagging guidance.

## Repository README

The root README must include:

- Engineering Core V1 status;
- ClosedV1 as engineering formula gate;
- main verification command;
- smoke/contracts profile commands;
- release readiness command;
- key documentation links;
- out-of-scope items;
- required non-claims;
- recommended and forbidden release wording.

## Public release notes

Public release notes must explain:

- what is included;
- what ClosedV1 means;
- what ClosedV1 does not mean;
- annual 8760 rule;
- user-visible transparency;
- verification command;
- future validation direction.

File:

    docs/releases/EngineeringCoreV1PublicReleaseNotes.md

## Announcement draft

Announcement draft must keep language careful and non-overclaiming.

File:

    docs/releases/EngineeringCoreV1AnnouncementDraft.md

Recommended wording:

    Engineering Core V1 is closed as an engineering formula gate with documented limitations.

Forbidden wording:

    No "EnergyPlus comparison workflow achieved" claim.
    No "ASHRAE 140 / BESTEST-style validated" claim.
    Full ISO 52016 implemented.

## Tagging guide

Tagging guide must require release readiness gate before tagging.

File:

    docs/releases/EngineeringCoreV1TaggingGuide.md

Required command:

    .\scripts\engineering-core\assert-engineering-core-v1-release-ready.ps1

## Guard tests

Run:

    dotnet test .\AssistantEngineer.sln --filter "EngineeringCoreV1RepositoryCommunicationTests"

## Non-claims

Repository-level communication must keep these visible:

- no exact EnergyPlus numerical equivalence;
- no exact StandardReference numerical equivalence;
- no ASHRAE 140 / BESTEST-style validation anchor coverage;
- no full ISO 52016 node/matrix solver equivalence;
- no latent/moisture/humidity support in V1.
