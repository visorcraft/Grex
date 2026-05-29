---
title: Feature Deep Dive
layout: default
---

# Feature Deep Dive

This document expands on the high-level feature list in the README and explains everything Grex can do once you start exploring beyond the basics.

## Core Search Power

- **Multi-environment paths** – Search Windows drive letters, UNC shares, and WSL paths (`\\wsl$`, `\\wsl.localhost`, `/mnt/...`) from the same tab.
- **Text or Regex** – Toggle between plain-text comparison and full .NET regular expressions, including multiline and global modes in the Regex Builder.
- **Case sensitivity everywhere** – One checkbox controls case behavior for text, Regex, and WSL `grep` invocations so results stay consistent.
- **Parallel pipeline** – Up to eight files are scanned concurrently with streaming IO, so even multi‑gigabyte trees stay responsive.
- **Smart filters** – Filename pattern matching, directory exclusions, `.gitignore` awareness, binary detection, symbolic link handling, size constraints, and Windows Search index acceleration all stack together.

## Docker Container Search

- **Opt-in setting** – Flip "Enable Docker Search" inside Settings to reveal Docker-aware controls on every tab. Grex remembers this preference for new tabs.
- **Container picker** – A new dropdown beside the path box defaults to "Local Disk" and lists every running container (with a quick refresh button). Choosing a container tells Grex to treat whatever path you enter as a container path such as `/var/www/html`.
- **Dual search strategies** – Grex automatically selects the fastest available search method:
  - **Direct grep (preferred)** – Uses the Docker API to run `grep` directly inside the container with parallel execution (`xargs -P 4`) for blazing-fast searches. Grex checks if `grep` is available in the container before attempting this method.
  - **Mirror fallback** – If `grep` is not available in the container (e.g., minimal Alpine images without coreutils), Grex falls back to mirroring the container path into `%LocalAppData%\Grex\docker-mirrors` and searching the local copy.
- **Performance optimizations** – Docker searches use several optimizations:
  - **Grep availability caching** – Results are cached per container to avoid repeated availability checks.
  - **Parallel grep execution** – Uses `find -print0 | xargs -0 -P 4 grep` for multi-core parallel searching.
  - **Smart filtering at find level** – System paths, hidden files, and binary extensions are filtered at the `find` level before grep runs.
- **Automatic method selection** – The search method is chosen transparently; you'll get results either way without needing to configure anything.
- **Symbolic link handling** – When the "Include symbolic links" option is unchecked (default), Grex uses `tar --dereference` to copy actual file contents instead of creating symlinks, avoiding Windows privilege issues with directories like `node_modules`.
- **Safety-first behavior** – Replace mode stays disabled while a container target is selected, Windows Search integration is automatically turned off, and the Browse button is greyed out to avoid mixing host folders with container roots.
- **Container-aware menus** – Right-clicking a result while in Docker mode shows a simplified menu with Copy Container Path / Copy File Name options that place the translated container path on your clipboard, making it easy to jump back into `docker exec` or your IDE.

## Advanced Filters & Size Limits

- **Match Files** – Include/exclude glob‑style patterns (`*.json|*.txt|-*.log`) for quick scoping.
- **Exclude Dirs** – Drop unwanted folders via comma-separated names or full Regex.
- **Respect .gitignore** – Honors nested `.gitignore` files just like Git.
- **Include System / Hidden / Binary / Symbolic Links** – Opt-in flags control what parts of the filesystem are considered. When "Include system files" is unchecked, Grex automatically excludes: `.git`, `vendor`, `node_modules`, `storage/framework`, `bin`, `obj`, `sys`, `proc`, and `dev` directories.
- **Size limit with tolerance** – Choose No Limit, Less Than, Equal To, or Greater Than with KB/MB/GB units and automatic tolerances so rounding never hides important files.
- **Windows Search integration** – Leverage the OS index for instant candidate discovery on indexed Windows folders; Grex seamlessly falls back to the custom file walker elsewhere.

## Localization & Accessibility

- **100+ built-in languages** – Includes English, Spanish, French, German, Chinese, Japanese, Arabic, Hindi, and many more, with instant UI updates when you switch languages.
- **Resource-based strings** – Every label, tooltip, placeholder, and status message lives in `.resw` files handled by a resilient `LocalizationService`.
- **Fallback safety** – Missing translations return the key itself so tests and headless automation never crash.
- **Automation-friendly tooltips** – All tooltips flow through the localization service and update across Search, Regex Builder, Settings, and About whenever the app language changes.
- **Easy entry addition** – Use `Scripts/add_localization_entry.py` to add new localization keys to all 100+ language files at once.

## Context Preview

- **On-demand context loading** – Press **Space** on any search result to instantly see surrounding lines without leaving your search view.
- **Configurable context size** – Adjust the number of lines shown before and after the match in Settings → Context Preview (1-20 lines, default 5).
- **Visual match highlighting** – The matched line is highlighted with a distinct background color and indicator bar, making it easy to spot in the context.
- **Monospace code display** – Context is displayed in a monospace font with line numbers, preserving code formatting and making it easy to read.
- **Quick file access** – Click "Open in Editor" in the preview dialog to jump directly to the file at the matched line.
- **Right-click menu integration** – The "Preview" option is also available in the context menu for mouse-driven workflows.
- **WSL path support** – Context preview works seamlessly with WSL paths, automatically converting paths as needed.

## Refined User Experience

- **Tabbed workflow** – Keep multiple searches open at once, each with its own filters and results.
- **Navigation pane** – One-click access to Search, Regex Builder, Settings, and About; collapsible for a compact layout.
- **CommandBar shortcuts** – Search, AI, Replace, Reset, and Filter toggles stay in reach.
- **Dual result modes** – Content view for per-line hits, Files view for aggregated summaries with size, match counts, encoding, and timestamps.
- **Column control** – Drag to resize, double-click to auto-fit, right-click to hide/show, with preferences persisted per table.
- **Responsive layout** – Narrow windows flip the filter stack vertical, and the entire shell follows your theme preference via a dropdown with eleven options: System Default, Light, Dark, and eight high-contrast themes (Black Knight, Paranoid, Diamond, Subspace, Red Velvet, Dreams, Tiefling, Vibes) for enhanced accessibility and visual variety.
- **Search timing** – The status bar shows exactly how long each search took, formatted intelligently (milliseconds for fast searches, seconds/minutes/hours for longer ones) with proper singular/plural handling.

## Search & Replace Safeguards

- **Replace workflow** – Enter replacement text, confirm via modal dialog ("Proceed / Cancel"), and watch the Files view highlight every file that will change.
- **Regex-aware replacements** – Use capture groups in your replace pattern for advanced refactors.
- **Undo warning** – Operations are permanent; Grex always warns you before touching files.
- **Stop on demand** – Click the Search or Replace button (which becomes "Stop" during an operation) to immediately cancel a running search or replace. The button reverts once the operation ends, and the other button is disabled to prevent conflicting operations.

## File Intelligence

- **Encoding detection** – Automatic BOM scanning, heuristics, and statistical analysis for 30+ encodings so matches are accurate even in multi-lingual repositories.
- **Document parsing** – Optional binary search mode indexes Office Open XML, OpenDocument, PDF, RTF, and ZIP archives by extracting their textual content.
- **Metadata columns** – Quickly inspect size (human readable), encoding, relative path, and modified timestamps when triaging results.

## Settings & Personalization

- Every toggle on the Settings page becomes the default for new tabs: search type, results mode, filter options, comparison mode, Unicode normalization, diacritic sensitivity, culture, theme, and more.
- **Default Match Files & Exclude Dirs** – Set default filename patterns and directory exclusions in Settings → Filter Options. New tabs automatically populate with your preferred values (e.g., `*.cs;*.json` for Match Files or `^(.git|node_modules|vendor)$` for Exclude Dirs).
- **AI endpoint configuration** – Settings → AI Search stores endpoint URL, optional API key, and optional model for AI-assisted search chat.
- **Endpoint connectivity probe** – The **Test Endpoint** button validates endpoint/auth settings by calling `GET /v1/models` and showing success/failure feedback.
- Settings live in `%LocalAppData%\Grex\settings.json`, update instantly, and survive rebuilds.
- Column visibility, window size, and recent paths are all remembered automatically.

### Backup & Restore

- **Export Settings** – Save your current configuration as a timestamped JSON file (`settings_YYYY_MM_DD_H_mm_ss.json`) that you can store anywhere for backup or transfer to another machine.
- **Import Settings** – Browse for a previously exported backup file; Grex validates the JSON and merges it into your current settings so you don't lose machine-specific preferences like window position.
- **Restore Defaults** – One click deletes your settings file and restarts the application with factory defaults—useful when troubleshooting or starting fresh.

## Export Results

- **Multiple export formats** – Export search results to CSV, JSON, or copy to clipboard with a single click from the command bar.
- **Format-appropriate output** – CSV includes proper quoting and escaping, JSON is pretty-printed, clipboard format uses tab-separated values for easy pasting into spreadsheets.
- **Mode-aware exports** – Content mode exports include file name, line number, column, content, and paths; Files mode exports include size, match count, extension, encoding, and timestamps.
- **File picker integration** – Save exports directly to disk with timestamped suggested filenames.

## Search Within Results

- **Instant result filtering** – Filter the current results list without re-scanning the filesystem.
- **Flexible matching** – Content mode matches file name, relative path, and matched text; Files mode matches file name, relative path, extension, and encoding.
- **Regex support** – Toggle the `.*` button to filter using regular expressions instead of plain text.
- **Status-aware** – When filtered, the status line switches to "Showing … (filtered from …)"; clear the box to restore the full set.

## AI-Assisted Search Chat

- **Dedicated AI entry point** – The Search command is compact (icon only) and a separate **AI** command starts AI mode.
- **Chat-first workflow** – In AI mode, result grids and "Search within results" are hidden and replaced with a chat panel.
- **Rich context handoff** – Grex sends the active search path, search query, search mode, result mode, and filter states as guidance context.
- **OpenAI-compatible backend** – Endpoint and optional API key are configurable in Settings.
- **Optional model pinning** – Set a model explicitly, or leave it blank to auto-detect from `/v1/models` (with fallback to `gpt-4o-mini`).
- **One-click endpoint test** – Run the Settings **Test Endpoint** action to confirm your endpoint + credentials before entering AI chat mode.
- **Follow-up conversation** – Continue refining the search through additional chat turns without leaving the tab.

## Search History

- **Automatic tracking** – Grex remembers your last 20 search queries along with their paths and filter settings.
- **Quick recall** – Access recent searches from the search history dropdown to instantly restore a previous query with all its filters.
- **Smart deduplication** – Duplicate searches are moved to the top rather than creating new entries.
- **Easy management** – Remove individual entries or clear the entire history from the dropdown menu.
- **Persistent storage** – Search history survives application restarts, stored in `%LocalAppData%\Grex\search_history.json`.

## Search Profiles

- **Save named profiles** – Store the current tab’s path, query, and search/filter options as a reusable profile.
- **Apply in one click** – Pick a profile from the Profiles menu to instantly populate a tab with those settings.
- **Safe management** – Overwrite confirmations, per-profile deletion, and an empty-state UI keep things tidy.
- **Persistent storage** – Profiles are stored in `%LocalAppData%\Grex\search_profiles.json`.

## CLI Mode

- **Headless operation** – Run searches from the command line without launching the GUI, perfect for scripts and automation.
- **grep-compatible output** – Default text format uses `path:line:column:content` for easy parsing and pipeline integration.
- **Multiple output formats** – Choose text (default), JSON, or CSV with the `--format` flag.
- **Full filter support** – All GUI filters available as command-line flags: `--regex`, `--case-sensitive`, `--gitignore`, `--include-hidden`, `--match-files`, `--exclude-dirs`, and more.
- **Exit codes** – Returns 0 for matches found, 1 for no matches, 2 for errors, enabling use in shell conditionals.
- **Count and files-only modes** – Use `--count` to output just the match count, or `--files-only` for unique file paths (like `grep -l`).
- **Quiet mode** – Use `--quiet` for silent operation where only the exit code matters.

### CLI Usage Examples

```bash
# Basic search
grex-cli "C:\Projects" "TODO"

# Regex search with JSON output
grex-cli "C:\src" "TODO|FIXME" --regex --format json

# Respect .gitignore and filter by file type
grex-cli "C:\repo" "deprecated" --gitignore --match-files "*.cs;*.ts"

# Count matches only
grex-cli "C:\docs" "error" --count

# Files with matches (like grep -l)
grex-cli "C:\code" "FIXME" --files-only

# Quiet mode for scripts
grex-cli "C:\config" "secret" --quiet && echo "Found!" || echo "Not found"
```

## Productivity Touches

- **Recent paths AutoSuggest** – Reuse up to 20 prior locations with type-ahead filtering and per-entry removal.
- **Search history** – Quickly recall recent searches with all their filter settings from the search history dropdown.
- **Keyboard shortcuts** – Press Enter in the search box to run searches, in the replace box to confirm replacements, and double-click results to open files in the shell.
- **Admin awareness** – Grex warns when you launch it elevated (toast notifications can't fire in that state) and even tells you how to re-launch unelevated from an admin shell.

Use this document whenever you need the full breadth of features—README keeps the elevator pitch short while this file remains your exhaustive reference.

