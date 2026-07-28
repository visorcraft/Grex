---
title: Feature Matrix
layout: default
---

# Feature Matrix

This page states what Grex supports and where behavior changes by target. The [Usage Guide](usage.md) explains workflows; the [Technical Reference](reference.md) defines exact formats and limits.

## Target support matrix

| Capability | Windows / UNC | WSL | Docker direct | Docker mirror | CLI |
| --- | --- | --- | --- | --- | --- |
| Plain-text search | .NET string comparison | `grep` | `grep -F` | Local .NET engine | Local engine or WSL adapter |
| Regex search | .NET Regex | `grep -E` | `grep -E` | .NET Regex | Local engine or WSL adapter |
| Case sensitivity | Yes | `grep -i` when off | `grep -i` when off | Yes | Yes |
| Culture / Unicode / diacritics | Yes for text search | No | No | Yes | Defaults only; no CLI flags |
| Recursive search | Yes | Yes | Yes | Yes | Yes |
| Match Files | `|` wildcards and exclusions | Post-filtered | Post-filtered | Yes | Yes |
| Exclude Dirs | Names or Regex | Find plus post-filter | Find pruning | Yes | Yes |
| `.gitignore` | Nested custom parser | `git grep` at Git root | Selected-root file, simplified | Nested custom parser | Same as target adapter |
| Hidden / system / binary / links | Yes | Adapter-specific | Adapter-specific | Yes | Yes |
| Size filter | Yes | Yes | No | Yes | Yes |
| Windows Search index | Plain text only | No | No | No | No flag |
| Document/archive extraction | Yes | No | No | Yes | Yes for local targets |
| Context preview | Yes | Yes when path conversion works | No host preview | Mirror path during active search | No |
| Replace | Direct file write | `sed -i` | No | No | No |
| Export | CSV, JSON, clipboard | Same GUI output | Same GUI output | Same GUI output | Text, JSON, CSV |

Docker mode is presented as one target in the UI. The direct and mirror columns explain its two internal strategies.

## Search

### Text matching

Local Text Search supports:

- ordinal, current-culture, and invariant-culture comparison;
- case-sensitive and case-insensitive forms;
- NFC, NFD, NFKC, and NFKD normalization;
- optional diacritic removal;
- a selected comparison culture.

WSL and direct Docker targets delegate matching to the target's `grep`, so only the options mapped to `grep` apply.

### Regex matching

Local and mirrored Regex Search uses .NET Regex with a 10-second timeout. Grex intentionally avoids `RegexOptions.Compiled` for user patterns to prevent unbounded process-level cache growth.

WSL and direct Docker use extended grep expressions. .NET-only constructs may fail or match differently there.

### Parallelism and streaming

- Local search processes up to eight files concurrently.
- Normal text files are streamed line by line.
- Encoding detection reads at most the first 64 KB.
- Direct Docker search batches files with `find -exec`.
- Results are accumulated per tab and published to observable collections in batches.

## Paths and environments

### Windows and UNC

Grex accepts drive paths and UNC shares using the current Windows user's permissions. Inaccessible files are skipped.

### WSL

Supported forms include:

```text
\\wsl$\Ubuntu\home\user
\\wsl.localhost\Ubuntu-24.04\home\user
/home/user
/mnt/c/Users/user
```

Grex extracts an explicit distribution from UNC forms. Plain Linux paths use the default WSL distribution.

### Docker

Docker search is opt-in. It requires the Docker CLI, a reachable daemon, and a running Linux container.

Direct mode:

- checks for `grep` through the Docker API;
- executes `find` and `grep` inside the container;
- caches grep availability for up to 50 containers;
- reads only the selected root's `.gitignore`;
- does not apply size limits or document extraction.

Mirror fallback:

- copies the selected container path to `%LocalAppData%\Grex\docker-mirrors`;
- uses a temporary tar archive in the container when dereferencing links;
- searches the copy with the local engine;
- removes the mirror after use when possible;
- cleans the active mirror when the target changes, Docker is disabled, or the tab closes; crash leftovers require manual deletion.

Browse, Windows Search, and Replace are disabled for a selected container.

## Filters

Grex can combine:

- filename includes and excludes;
- directory name or Regex exclusions;
- nested `.gitignore` rules on local paths;
- recursion;
- Windows hidden and system attributes;
- Unix dotfile filtering;
- known binary-extension filtering;
- symbolic-link/reparse-point filtering;
- file-size comparisons;
- Windows Search candidate seeding.

With **Include system files** off, common dependency, build, VCS, and Linux system directories are excluded. See [System path exclusions](reference.md#system-path-exclusions).

### Size tolerance

Size comparisons deliberately include a tolerance:

- KB: 10 KB
- MB: 1 MB
- GB: 25 MB

This affects less-than, equal-to, and greater-than comparisons. Direct Docker search is the exception and ignores size.

## Binary and document search

With **Include binary files** on, the local engine attempts:

- `.docx`, `.xlsx`, `.pptx`;
- `.odt`, `.ods`, `.odp`;
- `.zip`;
- `.pdf` up to 50 MB;
- `.rtf`.

Behavior is best effort:

- ZIP-based formats search UTF-8 `.xml`, `.txt`, and `.rels` entries.
- PDF extraction scans raw streams and simple text objects. It is not a complete PDF parser and performs no OCR.
- RTF extraction strips common control sequences.
- Encrypted, scanned, compressed, or unusual documents can produce no result.
- Legacy Office, images, media, executables, and most archives remain excluded.

Binary extraction is for search only. Replace skips known binary, archive, and document formats.

## Results and inspection

### Content results

Content mode shows one row per matching line with filename, line, column, sanitized text, relative path, and per-line match count.

### File results

Files mode groups matches by path and shows size, total matches, extension, encoding, relative path, and modification time.

### Result tools

- sortable and resizable columns;
- persisted column visibility;
- in-memory plain-text or Regex filtering;
- context preview with 1 to 20 surrounding lines;
- double-click open;
- Explorer and clipboard context actions;
- Docker-aware copy actions;
- CSV, JSON, and clipboard export.

## Replace

Replace is available for Windows, UNC, and WSL targets.

Safeguards:

- explicit Replace mode;
- non-empty replacement requirement;
- confirmation dialog;
- active-operation cancellation;
- Docker target lockout;
- local 100 MB per-file cap;
- Files result summary after writes.

Boundaries:

- no undo, backup, transaction, or journal;
- Windows/UNC writes are direct;
- WSL applies file/directory filters before `sed -i -E` and treats replacement text literally;
- cancellation does not roll back completed files;
- failures can yield a partial change set;
- known binary, archive, and document formats are skipped.

Use version control or a backup before replacing.

## Search organization

### Tabs

Each tab retains its own path, query, filter values, results, sorting, and active work.

### Recent paths and searches

- Recent paths: 20
- Recent searches: 20
- Duplicate entries move to the top
- Individual or full-history removal is available

Recent search entries store a practical subset of filters, not every tab property.

### Profiles

Up to 50 named profiles store a fuller path/query/filter snapshot. Profiles support apply, overwrite, and delete. They do not store results, replacement text, or Docker container selection.

## Regex Builder

The Regex Builder offers:

- Email, Phone, Date, Digits, and URL presets;
- sample text and live matches;
- case-insensitive, multiline, and global switches;
- a visual breakdown of literals, groups, classes, anchors, escapes, and quantifiers;
- overwrite confirmation for an existing pattern.

It does not automatically transfer a pattern to Search.

## AI chat

AI chat supports OpenAI-compatible `GET /v1/models` and `POST /v1/chat/completions` style endpoints.

It provides conversational search guidance from:

- current path and query;
- Text/Regex and Content/Files modes;
- filter suggestions;
- current tab conversation history, capped at 200 messages.

It does not read or send file contents or result rows automatically, execute suggested searches, or modify files.

Model selection can be explicit, first-model discovery, or `gpt-4o-mini` fallback. The HTTP timeout is 90 seconds.

The API key is stored in plain-text settings and included in settings exports. See [Security and Privacy](../SECURITY.md#ai-endpoint-and-api-key).

## Settings and personalization

Grex persists:

- default search and result modes;
- default Match Files and Exclude Dirs;
- filter defaults;
- string-comparison settings;
- UI language;
- 12 theme choices;
- Docker enablement;
- AI endpoint, key, and model;
- context-preview line counts;
- column visibility;
- window size and position.

Settings export, import, restore, restart, notification test, and localization test actions are available. Import keeps window geometry local and merges every other field present; see [Settings backup and import](reference.md#settings-backup-and-import).

## Localization

The repository ships an English catalog plus 107 non-English resource catalogs. Non-English coverage is incomplete and status varies by locale. Missing entries fall back to English or the resource key.

The UI can refresh language-dependent labels and registered tooltips without a full reinstall. Some changes may require restart.

See the [Translation Guide](translations.md) for the authoritative status script and contribution workflow.

## CLI

`grex-cli` provides:

- positional `<path> <term>` input;
- text or Regex search;
- common file filters;
- text, JSON, and CSV output;
- count, files-only, and quiet modes;
- exit codes 0, 1, and 2.

The CLI does not expose Docker search, replacement, Windows Search, or the GUI's advanced comparison settings. See the [CLI reference](reference.md#cli-reference).

## Deliberate non-features

Grex currently has:

- no automatic updater;
- no telemetry, analytics, or crash uploader implemented by the application;
- no replace undo;
- no Docker replace;
- no OCR;
- no cross-platform GUI or CLI build;
- no background filesystem index of its own;
- no automatic AI file upload.
