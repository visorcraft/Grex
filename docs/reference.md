---
title: Technical Reference
layout: default
---

# Technical Reference

This document provides detailed technical specifications for power users and developers who need precise information about Grex's behavior, file formats, and configuration.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Enter** | Execute search (when focus is in search text box) |
| **Enter** | Execute replace (when focus is in "Replace with" text box) |
| **Space** | Open context preview for selected search result (Content mode) |
| **Escape** | Close context preview dialog |
| **F1** | Open the About page (available from anywhere in the application) |
| **Double-Click** | Open file from search results (opens in default application) |

## Context Menu Options (Content Mode)

Right-click any search result in Content mode to access these options:

| Option | Description |
|--------|-------------|
| **Preview (Space)** | Opens context preview dialog showing surrounding lines |
| **Show in Explorer** | Opens Windows Explorer with the file selected |
| **Copy Path** | Copies the full file path to clipboard |
| **Copy File Name** | Copies just the file name to clipboard |

## Settings File Structure

Grex stores settings in `%LocalAppData%\Grex\settings.json`. Here's the complete structure:

```json
{
  "IsRegexSearch": false,
  "IsFilesSearch": false,
  "RespectGitignore": false,
  "SearchCaseSensitive": false,
  "IncludeSystemFiles": false,
  "IncludeSubfolders": true,
  "IncludeHiddenItems": false,
  "IncludeBinaryFiles": false,
  "IncludeSymbolicLinks": false,
  "UseWindowsSearchIndex": false,
  "EnableDockerSearch": false,
  "SizeUnit": 0,
  "ThemePreference": 3,
  "UILanguage": "en-US",
  "StringComparisonMode": 0,
  "UnicodeNormalizationMode": 0,
  "DiacriticSensitive": true,
  "Culture": "en-US",
  "DefaultMatchFiles": "",
  "DefaultExcludeDirs": "",
  "ContentLineColumnVisible": true,
  "ContentColumnColumnVisible": true,
  "ContentPathColumnVisible": true,
  "FilesSizeColumnVisible": true,
  "FilesMatchesColumnVisible": true,
  "FilesPathColumnVisible": true,
  "FilesExtColumnVisible": true,
  "FilesEncodingColumnVisible": true,
  "FilesDateModifiedColumnVisible": true,
  "WindowX": null,
  "WindowY": null,
  "WindowWidth": 1100,
  "WindowHeight": 700,
  "ContextPreviewLinesBefore": 5,
  "ContextPreviewLinesAfter": 5,
  "AiSearchEndpoint": "https://api.openai.com/v1",
  "AiSearchApiKey": "",
  "AiSearchModel": "gpt-4o-mini"
}
```

### Enum Values

**ThemePreference:**
| Value | Theme |
|-------|-------|
| 0 | System Default |
| 1 | Light Mode |
| 2 | Dark Mode |
| 3 | Gentle Gecko (High Contrast) |
| 4 | Black Knight (High Contrast) |
| 5 | Diamond (High Contrast) |
| 6 | Dreams (High Contrast) |
| 7 | Paranoid (High Contrast) |
| 8 | Red Velvet (High Contrast) |
| 9 | Subspace (High Contrast) |
| 10 | Tiefling (High Contrast) |
| 11 | Vibes (High Contrast) |

**SizeUnit:**
| Value | Unit |
|-------|------|
| 0 | KB (Kilobytes) |
| 1 | MB (Megabytes) |
| 2 | GB (Gigabytes) |

**StringComparisonMode:**
| Value | Mode |
|-------|------|
| 0 | Ordinal |
| 1 | CurrentCulture |
| 2 | InvariantCulture |

## AI Search Request Context

When AI mode is enabled from the Search command bar, Grex sends an OpenAI-compatible chat request with:

- Two system messages:
  - AI assistant behavior instructions.
  - Search context details (path, query, search type, results mode, and filter suggestions).
- The current conversation turns (user + assistant) for follow-up refinement.

Model selection behavior:

- If **Settings → AI Search → Model** is set, Grex uses that exact model id.
- If model is blank, Grex requests `GET /v1/models` on the configured endpoint and uses the first returned model id.
- If discovery fails, Grex falls back to `gpt-4o-mini`.

### AI Endpoint Test Button Behavior

Settings → AI Search includes a **Test Endpoint** button that validates connectivity before you start chat:

- Builds a models URL from your configured endpoint (`<base>/v1/models`, or `<base>/models` if already specified).
- Sends `GET /v1/models` with `Authorization: Bearer <apiKey>` when an API key is present.
- Reports success with the first returned model id (or "(no model id returned)" if absent).
- Reports endpoint errors by preferring structured API messages (`error.message`) and falling back to HTTP reason text.

## Search Profiles File Structure

Grex stores saved search profiles in `%LocalAppData%\Grex\search_profiles.json` as a JSON array. Each entry is a snapshot of the tab's search settings at the time the profile was saved.

Example:

```json
[
  {
    "Name": "My Project",
    "SearchPath": "C:\\Projects\\MyApp",
    "SearchTerm": "TODO",
    "IsRegexSearch": false,
    "IsFilesSearch": false,
    "RespectGitignore": true,
    "SearchCaseSensitive": false,
    "IncludeSystemFiles": false,
    "IncludeSubfolders": true,
    "IncludeHiddenItems": false,
    "IncludeBinaryFiles": false,
    "IncludeSymbolicLinks": false,
    "UseWindowsSearchIndex": false,
    "MatchFileNames": "*.cs;*.md",
    "ExcludeDirs": "bin,obj",
    "SizeLimitType": 0,
    "SizeLimitKB": null,
    "SizeUnit": 0,
    "StringComparisonMode": 0,
    "UnicodeNormalizationMode": 0,
    "DiacriticSensitive": true,
    "Culture": "en-US",
    "CreatedAt": "2026-01-20T10:00:00",
    "UpdatedAt": "2026-01-20T10:00:00"
  }
]
```

## Size Limit Tolerances

When filtering files by size, Grex applies tolerance ranges to handle rounding:

| Unit | Tolerance | Example |
|------|-----------|---------|
| KB | ±10 KB | "Equal To 100 KB" matches 90-110 KB |
| MB | ±1 MB | "Equal To 10 MB" matches 9-11 MB |
| GB | ±25 MB | "Equal To 1 GB" matches ~975 MB-1025 MB |

Tolerances apply to all operations: "Less Than" allows files up to (limit + tolerance), "Greater Than" allows files down to (limit - tolerance).

## Supported Binary File Formats

When "Include binary files" is enabled, Grex can search text content within these formats:

### Supported Formats

| Format | Extensions | Method |
|--------|------------|--------|
| Office Open XML | `.docx`, `.xlsx`, `.pptx` | Extracts XML content from ZIP archive |
| OpenDocument | `.odt`, `.ods`, `.odp` | Extracts XML content from ZIP archive |
| ZIP Archives | `.zip` | Searches file names and XML/text content |
| PDF Documents | `.pdf` | Extracts text from PDF streams |
| Rich Text Format | `.rtf` | Removes RTF control codes, searches text |

### Unsupported Formats

These binary types cannot be searched (excluded even with "Include binary files" enabled):

- **Legacy Office**: `.doc`, `.xls`, `.ppt` (OLE compound format)
- **Images**: `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`, `.ico`, `.svg`, `.webp`
- **Media**: `.mp3`, `.mp4`, `.avi`, `.mkv`, `.wav`, `.flac`, `.ogg`
- **Executables**: `.exe`, `.dll`, `.bin`
- **Archives**: `.tar`, `.gz`, `.7z`, `.rar`
- **Other binary**: `.pdb`, `.cache`, `.lock`, `.pack`, `.idx`

## Encoding Detection

Grex's encoding detection service supports 30+ encodings using multiple detection methods:

### Detection Methods

1. **BOM Detection** (95% confidence when present)
   - UTF-8, UTF-16 LE/BE, UTF-32 LE/BE

2. **Statistical Analysis**
   - Valid character sequence validation
   - Character frequency analysis
   - File name hint processing
   - Common text pattern detection

3. **Heuristic Analysis**
   - Byte pattern recognition for Shift-JIS, GB2312, EUC-KR

### Supported Encodings

| Category | Encodings |
|----------|-----------|
| Unicode | UTF-8 (with/without BOM), UTF-16 LE/BE, UTF-32 LE/BE |
| ISO-8859 | Latin-1 through Latin-10, Cyrillic, Arabic, Greek, Hebrew, Turkish, Thai |
| Windows | Windows-1250 through Windows-1258 |
| Asian | Shift-JIS, GB2312/GBK, Big5, EUC-KR |
| Cyrillic | KOI8-R (Russian), KOI8-U (Ukrainian) |

## Result Display Columns

### Content Mode

| Column | Description |
|--------|-------------|
| Name | File name |
| Line | Line number of match |
| Column | Column position within the line |
| Text | Matching text snippet (up to 500 characters) |
| Path | Relative path from search root |

### Files Mode

| Column | Description |
|--------|-------------|
| Name | File name |
| Size | Human-readable file size (B/KB/MB/GB) |
| Match Count | Number of matches in file |
| Path | Relative path from search root |
| Extension | File extension |
| Encoding | Detected file encoding |
| Date Modified | Last modification timestamp |

## Search Within Results

After a search completes, Grex can filter the current results list client-side (without re-running the filesystem scan):

- **Content mode**: matches file name, relative path, and line content.
- **Files mode**: matches file name, relative path, extension, and encoding.
- **Regex mode**: Click the `.*` toggle button next to the filter box to use regular expressions instead of plain text matching.

## User Data Locations

| Data | Location |
|------|----------|
| Settings | `%LocalAppData%\Grex\settings.json` |
| Recent Paths | `%LocalAppData%\Grex\search_path_history.json` |
| Search History | `%LocalAppData%\Grex\search_history.json` |
| Search Profiles | `%LocalAppData%\Grex\search_profiles.json` |
| Docker Mirrors | `%LocalAppData%\Grex\docker-mirrors\` |
| Application Logs | `%Temp%\Grex.log` |
| Notification Diagnostics | `%LocalAppData%\Grex\notification_test.log` |

## Match Files Pattern Syntax

The "Match Files" filter supports glob-style patterns:

| Pattern | Meaning |
|---------|---------|
| `*.json` | Match all JSON files |
| `*.txt` | Match all text files |
| `*.json\|*.txt` | Match JSON or text files (pipe separator) |
| `-*.log` | Exclude log files (dash prefix) |
| `*.json\|-*.bak` | Match JSON files, exclude backup files |

## Exclude Dirs Syntax

The "Exclude Dirs" filter supports two modes:

1. **Comma-separated names**: `node_modules,vendor,.git`
2. **Regex patterns**: `^(.git|vendor|node_modules)$`

When the value contains `^`, `$`, or `|` but no comma, it's validated as a Regex pattern. Invalid Regex patterns show an error notification and cancel the operation.

## System Paths Auto-Exclusion

When "Include system files" is **unchecked**, Grex automatically excludes these directories:

| Directory | Reason |
|-----------|--------|
| `.git` | Git version control |
| `vendor` | PHP/Composer dependencies |
| `node_modules` | Node.js dependencies |
| `storage/framework` | Laravel framework cache |
| `bin` | Build output (.NET) |
| `obj` | Build intermediates (.NET) |
| `sys` | Linux system (Docker/WSL) |
| `proc` | Linux process info (Docker/WSL) |
| `dev` | Linux devices (Docker/WSL) |

This exclusion applies to Windows local searches, WSL searches, and Docker container searches.

## Search Timing Display

The status bar formats elapsed time intelligently:

| Duration | Format Example |
|----------|----------------|
| < 30 seconds | "12.43 seconds" (with milliseconds) |
| 30-59 seconds | "45 seconds" (whole seconds) |
| 1-59 minutes | "2 minutes 15 seconds" |
| 60+ minutes | "1 hour 9 minutes" |

Singular/plural forms are used correctly ("1 minute" vs "2 minutes").

## WSL Path Formats

Grex supports multiple WSL path formats:

| Format | Example |
|--------|---------|
| `\\wsl$\` UNC | `\\wsl$\Ubuntu\home\user` |
| `\\wsl.localhost\` UNC | `\\wsl.localhost\Ubuntu-24.04\home\user` |
| Unix-style | `/mnt/c/Users/You` |

All formats are converted appropriately when shelling out to WSL `grep`.

## CLI Reference

The CLI (`grex-cli`) provides headless search for script integration.

### Command Syntax

```
grex-cli <path> <term> [options]
```

### CLI Options

| Option | Short | Description |
|--------|-------|-------------|
| `--regex` | `-E` | Treat search term as regex pattern |
| `--case-sensitive` | `-i` | Case sensitive search |
| `--gitignore` | `-g` | Respect .gitignore files |
| `--include-hidden` | `-H` | Include hidden files |
| `--include-binary` | `-b` | Include binary files |
| `--include-system` | `-s` | Include system files |
| `--no-subfolders` | `-d` | Don't recurse into subdirectories |
| `--include-symlinks` | `-L` | Follow symbolic links |
| `--match-files` | `-m` | File name pattern (e.g., `*.cs;*.txt`) |
| `--exclude-dirs` | `-x` | Directories to exclude (semicolon-separated) |
| `--size-limit` | | File size limit |
| `--size-unit` | | Size unit: KB, MB, GB (default: KB) |
| `--size-type` | | Size comparison: less, equal, greater (default: less) |
| `--format` | `-f` | Output format: text, json, csv (default: text) |
| `--count` | `-c` | Only print total match count |
| `--files-only` | `-l` | Only print file names with matches |
| `--quiet` | `-q` | Suppress all output, exit code indicates match |

### CLI Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Matches found |
| 1 | No matches found |
| 2 | Error (invalid path, bad regex, etc.) |

### CLI Output Formats

**Text (default)** - grep-compatible format:
```
path/to/file.cs:42:10:// TODO: Fix this
```

**JSON** - Pretty-printed array:
```json
[
  {
    "file": "path/to/file.cs",
    "line": 42,
    "column": 10,
    "content": "// TODO: Fix this",
    "matchCount": 1,
    "fullPath": "C:\\Projects\\path\\to\\file.cs"
  }
]
```

**CSV** - Header row plus data:
```
File,Line,Column,Content,FullPath,MatchCount
path/to/file.cs,42,10,// TODO: Fix this,C:\Projects\path\to\file.cs,1
```

## Limitations

- Search results display up to 500 characters per line (full content available in tooltips)
- Maximum of 20 recent paths stored
- Maximum of 20 recent searches stored
- File encoding detection for WSL paths defaults to UTF-8
- Replace operations modify files directly (no undo functionality)
- Windows Search integration only works for indexed Windows paths with plain-text searches
- CLI does not support Docker container searches or replace operations


