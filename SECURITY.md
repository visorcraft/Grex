<!-- SPDX-FileCopyrightText: 2026 VisorCraft LLC -->
<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Security and Privacy Policy

Grex is a developer tool that reads (and optionally writes) arbitrary
local files, can attach to Docker containers and WSL distributions,
and can optionally send a small context payload to an AI endpoint.
This document records the threat model, the data Grex touches, and the
policies it enforces.

## Reporting a vulnerability

**Do not file a public GitHub issue or pull request for security
problems.** Instead:

- Email the security contact at **security@visorcraft.com**.
- Include reproduction steps, the Grex version (`Help → About`), your
  Windows build, and the relevant configuration if possible.
- We aim to acknowledge within 72 hours and to publish a patch +
  advisory within two weeks of confirming a fix.

Please give us a 30-day coordinated-disclosure window before going
public, unless the vulnerability is already actively exploited in the
wild.

## Supported versions

The latest stable release of Grex is supported with security fixes.
Older versions receive fixes only when the patch is trivial to
backport. The "About" dialog and the GitHub Releases page are the
canonical version sources.

## Telemetry policy

**Grex ships zero telemetry.** No analytics, no crash reports, no
ping-home behavior. Opt-in diagnostics surface as local log files
under `%LOCALAPPDATA%\Grex\logs\`, never as outbound traffic.

A future "diagnostics" feature, if added, must:

- be off by default,
- redact every path / search term before submission,
- surface a one-time consent dialog,
- target a documented, versioned endpoint.

## Outbound traffic

Grex makes network requests in exactly two situations:

1. **AI Search Chat** — only when the AI feature is enabled AND the
   user has supplied an endpoint + API key AND they explicitly invoke
   the AI panel. The allowed endpoint set is OpenAI-compatible
   servers configured by the user.
2. **Endpoint test** (`Settings → AI → Test endpoint`) — a single
   request against the user-supplied endpoint to verify connectivity.

No other Grex subsystem opens a socket. Update checks, telemetry, and
crash uploads do not exist.

## Local file access

- **Search** reads files the user explicitly points at. Default
  exclusions include `bin`, `obj`, `node_modules`, `.git`, `vendor`,
  and Windows system roots when not explicitly opted in.
- **Replace** writes via atomic temp-file-then-rename on the same
  volume. NTFS permissions are preserved. A journal at
  `%LOCALAPPDATA%\Grex\replace-journal.json` records each file the
  replace pipeline modifies; the journal is rotated on clean exit.
- **UNC shares** are accessed using the credentials of the running
  user — Grex does not prompt for, store, or transmit network
  credentials.
- **WSL search** invokes `wsl.exe` against the user-selected
  distribution and runs `grep`-equivalents inside it. Grex does not
  elevate to root inside the distribution or modify its rootfs during
  search.
- **Container search** uses the Docker API (`Docker.DotNet`) to
  `exec` `grep` inside the container. Grex never runs privileged
  container operations and never writes to a container during a
  search.

## Replace risks

- **No undo.** Replace writes are committed atomically. The journal
  records *which* files were modified but not their previous content.
  Users who need rollback should snapshot the tree first (Git stash,
  Volume Shadow Copy, `xcopy` snapshot, etc.).
- **Archived documents are never modified.** OOXML / ODF / ZIP / PDF /
  RTF files are extracted read-only for search and skipped by the
  replace pipeline.
- **Containers and WSL are read-only.** The container and WSL adapters
  have no replace entry point; the search engine refuses to write to
  them.
- **System directories require an explicit opt-in.** Replace targeting
  `C:\Windows`, `C:\ProgramData`, `Program Files`, or `Program Files
  (x86)` is gated behind a confirmation dialog and a settings flag.

## Container and WSL access

Mounting a Docker socket or invoking `wsl.exe` grants substantial
privileges to the running process. Grex relies on the user's existing
group membership / WSL configuration and never installs helpers, never
elevates on its own, and never writes to a container or distribution
during a search.

The Settings UI must surface "this is privileged access" explicitly
when the user enables container search or WSL search for the first
time.

## API key handling

API keys for the AI endpoint are stored in the **Windows Credential
Manager** through the `PasswordVault` API.

- Resource: `com.visorcraft.Grex.ai`
- UserName: canonical endpoint base URL
- Multiple endpoints can each have their own key without overwriting.

**No plaintext fallback.** If Credential Manager is unavailable (for
example in a restricted enterprise lockdown profile), the AI panel
surfaces the error verbatim and refuses to store the key.

API keys are excluded from:

- Settings exports (`Settings → Export…`)
- `ILogger` output (the AI client never logs the value)
- Screenshots / diagnostics packages
- The replace journal

## Path redaction in diagnostics

The CLI logs to `%LOCALAPPDATA%\Grex\logs\grex.log` by default. The
default fields are search root, query, regex flag, case sensitivity,
and gitignore flag — none of which would be considered a secret by
themselves.

A future privacy-mode toggle will redact:

- Path prefixes outside `%USERPROFILE%`
- The search term
- The replacement string
- The detected encoding label for content under
  `C:\Windows\`, `C:\ProgramData\`, and `C:\Program Files*\`

## Threat model summary

| Threat | Mitigation |
| ------ | ---------- |
| User searches a malicious archive on their disk | Grex reads bytes, never executes. Document extraction goes through hardened parsers (`System.IO.Packaging` for OOXML, `iText` for PDF) running in-process without shell expansion. |
| User replaces something they didn't intend | Confirmation dialog before replace; journal records the change set; atomic rename means files are either fully replaced or untouched. |
| AI endpoint is malicious (crafted JSON response) | The client deserializes with `System.Text.Json` (no script eval), surfaces errors through typed result models, and never executes content. |
| Container `docker exec` injection | Every argv passed to the Docker API is built as a `List<string>`, not concatenated; integration tests pin this. |
| WSL command injection | The WSL adapter builds `wsl.exe` argv as a list and does not pass user input through `cmd /c`. |
| Stale temp file leaks data | Replace writes use `TempFile` patterns scoped to the target volume; the journal is the source of truth and is removed on clean exit. |
| Credential Manager unavailable, user tries to enable AI | The vault call surfaces the error; the UI refuses to fall back to plaintext storage. |
| Logs accidentally capture an API key | The AI client and credential layer never log the key value. |
| Search root is the whole drive | System-dir auto-exclusions kick in unless the user opts in to system search; tests in `Tests/Grex.Tests/RootSafetyTests.cs` pin the behavior. |

## Dependency hygiene

- NuGet license metadata is reviewed against the GPL-3.0 compatibility
  matrix during dependency upgrades.
- `dotnet list package --vulnerable` is the pre-release security check.
- Dependabot opens weekly PRs for transitive and direct updates.
- Vulnerabilities discovered after release are tracked through GitHub
  Security Advisories on this repository.
