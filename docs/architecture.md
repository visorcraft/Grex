---
title: Architecture
layout: default
---

# Architecture

Grex is an unpackaged WinUI 3 desktop application plus a Windows console application. Both target .NET 8 and reuse the same search engine.

## Design shape

The application is MVVM-shaped, with a deliberate WinUI code-behind layer:

- Models hold result, profile, history, Docker, and preview data.
- Services own filesystem, process, network, settings, localization, and export behavior.
- ViewModels own tab state, operations, sorting, filtering, and result collections.
- XAML defines controls and bindings.
- Code-behind handles WinUI lifecycle, dynamic menus, dialogs, column sizing, keyboard input, file pickers, AI chat presentation, and other UI-specific orchestration.

`SearchTabContent.xaml.cs` is therefore a UI coordinator, not a passive view. New engine behavior belongs in Services; new tab state belongs in `TabViewModel`; WinUI-only interaction stays in the view.

## Runtime stack

| Layer | Technology |
| --- | --- |
| Runtime | .NET 8, C# |
| GUI | WinUI 3, Windows App SDK 1.8 |
| Pattern engine | `System.Text.RegularExpressions` |
| Local files | `System.IO`, async streams, parallel enumeration |
| Windows Search | `System.Data.OleDb` query adapter |
| WSL | `wsl.exe`, `find`, `grep`, `git grep`, `sed`, `stat` |
| Docker direct | Docker.DotNet exec API |
| Docker fallback | Docker CLI, container/host `tar`, local search |
| Serialization | `System.Text.Json` |
| AI | `HttpClient`, OpenAI-compatible JSON |
| CLI parsing | System.CommandLine |
| Tests | xUnit, Moq, FluentAssertions |

Direct GUI dependencies are recorded in `Grex.csproj`. The CLI adds System.CommandLine and references the GUI project to reuse services and models.

## Repository map

```text
Grex/
├── App.xaml(.cs)                    Application startup
├── MainWindow.xaml(.cs)             Shell, navigation, tabs, themes, shortcuts
├── Controls/
│   ├── SearchTabContent.xaml(.cs)   Search UI and WinUI coordination
│   ├── RegexBuilderView.xaml(.cs)   Interactive Regex sandbox
│   ├── SettingsView.xaml(.cs)       Settings, import/export, diagnostics
│   ├── ContextPreviewDialog.*       Match context presentation
│   ├── AboutView.*                  Version and project information
│   └── CreditsView.*                Dependency license display
├── ViewModels/
│   ├── MainViewModel.cs             Tab creation and disposal
│   └── TabViewModel.cs              Per-tab state and operations
├── Models/                           Result and persistence DTOs
├── Services/                         Search and platform integrations
├── Converters/                       XAML value converters
├── Grex.Cli/                         Console entry point, options, formatters
├── Strings/<culture>/Resources.resw Localized resource catalogs
├── Assets/                           Icons and license manifest
├── Tests/                            Unit tests
├── Tests/Grex.Cli.Tests/             CLI unit tests
├── IntegrationTests/                 Filesystem and app integration tests
├── UITests/                          ViewModel-driven UI tests
├── Scripts/                          Python maintenance tools
├── docs/                             Public documentation
├── build.ps1                         Windows build/package entry point
├── Grex.iss                          Inno Setup installer definition
└── .github/workflows/
    ├── ci.yml                        Push/PR build and test gate
    └── release.yml                   Tag-triggered release packaging
```

## Core components

### MainWindow

`MainWindow` owns:

- navigation among Search, Regex Builder, Settings, About, and Credits;
- the tab host;
- shared status UI;
- global F1, F5, and Escape behavior;
- theme resources and Mica setup;
- localization refresh coordination;
- window geometry persistence;
- cancellation and event unsubscription on close.

### MainViewModel

`MainViewModel` creates, selects, closes, and disposes `TabViewModel` instances. It keeps at least one tab available.

### TabViewModel

Each tab contains:

- target and path;
- query and replacement;
- search/result modes;
- all filters and comparison settings;
- Docker container state;
- complete and displayed result collections;
- sort field/direction;
- result-filter state;
- status text and stopwatch;
- operation cancellation;
- active Docker mirror;
- disposal logic.

The ViewModel calls `ISearchService` for local/WSL search and replace, and `DockerSearchService` for Docker preparation/direct search.

### SearchService

`SearchService` is shared by GUI and CLI. It dispatches between:

- `SearchWindowsPathAsync`;
- `SearchWslPathAsync`;
- `ReplaceWindowsPathAsync`;
- `ReplaceWslPathAsync`.

The local path implementation performs:

1. input and Regex validation;
2. candidate enumeration or Windows Search seeding;
3. hidden, system, `.gitignore`, filename, directory, binary, link, and size filtering;
4. up to eight concurrent file scans;
5. bounded encoding detection;
6. line matching or document extraction;
7. sanitized `SearchResult` creation.

Regex objects are created once per operation without `RegexOptions.Compiled` and with a 10-second timeout.

### GitIgnoreService

`GitIgnoreService`:

- discovers nested `.gitignore` files from root to candidate;
- translates common Git ignore syntax into bounded Regex;
- handles comments, negation, root-relative and directory patterns;
- caches up to 100 parsed entries;
- protects its shared cache.

It is a custom implementation, not a call to Git itself. WSL and direct Docker use different adapters.

### EncodingDetectionService

`EncodingDetectionService`:

- reads no more than 64 KB;
- checks byte-order marks;
- scores available encodings;
- applies byte-pattern heuristics;
- falls back to UTF-8;
- returns the encoding, confidence, BOM state, and reason.

### ContextPreviewService

The preview service detects encoding, seeks toward the requested line, and returns numbered lines plus the match-line index. Reads while seeking are capped at 1 MB.

### WindowsSearchIntegration

The Windows Search adapter queries the OS index for file candidates under a scope. `SearchService` still applies Grex filters and reads candidates to confirm content. A failed index path falls back to enumeration.

### DockerSearchService

Docker direct flow:

1. Verify `docker version` through the CLI for UI availability.
2. List running containers with `docker ps`.
3. Create a Docker.DotNet client for the local daemon.
4. Exec `which grep`, cached per container.
5. Exec `sh -c` with `find -exec ... grep`; arguments are shell-quoted and command failures remain visible.
6. Parse `path:line:content`.
7. Apply filename and simplified root `.gitignore` post-filters.

Mirror flow:

1. Create a unique host directory.
2. With symbolic links included, call `docker cp`.
3. Otherwise create a dereferenced tar under container `/tmp`.
4. Copy the tar to the host, extract with `tar.exe`, and remove temporary archives best-effort.
5. Search the mirror with `SearchService`.
6. Translate local result paths back to container paths.
7. remove the mirror best-effort.

The service exposes a six-hour stale-pruning helper, but the current app does not call it automatically. Active mirrors are cleaned when targets change, Docker is disabled, or tabs close. Docker replacement is blocked at the ViewModel/UI boundary.

### AiSearchService

`AiSearchService`:

- normalizes endpoint URLs;
- optionally discovers a first model from Models;
- builds Chat Completions messages;
- attaches a bearer key when configured;
- uses one shared `HttpClient` with a 90-second timeout;
- accepts several common response content shapes;
- returns a success/message/error DTO.

The service receives path/query/filter metadata and conversation text from the view. It does not read files or results.

`SearchTabContent` caps both visible AI messages and payload history at 200 per tab.

### Persistence services

| Service | File | Cap |
| --- | --- | --- |
| SettingsService | `settings.json` | One settings object |
| RecentPathsService | `search_path_history.json` | 20 |
| RecentSearchesService | `search_history.json` | 20 |
| SearchProfilesService | `search_profiles.json` | 50 |

These services use `System.Text.Json`, process-local locks, and fail-soft reads/writes. Corrupt or inaccessible files generally produce defaults or empty lists.

### ExportService

Export builds CSV, JSON, or tab-separated text from the current mode. The view handles file picker and clipboard integration.

### LocalizationService

Localization uses Windows App SDK resource management and `Strings/<culture>/Resources.resw`.

The service:

- tracks current UI culture;
- provides formatted lookup;
- falls back when lookup fails;
- raises change notifications;
- caches up to 20 resource contexts.

`LocalizedToolTipRegistry` retains weak/lifecycle-managed registrations and refreshes tooltips when culture changes.

### LogService

All application diagnostics route to `%Temp%\Grex.log`. Writes are lock-protected. Once the file exceeds roughly 1 MB, the logger keeps the most recent approximately 512 KB at a line boundary.

## Search flows

### Local or UNC

```text
SearchTabContent
  -> TabViewModel.PerformSearchAsync
  -> SearchService.SearchAsync
  -> Windows candidate enumeration/index
  -> filters
  -> parallel scans and extraction
  -> SearchResult list
  -> content rows or file aggregation
  -> result filter and observable collection
```

The ViewModel keeps an unfiltered backing list so Search Within Results can reset without another filesystem scan.

### WSL

```text
SearchService detects WSL path
  -> extract distribution and Linux path
  -> build wsl.exe + bash command
  -> find/grep or git grep
  -> parse output
  -> post-filter filename/directories
  -> normalize result paths
```

Process cancellation kills the WSL process tree when possible.

### Docker

```text
TabViewModel detects selected container
  -> DockerSearchService direct grep
  -> if successful: use parsed results
  -> if unavailable/failure: create mirror
  -> SearchService searches local mirror
  -> ViewModel maps mirror paths to container paths
```

Direct and mirror modes do not have identical filter semantics. The [Feature Matrix](features.md#target-support-matrix) is authoritative for users.

## Replace flows

### Windows and UNC

The local replace pipeline enumerates and filters candidates, then processes up to eight files concurrently:

1. skip files over 100 MB;
2. detect encoding;
3. read the full file;
4. count and replace matches;
5. overwrite with `File.WriteAllText`;
6. report a `FileSearchResult`.

There is no transaction or backup.

### WSL

WSL replacement finds matching paths, applies filename/directory filters before writes, counts matches with argument-separated `grep`, applies an escaped `sed -i -E` expression, checks command exit codes, reads metadata with `stat`, and returns file summaries. Replacement text is literal; Regex mode affects the search pattern.

Cancellation stops remaining work but does not roll back completed writes.

## CLI reuse

`Grex.Cli` maps parsed options to `ISearchService.SearchAsync`, then selects:

- quiet exit only;
- total count;
- unique full paths;
- text, JSON, or CSV formatter.

The CLI returns `0`, `1`, or `2` and does not launch WinUI. Its project reference still carries the Windows-targeted Grex assembly and dependencies into build/publish, so the CLI is Windows-only rather than a portable cross-platform console package.

## Threading and cancellation

- Filesystem work uses async APIs where available and bounded `Parallel.ForEachAsync`/`Parallel.ForEach`.
- UI collections are updated by the ViewModel after background work.
- Each tab owns and disposes its search cancellation source.
- The view owns and disposes per-request AI cancellation sources.
- Process adapters register cancellation to kill child process trees.
- Closing tabs/windows cancels work and unsubscribes long-lived events.
- Shared mutable caches use locks or concurrent collections and explicit caps.

## Failure strategy

Grex distinguishes between operation-level and file-level failures:

- invalid path or Regex can fail the operation;
- inaccessible or unreadable individual files are skipped;
- Windows Search failure falls back;
- Docker direct failure falls back to mirror;
- settings/history read failure returns defaults;
- logging failure is ignored;
- cancellation returns the UI to Ready without rollback.

This fail-soft approach keeps search useful on mixed-permission trees, but it means result counts can omit files that could not be processed.

## Tests

The solution contains:

- `Tests/Grex.Tests.csproj`: service, model, ViewModel, control-helper, and license coverage tests;
- `Tests/Grex.Cli.Tests/Grex.Cli.Tests.csproj`: runner and formatter tests;
- `IntegrationTests/Grex.IntegrationTests.csproj`: filesystem and app integration;
- `UITests/Grex.UITests.csproj`: ViewModel-driven UI behavior.

All .NET projects target Windows. Some UI-context integration facts are explicitly skipped.

Python maintenance scripts have `unittest` checks under `Scripts/test_*.py`.

## Packaging and licenses

`build.ps1` restores, builds, tests, publishes self-contained win-x64 GUI/CLI directories, creates ZIP files, and uses Inno Setup when available.

`Grex.iss` installs per user and can add the CLI directory to the current user's `PATH`.

GUI dependency metadata lives in `Assets/third-party-licenses.json`. `Scripts/generate_third_party_notices.py` is the only writer for `THIRD-PARTY-NOTICES.txt`, and `CreditsLicenseCoverageTests` checks resolved GUI packages against the manifest or explicit build-only exclusions.
