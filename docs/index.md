---
title: Grex Documentation
layout: default
---

# Grex Documentation

Grex is a Windows file-search application and CLI for local drives, UNC shares, WSL distributions, and Docker containers.

These docs describe the current implementation, including destructive behavior and target-specific limitations.

## Start here

| Goal | Read |
| --- | --- |
| Install or update Grex | [Usage: Install, update, or remove](usage.md#install-update-or-remove-grex) |
| Run the first search | [Usage: Run a search](usage.md#run-a-local-unc-or-wsl-search) |
| Understand filters | [Usage: Filters](usage.md#filters) |
| Replace safely | [Usage: Replace text](usage.md#replace-text) |
| Search Docker | [Usage: Docker](usage.md#search-docker-containers) |
| Search WSL | [Usage: WSL](usage.md#search-wsl) |
| Configure AI | [Usage: AI chat](usage.md#use-ai-chat) |
| Automate with the CLI | [Technical Reference: CLI](reference.md#cli-reference) |
| Troubleshoot | [Usage: Troubleshooting](usage.md#troubleshooting) |
| Build or test | [Build and Test](build-and-test.md) |
| Contribute a translation | [Translation and Localization](translations.md) |
| Report a vulnerability | [Security and Privacy](https://github.com/visorcraft/Grex/blob/master/SECURITY.md) |

## Documentation set

### User documentation

- [README](https://github.com/visorcraft/Grex#readme): product overview, downloads, quick start, and important warnings
- [Usage Guide](usage.md): complete installation and GUI workflow
- [Feature Matrix](features.md): exact target support and capability boundaries
- [Technical Reference](reference.md): shortcuts, syntax, schemas, files, CLI, protocols, outputs, and caps
- [Security and Privacy](https://github.com/visorcraft/Grex/blob/master/SECURITY.md): local writes, keys, network activity, Docker, WSL, logs, and reporting

### Developer documentation

- [Architecture](architecture.md): services, ViewModels, code-behind, search flows, persistence, and failure strategy
- [Build and Test](build-and-test.md): Windows setup, commands, test projects, package artifacts, and release automation
- [Translation and Localization](translations.md): catalogs, status comments, scripts, tests, and CRLF rules
- [Regex Builder Localization](regex-localization.md): Regex-specific resource families and validation workflow
- [Contributing](https://github.com/visorcraft/Grex/blob/master/CONTRIBUTING.md): workflow, code standards, tests, docs, dependencies, and release checklist
- [Third-party Notices](https://github.com/visorcraft/Grex/blob/master/THIRD-PARTY-NOTICES.txt): redistributed components and license texts

## Important boundaries

- GUI, CLI, and .NET tests are Windows-only.
- Published releases target win-x64.
- Replace has no undo, transaction, or automatic backup.
- Docker replacement is disabled.
- WSL replacement filters eligible text files, uses `sed -i -E`, and treats replacement text literally.
- Direct Docker search and mirror fallback do not have identical filters.
- Binary/document search is best effort and has no OCR; replacement skips those formats.
- AI chat does not automatically send file contents, but does send path/query/filter metadata and conversation text.
- AI keys and settings exports are plain text.
- Non-English resource catalogs are incomplete.
- The app has no automatic updater or application telemetry implementation.

## Releases and support

- [Latest release](https://github.com/visorcraft/Grex/releases/latest)
- [All releases](https://github.com/visorcraft/Grex/releases)
- [Issues](https://github.com/visorcraft/Grex/issues)
- [Private vulnerability report](https://github.com/visorcraft/Grex/security/advisories/new)

For a bug report, include Grex version, Windows build, target type, path form, query mode, filters, reproduction steps, and relevant redacted `%Temp%\Grex.log` lines.

## Documentation maintenance

Behavior changes should update one authoritative page:

| Change | Page |
| --- | --- |
| Install or user workflow | `usage.md` |
| Capability or target support | `features.md` |
| Option, schema, path, output, or limit | `reference.md` |
| Component or data flow | `architecture.md` |
| Build, test, package, or release | `build-and-test.md` |
| Resource process | `translations.md` |
| Data handling, network, destructive behavior | root `SECURITY.md` |

Avoid duplicate implementation reports. Link to the authoritative page instead.
