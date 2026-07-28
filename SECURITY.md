<!-- SPDX-FileCopyrightText: 2026 VisorCraft LLC -->
<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Security and Privacy

Grex searches user-selected files, can overwrite matches, can execute search commands in WSL and Docker containers, and can send metadata to a configured AI endpoint. Read this page before using Replace, Docker, WSL, or AI features on sensitive systems.

## Report a vulnerability

Do not open a public issue, discussion, or pull request for a suspected vulnerability.

Use [GitHub private vulnerability reporting](https://github.com/visorcraft/Grex/security/advisories/new).

Include:

- affected Grex version;
- Windows version and architecture;
- target type: local, UNC, WSL, Docker, CLI, or AI;
- impact and realistic attack scenario;
- minimal reproduction steps or proof of concept;
- relevant configuration and redacted logs;
- suggested mitigation, if known.

Do not include real API keys, private file contents, or credentials. Maintainers will coordinate validation, remediation, release, and disclosure in the private advisory.

## Supported versions

| Version | Security support |
| --- | --- |
| Latest stable release | Supported |
| Older releases | Upgrade required unless a backport is announced |
| Source snapshots | Best effort only |

The [latest GitHub release](https://github.com/visorcraft/Grex/releases/latest) and **About** page are the version sources.

## Security model

Grex is a desktop tool running with the current user's permissions. It is not a sandbox.

- A search root is a scope request, not a security boundary.
- Hidden, system, `.gitignore`, binary, and directory exclusions are convenience filters, not access controls.
- Enabling symbolic links can traverse outside the apparent root.
- UNC access uses credentials already available to Windows.
- WSL commands run with the selected distribution's current user.
- Docker access has the privileges of the configured Docker daemon connection.
- Opening a result invokes Windows, Explorer, WSL path conversion, or the default file handler.

Do not run Grex with more privilege than required.

## Local data

Grex persists:

| Data | Location | Sensitive content |
| --- | --- | --- |
| Settings | `%LocalAppData%\Grex\settings.json` | Preferences, AI endpoint, plain-text API key, model |
| Recent paths | `%LocalAppData%\Grex\search_path_history.json` | Search roots |
| Search history | `%LocalAppData%\Grex\search_history.json` | Queries, paths, selected filters, counts |
| Search profiles | `%LocalAppData%\Grex\search_profiles.json` | Named paths, queries, and filters |
| Docker mirrors | `%LocalAppData%\Grex\docker-mirrors\` | Temporary copies of container files |
| Application log | `%Temp%\Grex.log` | Errors, diagnostics, and sometimes paths |
| Notification test log | `%LocalAppData%\Grex\notification_test.log` | Notification diagnostics |

Search result collections and AI conversations remain in process memory for the tab. Search results are not saved to a Grex database. Exports are written only when the user chooses a destination or clipboard action.

Uninstalling does not remove `%LocalAppData%\Grex`.

## Search and file access

Local search:

- enumerates files under the selected root;
- ignores inaccessible entries where possible;
- reads files with the current user's permissions;
- reads up to 64 KB for encoding detection;
- streams ordinary text files;
- can read whole ZIP entries or PDF files for document extraction;
- can follow reparse points when symbolic links are enabled.

Windows Search mode asks the local Windows Search service for candidate paths, then reads candidate files to confirm results.

UNC search uses Windows filesystem APIs and the current user's existing share/session credentials. Grex does not implement a credential prompt or credential store for shares.

## Replace risks

> Replace has no undo, rollback, journal, transaction, recycle-bin integration, or automatic backup.

Windows and UNC replacement:

- reads the full eligible file;
- applies text or .NET Regex replacement;
- overwrites the original path directly with `File.WriteAllText`;
- processes up to eight files concurrently;
- skips files over 100 MB;
- skips some read/write failures without restoring earlier changes.

WSL replacement:

- identifies files with `find`/`grep` or `git grep`;
- applies Match Files and Exclude Dirs before writing;
- passes the `sed` expression and target path as separate process arguments;
- applies `sed -i -E`;
- treats replacement text literally. Regex mode affects only the search pattern.

Cancellation stops future work but does not restore files already written. A process or power failure can leave a partial change set.

Docker replacement is disabled.

Binary/document extraction is not paired with a safe writer. Replace therefore skips known binary, archive, and document formats even when **Include binary files** is enabled.

Use source control, a snapshot, or a separate backup before replacement. Review the same search in Content/Files mode first.

## AI endpoint and API key

AI is optional and user-triggered.

### Storage

The API key is stored as plain text in:

```text
%LocalAppData%\Grex\settings.json
```

The Settings UI uses a `PasswordBox`, but that masks display only. Grex does not use Windows Credential Manager or encryption.

Settings export includes the key. Settings import accepts and stores it. Protect settings files and exports, avoid committing them, and remove keys before sharing diagnostics.

### Requests

Grex makes AI requests when:

- the user selects **Test Endpoint**, which sends a Models request;
- the user starts AI chat or sends a follow-up, which can perform model discovery and sends Chat Completions.

The chat request includes:

- search path;
- search query;
- Text/Regex mode;
- Content/Files mode;
- active filter suggestions;
- current tab conversation history, capped at 200 messages.

Grex does not automatically include:

- file contents;
- search result rows;
- replacement text;
- recent search/profile files;
- arbitrary directory listings.

User-entered chat text can still contain sensitive information. The configured provider receives and may retain request metadata according to its own policy.

### Transport

Grex prepends `https://` when a scheme is absent, but accepts an explicitly configured `http://` endpoint. Use HTTPS except for an isolated local service.

Bearer authentication is added only when a key is non-empty. Validate certificate and endpoint ownership outside Grex for high-sensitivity use.

### Endpoint trust

The endpoint response is parsed as JSON and displayed as text. Grex does not execute model output. However:

- endpoint errors can be written to logs;
- a malicious endpoint sees request metadata;
- a model can provide unsafe instructions;
- the user remains responsible for commands or edits performed after reading a response.

## Network and external-process behavior

| Feature | Connection or process | Trigger |
| --- | --- | --- |
| AI model test | HTTP GET to configured endpoint | Test Endpoint |
| AI chat | HTTP GET/POST to configured endpoint | AI discussion |
| Docker availability/list/mirror | `docker.exe`, local Docker daemon, `tar.exe` | Docker enabled/selected |
| Docker direct search | Docker API exec of `which`, `sh`, `find`, `grep`, `cat` | Container search |
| WSL search/replace | `wsl.exe` running shell tools | WSL path |
| UNC search | Windows network filesystem | UNC path |
| Windows Search | Local Windows Search service via OleDb | Toggle enabled |
| Open result | Explorer/default application/process | User action |

Grex does not implement an automatic update checker, analytics client, crash uploader, or background telemetry sender.

Operating-system components, the Windows App Runtime, Docker, WSL, default file handlers, and configured AI providers have their own privacy and update behavior.

## Docker access

Access to a Docker daemon is often equivalent to broad host control. Enable Docker search only for a daemon and containers you trust.

Direct Docker search:

- creates exec processes inside an existing running container;
- runs `sh -c` with argument-quoted `find -exec ... grep`;
- may read `<search-root>/.gitignore` with `cat`;
- does not start, stop, create, remove, or privilege-escalate containers.

Mirror fallback:

- copies selected container content to the host;
- normally creates a temporary tar archive under container `/tmp`;
- copies and extracts that archive;
- attempts to delete container and host temporary data;
- can leave temporary data after a crash, cancellation, permission failure, or cleanup error.

Mirrors can contain secrets from the container. Grex cleans the active mirror when the target changes, Docker is disabled, or the tab closes, but cleanup is best effort. A six-hour pruning helper exists in the service but the current app does not invoke it automatically. Inspect `%LocalAppData%\Grex\docker-mirrors` after interrupted or sensitive searches.

Docker commands run with the current daemon's existing authorization. Grex does not provide a separate allowlist or read-only Docker credential.

## WSL access

Grex constructs and runs WSL shell commands for search and replacement. Search commands read under the selected root; replacement modifies files with `sed -i`.

Risks:

- Linux shell and grep/Regex syntax differs from .NET;
- Regex patterns use the target's extended regular-expression syntax;
- an explicit `\\wsl$\<distro>` path selects that distribution;
- a plain Linux path uses the default distribution;
- current WSL user permissions apply;
- no container or filesystem snapshot is created.

Use a disposable test tree before relying on WSL Replace with complex Regex patterns.

## Logs and diagnostics

`%Temp%\Grex.log` is capped near 1 MB and trimmed to retain roughly the newest 512 KB. It can include:

- file paths;
- process or endpoint errors;
- Regex timeout file names;
- stack traces;
- platform diagnostic details.

It should not intentionally log the AI key, but treat the entire file as sensitive. Inspect and redact it before attaching to an issue.

Settings notification/localization tests can create extra diagnostic output. Run them only when needed.

## Telemetry and retention

The Grex application contains no analytics, usage telemetry, crash reporting, or update-check implementation.

Local history exists for usability and has explicit caps. Clear it from the UI or delete the relevant JSON files when working with sensitive paths or queries.

AI retention is controlled by the configured endpoint, not Grex. Docker/WSL/UNC activity can also appear in platform logs outside Grex.

## Release integrity

Official binaries are attached to this repository's GitHub Releases. Current Windows executables and installer are not code-signed.

Before running a release:

1. use the canonical `https://github.com/visorcraft/Grex/releases` page;
2. compare the asset's GitHub-displayed SHA-256 digest;
3. inspect release/tag provenance;
4. avoid third-party mirrors.

PowerShell:

```powershell
Get-FileHash .\grex-<version>-setup.exe -Algorithm SHA256
```

## Dependency security

Maintainers should:

- review direct and transitive NuGet changes;
- run `dotnet list package --vulnerable`;
- keep `Assets/third-party-licenses.json` current;
- regenerate `THIRD-PARTY-NOTICES.txt`;
- run the complete Windows test gate before tagging;
- use private advisories for coordinated fixes.

The release workflow builds and tests tagged source before packaging. Maintainers should still run the complete Windows test gate before tagging.

## Hardening recommendations

- Run non-elevated.
- Search the narrowest root needed.
- Leave system, hidden, binary, and symbolic-link inclusion off unless required.
- Back up before Replace.
- Prefer explicit WSL distribution paths.
- Treat Docker daemon access as privileged.
- Use HTTPS for AI.
- Use a low-scope provider key and rotate it if a settings file/export leaks.
- Clear histories after sensitive work.
- Review logs and mirrors before sharing or disposal.

## Security non-goals

Grex does not claim:

- sandboxed filesystem access;
- atomic or reversible replacement;
- protection against malicious files or archives beyond ordinary parsing limits;
- shell-command isolation for WSL/Docker adapters;
- encrypted credential storage;
- secure deletion;
- a complete `.gitignore` security boundary;
- signed release binaries;
- endpoint identity beyond normal HTTPS validation.
