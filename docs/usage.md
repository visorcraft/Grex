---
title: Usage Guide
layout: default
---

# Usage Guide

This guide covers installation, everyday searches, replacement, Docker, WSL, AI chat, saved state, and common failures. For exact schemas and option tables, use the [Technical Reference](reference.md).

## Install, update, or remove Grex

### Requirements

Published Grex releases require:

- Windows 10 version 1809, build 17763, or later
- An x64 processor
- Windows App Runtime 1.8

Install the Windows runtime:

```powershell
winget install --id Microsoft.WindowsAppRuntime.1.8 -e --source winget
```

The published archives include the .NET runtime. You need the .NET 8 SDK only when building from source.

### Installer

1. Open the [latest release](https://github.com/visorcraft/Grex/releases/latest).
2. Download `grex-<version>-setup.exe`.
3. Run the installer.
4. Optionally create a desktop shortcut.
5. Optionally add the bundled `grex-cli` directory to your per-user `PATH`.

The installer:

- installs per user under `%LocalAppData%\Programs\Grex`;
- does not request administrator privileges;
- installs the GUI and CLI;
- creates Start menu and uninstall entries;
- can add or remove the CLI directory from the current user's `PATH`.

The installer is currently unsigned. Windows SmartScreen may show a warning. Confirm that the file came from `github.com/visorcraft/Grex/releases` before running it. GitHub shows a SHA-256 digest for each release asset. You can compare it with:

```powershell
Get-FileHash .\grex-<version>-setup.exe -Algorithm SHA256
```

### Portable GUI or CLI

Download and extract one of these assets:

- `grex-<version>-win-x64.zip` for `Grex.exe`;
- `grex-cli-<version>-win-x64.zip` for `grex-cli.exe`.

Keep the extracted files together. Portable builds do not add shortcuts, change `PATH`, or update themselves.

### Update

- Installer: download the newer setup file and run it over the existing installation.
- Portable: extract the newer archive into a new folder, then remove the old folder after checking the new build.

User settings and history live outside the install directory, so upgrading or replacing a portable folder does not remove them.

### Uninstall and remove user data

Remove the installed app from **Settings > Apps > Installed apps > Grex**, or use its Start menu uninstall entry.

Uninstalling leaves preferences and history under `%LocalAppData%\Grex`. Delete that directory manually only if you also want to remove settings, recent paths, search history, profiles, and any leftover Docker mirrors. `%Temp%\Grex.log` is separate.

## Application layout

The navigation pane contains:

- **Search**: tabbed search, replace, AI chat, export, profiles, and history
- **Regex Builder**: sample text, pattern presets, live matches, and visual breakdown
- **Settings**: defaults, appearance, language, Docker, AI, preview, backup, and diagnostics
- **About**: version and project information
- **Credits**: bundled dependencies and license texts

Each Search tab owns its path, query, filters, mode, result list, and active operation. Closing a tab cancels its work and releases its retained results.

## Run a local, UNC, or WSL search

1. Enter or browse to a path.
2. Enter a search term.
3. Choose **Text Search** or **Regex Search**.
4. Choose **Content** or **Files** results.
5. Set filters.
6. Select **Search**, press Enter in the query box, or press F5.

Supported path examples:

```text
C:\Users\you\source
\\server\share\logs
\\wsl$\Ubuntu\home\you\source
\\wsl.localhost\Ubuntu-24.04\home\you\source
/home/you/source
/mnt/c/Users/you/source
```

A Linux path without a distribution name uses the default WSL distribution. Recent path suggestions store up to 20 entries.

Changing the Content/Files result mode does not convert an existing result collection. Run the search again after changing modes.

## Matching modes

### Text Search

Text Search supports:

- case-sensitive or case-insensitive matching;
- ordinal, current-culture, or invariant-culture comparison;
- no normalization, NFC, NFD, NFKC, or NFKD Unicode normalization;
- diacritic-sensitive or diacritic-insensitive matching;
- a selected culture for culture-aware comparison.

Case sensitivity applies to all targets. The advanced comparison settings apply to the Windows/local search engine. WSL and direct Docker searches use the matching behavior of their installed `grep`.

### Regex Search

Regex Search uses .NET regular expressions for local and mirrored searches. Dynamic patterns are not compiled and use a 10-second match timeout to limit catastrophic backtracking.

WSL and direct Docker searches use extended `grep` Regex syntax instead of .NET Regex syntax. A pattern can therefore behave differently across targets.

Regex replacement on local files uses .NET replacement syntax, including `$1` capture references. WSL replacement uses `sed -E`.

## Filters

Open **Filter Options** to configure the current tab.

| Filter | Behavior |
| --- | --- |
| Match Files | Case-insensitive filename wildcards. Separate alternatives with `|`; prefix an alternative with `-` to exclude it. |
| Exclude Dirs | Comma-separated directory names or a Regex. An anchored Regex such as `^(.git|vendor)$` is the least ambiguous form. |
| Search Type | Text or Regex. |
| Search Results | Content lines or files aggregated by path. |
| Size Limit | No limit, less than, equal to, or greater than a value in KB, MB, or GB. Tolerances are described in the [reference](reference.md#size-filter-tolerances). |
| Respect `.gitignore` | Applies ignore rules. Exact behavior differs by target; see the [target matrix](features.md#target-support-matrix). |
| Search case-sensitive | Enables case-sensitive text or Regex matching. |
| Include system files | Includes system attributes and normally excluded dependency/build/system directories. |
| Include subfolders | Recurses below the selected root. |
| Include hidden items | Includes Windows hidden attributes and dot-prefixed files/directories. |
| Include binary files | Enables best-effort searching of selected document/archive formats. It does not make every binary format searchable. |
| Include symbolic links | Follows reparse points or symbolic links where the target adapter supports them. |
| Use Windows Search | Seeds plain-text Windows searches from the Windows Search index, with fallback to normal enumeration. |

Examples:

```text
*.cs
*.cs|*.xaml|*.md
*.json|-*.generated.json
```

```text
.git,node_modules,vendor
^(.git|node_modules|vendor)$
```

Use `|`, not `;`, between Match Files alternatives. `Exclude Dirs` name lists use commas.

### Default exclusions

With **Include system files** off, Grex excludes Windows system attributes and paths containing:

```text
.git
vendor
node_modules
storage/framework
bin
obj
```

WSL and Docker adapters also exclude `sys`, `proc`, and `dev` beneath the search root.

## Work with results

### Content mode

Content mode returns one row per matching line:

- file name;
- 1-based line and column;
- sanitized line content;
- relative path;
- number of matches on that line.

### Files mode

Files mode aggregates matching lines by file and shows:

- file name;
- size;
- total match count;
- relative path;
- extension;
- detected encoding;
- last modified time.

The default sort is filename ascending in Content mode and match count descending in Files mode.

### Open and inspect

- Double-click a row to open the file with its default Windows application.
- Right-click a Content row for preview, Explorer, and copy actions.
- Right-click a Files row for file actions.
- In Docker mode, right-click copies the container path or filename instead of opening a host path.
- Right-click a column header to show or hide optional columns.
- Drag a column edge to resize it; double-click the edge to auto-fit.

### Context preview

Select a Content result and press Space, or use **Preview** from its context menu. The preview shows configurable lines before and after the match and can open the source file.

Settings accepts 1 to 20 lines before and after. Context loading reads at most 1 MB while seeking the requested area. Very distant lines in unusually large files may therefore be unavailable.

### Search within results

The result filter runs in memory and does not scan the filesystem again.

- Content mode checks filename, relative path, and line content.
- Files mode checks filename, relative path, extension, and encoding.
- The `.*` toggle enables a case-insensitive Regex filter.

An invalid result-filter Regex leaves all results visible until the pattern becomes valid.

### Export

The Export menu offers:

- CSV;
- pretty-printed JSON;
- tab-separated clipboard text.

Exports reflect the currently displayed mode and filtered result list.

## Replace text

> Replace is destructive. There is no undo, transaction, journal, or automatic backup.

Before replacing important data:

1. Commit or stash a Git worktree, or make a separate backup.
2. Run a normal search with the same path and filters.
3. Review the matching files.
4. Enable **Replace**.
5. Enter non-empty replacement text.
6. Select **Replace** and review the confirmation dialog.

Behavior by target:

- Windows and UNC files are read and then overwritten directly with `File.WriteAllText`.
- Files larger than 100 MB are skipped by local replacement.
- WSL replacement applies Match Files and Exclude Dirs before writing, then runs `sed -i -E`.
- WSL replacement text is literal. Regex mode applies extended Regex syntax only to the search pattern.
- Docker replacement is disabled.
- Cancelling stops future work but does not restore files already written.
- Local read/write failures are skipped. WSL command failures are reported.

Binary/document search uses extraction, but replacement does not reconstruct those formats. Replace skips known binary, archive, and document extensions even when **Include binary files** is on.

An empty or whitespace-only replacement is not accepted by the current UI.

## Tabs, history, and profiles

### Tabs

Use `+` to create another search. Tabs let searches run with independent paths and filters. At least one Search tab remains open.

### Recent searches

Grex keeps up to 20 searches in `%LocalAppData%\Grex\search_history.json`. Selecting one restores:

- path and query;
- Match Files and Exclude Dirs;
- search and result modes;
- case sensitivity;
- `.gitignore`;
- subfolder, hidden, and binary choices.

It does not restore every tab property. System, symbolic-link, Windows Search, size, culture, and Docker selections remain at their current values.

### Search profiles

Profiles are named snapshots and are stored in `%LocalAppData%\Grex\search_profiles.json`. A profile includes path, query, result/search modes, filters, size, and comparison settings. Grex keeps up to 50 profiles.

Profiles do not store replacement text, active results, or a selected Docker container.

## Search Docker containers

Requirements:

- Docker Desktop or another reachable Docker Engine;
- the `docker` CLI on `PATH`;
- access to the local Docker daemon;
- a running Linux container.

Workflow:

1. Enable **Docker Search** in Settings.
2. Return to Search.
3. Choose a running container from **Search Target**.
4. Enter a path inside the container, such as `/app/src`.
5. Run the search.

Grex first checks for `grep` and tries a direct search through the Docker API. Direct search executes `find -exec ... grep` inside the container. If that path fails or `grep` is unavailable, Grex creates a local mirror under `%LocalAppData%\Grex\docker-mirrors` and uses the local search engine.

Mirror fallback requires `tar` inside the container and Windows `tar.exe`. When symbolic links are excluded, Grex creates a temporary archive under `/tmp` in the container, copies and extracts it, then removes it on a best-effort basis. When symbolic links are included, it uses `docker cp`, which can fail under Windows symlink restrictions.

Docker notes:

- Browse, Windows Search, and Replace are disabled for a container target.
- Direct search ignores the size filter. Mirror fallback applies it.
- Direct `.gitignore` support reads only the `.gitignore` at the selected root and does not implement negation. Mirror fallback uses the local nested parser.
- Direct binary inclusion uses `grep -a`; it does not extract Office, PDF, RTF, or ZIP content.
- The active mirror is removed when the target changes, Docker is disabled, or the tab closes. Crash leftovers are not automatically pruned by the current app.
- Direct search batches matching paths through `find -exec`.

## Search WSL

Grex invokes `wsl.exe` and Linux command-line tools. Ensure these work first:

```powershell
wsl --status
wsl -e sh -lc "command -v grep"
```

Use `\\wsl$` or `\\wsl.localhost` to choose a distribution explicitly. A plain Linux path uses the default distribution.

WSL notes:

- Search uses Linux `find`, `grep`, and optional `git grep`.
- With Respect `.gitignore` enabled at a Git root, `git grep` searches Git-visible files. Outside a Git root, Grex falls back to `find` and `grep`.
- Culture, Unicode normalization, and diacritic options are not translated to `grep`.
- Context preview and open-file actions convert paths back to Windows when possible.
- Replace uses `sed -i -E`, treats replacement text literally, and modifies eligible text files in the WSL filesystem.

## Windows Search acceleration

**Use Windows Search** is available for non-Regex Windows paths. Grex asks the Windows Search index for candidate files, then applies filters and confirms content through its own search pipeline.

If the index is unavailable, the query fails, or the path is not indexed, Grex falls back to normal file enumeration. It is not used for WSL, Docker, or Regex searches.

## Use AI chat

AI chat is optional and does not search file contents by itself.

1. Open **Settings > AI Search**.
2. Enter an OpenAI-compatible endpoint.
3. Enter an API key if the endpoint requires one.
4. Enter a model id, or leave it blank for discovery.
5. Select **Test Endpoint**.
6. Return to Search, enter a path and query, then select **AI**.
7. Continue in the chat input.

Endpoint handling:

- Grex adds `https://` when no scheme is supplied.
- A base ending in `/v1` receives `/chat/completions` and `/models`.
- Another base receives `/v1/chat/completions` and `/v1/models`.
- A configured model id is used exactly.
- A blank model triggers `GET /v1/models`; Grex uses the first returned id.
- Failed discovery falls back to `gpt-4o-mini`.
- Requests time out after 90 seconds.

Each discussion sends:

- search path and query;
- Text/Regex mode;
- Content/Files mode;
- active filter suggestions;
- up to 200 user and assistant messages from the current tab.

Grex does not automatically attach file contents or result rows. The endpoint may retain whatever is sent according to its own policy.

The API key is plain text in `%LocalAppData%\Grex\settings.json` and settings exports. Use HTTPS and protect both files. See [Security and Privacy](../SECURITY.md#ai-endpoint-and-api-key).

## Regex Builder

The Regex Builder provides:

- sample text;
- a pattern input;
- Email, Phone, Date, Digits, and URL presets;
- live matches;
- a visual token breakdown;
- case-insensitive, multiline, and global options.

Selecting a preset asks before replacing a non-empty pattern. The builder is a sandbox only; copy the final pattern into a Search tab to search files.

## Settings, backup, and reset

Settings become defaults for new tabs. Existing tabs keep most of their current state.

Settings include:

- UI language and 12 theme choices;
- default search/result modes and filters;
- string comparison, culture, normalization, and diacritic behavior;
- Docker enablement;
- AI endpoint, key, and model;
- context lines before and after;
- column visibility and window geometry;
- export, import, restore, restart, notification, and localization diagnostics.

**Export Settings** writes the complete settings object, including the AI API key and window geometry, to JSON.

**Import Settings** merges every field present except machine-specific window geometry. Missing fields keep their current values.

**Restore Defaults** deletes `%LocalAppData%\Grex\settings.json` and restarts the application. History and profiles remain.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| App does not start | Install Windows App Runtime 1.8 x64. Confirm Windows build 17763 or later. |
| SmartScreen warning | The installer is unsigned. Verify the source and GitHub SHA-256 digest. |
| Build fails for Any CPU | Use `-p:Platform=x64`, `x86`, or `ARM64` on Windows. |
| Build fails on Linux/macOS | WinUI XAML, Windows App SDK, MSIX, and PRI tooling are Windows-only. Only `Scripts/` is cross-platform. |
| Search finds nothing | Check result/search mode, case, `.gitignore`, Match Files `|` syntax, hidden/system/binary flags, and path permissions. |
| Replace button is disabled | Enter path, query, and non-whitespace replacement text; stop other work; select a non-Docker target. |
| Docker target is unavailable | Start Docker, run `docker version`, confirm daemon access, then refresh the list. |
| Docker result count ignores size | Direct Docker search does not apply size. Force mirror fallback only by using a container without `grep`, or search a host-mounted path locally. |
| WSL fails | Run `wsl --status`; verify the distribution and `grep`; use an explicit `\\wsl$\<distro>` path. |
| AI test returns 404 | Use the provider's base URL or `/v1` URL. Grex appends `/models` and `/chat/completions`. |
| AI model fails | Set the exact provider model id instead of relying on first-model discovery. |
| Toast notification missing | Avoid elevated launch; run the Settings notification test and inspect its diagnostics. |
| UI text remains English | Non-English catalogs are incomplete; missing entries fall back to English/key text. |

Application diagnostics are in `%Temp%\Grex.log`. The file is capped near 1 MB and trimmed to roughly half that size when the cap is exceeded.

When opening an issue, include the Grex version, Windows build, target type, exact path form, query mode, filters, reproduction steps, and only the relevant redacted log lines.
