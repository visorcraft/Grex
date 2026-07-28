---
title: Technical Reference
layout: default
---

# Technical Reference

This is the precise reference for Grex's UI commands, patterns, files, settings, outputs, and limits. See [Usage](usage.md) for guided workflows and [Architecture](architecture.md) for implementation flow.

## Keyboard and pointer commands

| Input | Context | Action |
| --- | --- | --- |
| Enter | Search query | Run Search; in AI mode, begin a new discussion |
| Enter | Replace text | Run Replace when enabled |
| Enter | AI follow-up | Send the message |
| F5 | Active Search tab | Run Search; if Search is active, invoke its Stop behavior |
| Escape | Active operation | Cancel Search or Replace |
| Escape | Idle Search tab | Clear search and replacement inputs |
| F1 | Anywhere in the main window | Open About |
| Space | Selected Content result | Open context preview |
| Double-click | Content or Files result | Open the file with the default application |
| Right-click | Result | Open file, preview, Explorer, or copy actions appropriate to the target |
| Right-click | Column header | Show or hide optional columns |
| Double-click | Column divider | Auto-fit that column |

## Path forms

| Target | Form | Example |
| --- | --- | --- |
| Windows | Drive path | `C:\source\Grex` |
| UNC | Share path | `\\server\share\logs` |
| WSL explicit | Legacy UNC | `\\wsl$\Ubuntu\home\user` |
| WSL explicit | Current UNC | `\\wsl.localhost\Ubuntu-24.04\home\user` |
| WSL default distro | Linux path | `/home/user` |
| WSL default distro | Mounted Windows path | `/mnt/c/Users/user` |
| Docker | Container path | `/app/src` |

The CLI validates `Directory.Exists` before invoking the search service. Use WSL UNC paths with the CLI; a raw `/home/...` path is not a Windows directory and fails CLI validation.

## Match Files syntax

Match Files compares only the filename, case-insensitively.

| Syntax | Meaning |
| --- | --- |
| `*` | Any number of characters |
| `?` | One character |
| `|` | Alternative separator |
| leading `-` | Exclude an alternative |

Examples:

| Pattern | Result |
| --- | --- |
| `*.cs` | Include C# files |
| `*.cs|*.xaml` | Include C# or XAML files |
| `*.json|-*.generated.json` | Include JSON except generated JSON |
| `-*.log` | Include everything except `.log` files |

Use `|`, not semicolons.

## Exclude Dirs syntax

Exclude Dirs supports:

1. comma-separated directory names, such as `.git,node_modules,vendor`;
2. a case-insensitive Regex matched against each path component and the relative directory path.

Use an anchored expression to make Regex intent explicit:

```regex
^(.git|node_modules|vendor)$
```

The UI rejects obviously invalid Regex-like input. The local engine falls back to comma-name handling if Regex construction fails.

## `.gitignore` behavior

Local Windows and mirror searches use `GitIgnoreService`, which supports:

- comments and blank lines;
- negation;
- root-relative patterns;
- directory-only patterns;
- `*`, `?`, `**`, and bracket patterns;
- nested `.gitignore` files.

The parser caches at most 100 entries and evicts half when over capacity. Ignore-rule Regex operations use a 5-second timeout.

Target differences:

- WSL at a Git root uses `git grep`, which searches files visible to Git. Otherwise WSL uses normal `find`/`grep`.
- Direct Docker reads only `<search-root>/.gitignore`, uses simplified wildcard matching, and ignores negation.
- Docker mirror fallback uses the local parser on the copied tree.

## System path exclusions

When **Include system files** is off, local search excludes Windows `System` attributes and any path component or path pair below:

| Exclusion | Scope |
| --- | --- |
| `.git` | VCS metadata |
| `vendor` | Common dependency directory |
| `node_modules` | Node dependencies |
| `storage/framework` | Laravel generated state |
| `bin` | Build output |
| `obj` | Build intermediates |
| `sys` | WSL/Docker system tree |
| `proc` | WSL/Docker process tree |
| `dev` | WSL/Docker device tree |

Hidden filtering is separate. With **Include hidden items** off, Grex skips the Windows `Hidden` attribute and dot-prefixed files or directory components.

## Size filter tolerances

| Unit | Tolerance |
| --- | --- |
| KB | 10 KB |
| MB | 1 MB |
| GB | 25 MB |

The local engine applies:

- Less Than: `size < limit + tolerance`
- Equal To: `abs(size - limit) <= tolerance`
- Greater Than: `size > limit - tolerance`

WSL maps size to `find -size`, whose boundary behavior can differ. Direct Docker search does not apply size.

The CLI passes the numeric size and selected unit directly to the search service. For example, `--size-limit 2 --size-unit MB` means 2 MB.

## Text comparison

### StringComparisonMode

| JSON value | Mode |
| --- | --- |
| `0` | Ordinal |
| `1` | CurrentCulture |
| `2` | InvariantCulture |

Case sensitivity selects the matching sensitive/insensitive form.

### UnicodeNormalizationMode

| JSON value | Mode |
| --- | --- |
| `0` | None |
| `1` | NFC / Form C |
| `2` | NFD / Form D |
| `3` | NFKC / Form KC |
| `4` | NFKD / Form KD |

When diacritic sensitivity is off, Grex decomposes text and removes non-spacing marks before comparison.

These settings affect local plain-text matching. Regex, WSL grep, and direct Docker grep do not use the full comparison pipeline.

## Regex behavior

| Operation | Engine | Timeout |
| --- | --- | --- |
| Local search and replace | .NET Regex | 10 seconds per match operation |
| Local wildcard conversion | .NET Regex | 10 seconds |
| `.gitignore` parser | .NET Regex | 5 seconds |
| Docker result parsing/filter helpers | .NET Regex | 5 seconds |
| Result filter | .NET Regex | 2 seconds |
| Exclude Dirs UI validation | .NET Regex | 2 seconds |
| WSL / direct Docker search | target `grep -E` | target-defined |
| WSL replacement | target `sed -E` | target-defined |

User-supplied .NET patterns use `RegexOptions.None` plus optional `IgnoreCase`, never `RegexOptions.Compiled`.

An invalid search Regex reports an error. An invalid result-filter Regex displays all current results until valid.

## Searchable documents and binary formats

The local extractor recognizes:

| Format | Extensions | Method |
| --- | --- | --- |
| Office Open XML | `.docx`, `.xlsx`, `.pptx` | Search `.xml`, `.txt`, and `.rels` ZIP entries |
| OpenDocument | `.odt`, `.ods`, `.odp` | Search `.xml`, `.txt`, and `.rels` ZIP entries |
| ZIP | `.zip` | Search selected textual entries |
| PDF | `.pdf` | Best-effort raw stream and text-object scan, maximum 50 MB |
| Rich Text | `.rtf` | Strip common RTF controls and search readable text |

This is not OCR or full document parsing. Password-protected, scanned, heavily compressed, or unusual documents may not match.

Known binary extensions that remain excluded include legacy Office (`.doc`, `.xls`, `.ppt`), images, media, executables, libraries, and most archive types.

Replacement does not reconstruct these formats. Replace skips known binary, archive, and document extensions even when binary search is enabled.

## Encoding detection

Encoding detection reads at most 65,536 bytes and uses:

1. BOM detection;
2. candidate decoding and statistical scoring;
3. Shift-JIS, Chinese, Korean, and Cyrillic-oriented heuristics;
4. UTF-8 fallback.

Built-in Unicode candidates are UTF-8, UTF-16 LE/BE, and UTF-32 LE. The service also attempts to load UTF-32 BE, ISO-8859 variants, Windows-125x, Shift-JIS, GB2312/GBK, Big5, EUC-KR, and KOI8-R/U. Availability depends on the encoding providers registered by the runtime.

WSL and direct Docker results report UTF-8-oriented content rather than running the full local detector.

## Results

### Content row

| Field | Meaning |
| --- | --- |
| FileName | Basename, or archive plus entry name |
| LineNumber | 1-based source/extracted line |
| ColumnNumber | 1-based first-match column |
| LineContent | Sanitized matching line |
| MatchCount | Occurrences on this line |
| FullPath | Host, WSL, or container path |
| RelativePath | Path relative to the search root |
| MatchPreviewBefore/Match/After | Tooltip segments centered near the first match |

Match previews are capped at 400 characters. Extracted archive/PDF display lines are capped at 500 characters plus an ellipsis.

### Files row

| Field | Meaning |
| --- | --- |
| FileName | Basename |
| Size | Bytes |
| MatchCount | Sum across matching lines |
| FirstMatchLineNumber | First match when known |
| PreviewMatches | First few line previews |
| FullPath / RelativePath | Absolute and root-relative paths |
| Extension | File extension |
| Encoding | Detected encoding label |
| DateModified | Last modification time |

## Result export

GUI export uses the currently displayed result mode and filter:

- Content CSV/JSON/clipboard: name, line, column, content, relative path, full path, and match count
- Files CSV/JSON/clipboard: name, size, match count, relative path, full path, extension, encoding, and modified time

Clipboard output is tab-separated.

## User data and files

| Data | Path | Retention |
| --- | --- | --- |
| Settings | `%LocalAppData%\Grex\settings.json` | Until reset/manual deletion |
| Recent paths | `%LocalAppData%\Grex\search_path_history.json` | 20 |
| Search history | `%LocalAppData%\Grex\search_history.json` | 20 |
| Search profiles | `%LocalAppData%\Grex\search_profiles.json` | 50 |
| Docker mirrors | `%LocalAppData%\Grex\docker-mirrors\` | Active cleanup; crash leftovers require manual deletion |
| Application log | `%Temp%\Grex.log` | About 1 MB cap, then trimmed near 512 KB |
| Notification test log | `%LocalAppData%\Grex\notification_test.log` | Diagnostic test output |
| Installed app | `%LocalAppData%\Programs\Grex` | Installer default |

No search index or result database is persisted by Grex.

## Settings schema

`%LocalAppData%\Grex\settings.json` is a JSON serialization of `DefaultSettings`. Enum values are numeric.

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

`Culture` defaults to the current machine culture, so it may differ from this example. Preview line values are clamped to 1 through 20 when read or written.

### SizeUnit

| Value | Unit |
| --- | --- |
| `0` | KB |
| `1` | MB |
| `2` | GB |

### ThemePreference

| Value | Theme |
| --- | --- |
| `0` | System |
| `1` | Light |
| `2` | Dark |
| `3` | Gentle Gecko |
| `4` | Black Knight |
| `5` | Diamond |
| `6` | Dreams |
| `7` | Paranoid |
| `8` | Red Velvet |
| `9` | Subspace |
| `10` | Tiefling |
| `11` | Vibes |

### Settings write behavior

- Settings are cached in process and saved immediately after most changes.
- Missing or unreadable JSON returns defaults.
- Save failures are ignored by the service.
- Restore Defaults deletes only `settings.json`.
- The AI API key is plain text.

## Settings backup and import

Export serializes all `DefaultSettings` fields, including:

- window position and size;
- AI endpoint;
- AI API key;
- AI model.

Treat exported JSON as a secret when it contains a key.

Import validates JSON and merges only fields present in the imported object. It applies every exported non-window field, including Docker enablement, default Match Files/Exclude Dirs, context-preview counts, and AI settings. Unknown fields are ignored. Invalid enum values are rejected. Window position and size remain machine-local and are never imported.

## Search history schema

Each entry in `search_history.json` contains:

- `SearchTerm`, `SearchPath`;
- `MatchFileNames`, `ExcludeDirs`;
- `IsRegexSearch`, `IsFilesSearch`;
- `SearchCaseSensitive`, `RespectGitignore`;
- `IncludeSubfolders`, `IncludeHiddenItems`, `IncludeBinaryFiles`;
- `Timestamp`, `ResultCount`.

The deduplication key uses term, path, search/result modes, case sensitivity, Match Files, and Exclude Dirs.

## Search profile schema

Each profile contains:

- name, path, and term;
- search and result modes;
- `.gitignore`, case, system, recursion, hidden, binary, link, and Windows Search flags;
- Match Files and Exclude Dirs;
- size type, value, and unit;
- string comparison, normalization, diacritic, and culture values;
- created and updated timestamps.

Profiles do not contain result rows, replacement text, or a Docker container id.

## AI protocol

### Endpoint construction

| Configured endpoint | Chat URL | Models URL |
| --- | --- | --- |
| `https://host/v1` | `https://host/v1/chat/completions` | `https://host/v1/models` |
| `https://host` | `https://host/v1/chat/completions` | `https://host/v1/models` |
| exact `.../chat/completions` | unchanged for chat | base normalization may not suit model discovery |
| exact `.../models` | base normalization may not suit chat | unchanged for models |

Grex prepends `https://` when the scheme is missing and trims trailing slashes. It also accepts an explicit `http://` URL, but HTTPS is strongly recommended.

### Request

The chat request contains:

```json
{
  "model": "configured-or-resolved-id",
  "temperature": 0.2,
  "messages": [
    { "role": "system", "content": "Grex assistant instructions" },
    { "role": "system", "content": "Path, query, modes, and filter suggestions" },
    { "role": "user", "content": "Conversation..." }
  ]
}
```

An `Authorization: Bearer <key>` header is added only when the key is non-empty.

### Model selection

1. Use a configured non-empty model exactly.
2. Otherwise request the first `data[].id` from Models.
3. Fall back to `gpt-4o-mini`.

The resolved model is cached until the normalized endpoint changes.

### Response parsing

Grex accepts:

- `choices[0].message.content` as a string or content-part array;
- `choices[0].text`;
- top-level `output_text`.

Structured `error.message` is preferred for failures. HTTP requests use a shared client with a 90-second timeout.

## CLI reference

### Syntax

```text
grex-cli <path> <term> [options]
```

The path comes first.

### Options

| Option | Short | Default | Meaning |
| --- | --- | --- | --- |
| `--regex` | `-E` | off | Treat term as Regex |
| `--case-sensitive` | `-i` | off | Case-sensitive search |
| `--gitignore` | `-g` | off | Respect `.gitignore` |
| `--include-hidden` | `-H` | off | Include hidden items |
| `--include-binary` | `-b` | off | Include searchable binary/document formats |
| `--include-system` | `-s` | off | Include system files and normally excluded directories |
| `--no-subfolders` | `-d` | off | Do not recurse |
| `--include-symlinks` | `-L` | off | Follow symbolic links |
| `--match-files <pattern>` | `-m` | empty | Filename pattern using `|` alternatives |
| `--exclude-dirs <value>` | `-x` | empty | Comma names or Regex |
| `--size-limit <number>` | | none | Size value |
| `--size-unit <KB\|MB\|GB>` | | `KB` | Size unit |
| `--size-type <less\|equal\|greater>` | | `less` | Size comparison |
| `--format <text\|json\|csv>` | `-f` | `text` | Output format |
| `--count` | `-c` | off | Print total occurrences |
| `--files-only` | `-l` | off | Print unique full paths |
| `--quiet` | `-q` | off | Print nothing |

The short `-i` flag is intentionally wired to case-sensitive behavior in the current CLI, unlike GNU grep's conventional meaning.

Invalid size units fall back to KB. Invalid size types become No Limit.

Output-mode priority is Quiet, Count, Files Only, then Format.

### Examples

```powershell
grex-cli "C:\repo" "TODO"
grex-cli "C:\repo" "TODO|FIXME" --regex --case-sensitive
grex-cli "C:\repo" "deprecated" --gitignore --match-files "*.cs|*.xaml"
grex-cli "C:\repo" "error" --exclude-dirs ".git,node_modules" --format json
grex-cli "C:\repo" "warning" --count
grex-cli "C:\repo" "secret" --quiet
```

PowerShell exit-code use:

```powershell
grex-cli "C:\repo" "TODO" --quiet
if ($LASTEXITCODE -eq 0) { "Found" }
elseif ($LASTEXITCODE -eq 1) { "No match" }
else { "Search error" }
```

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | At least one result |
| `1` | No result |
| `2` | Invalid path, invalid pattern, cancellation, or another error |

### Text output

One line per matching source line, using a relative path:

```text
path\file.cs:42:10:// TODO: fix this
```

### JSON output

```json
[
  {
    "file": "path\\file.cs",
    "line": 42,
    "column": 10,
    "content": "// TODO: fix this",
    "matchCount": 1,
    "fullPath": "C:\\repo\\path\\file.cs"
  }
]
```

### CSV output

```csv
File,Line,Column,Content,FullPath,MatchCount
path\file.cs,42,10,// TODO: fix this,C:\repo\path\file.cs,1
```

Fields containing commas, quotes, or newlines are CSV-escaped. `--files-only` prints unique full paths rather than formatted records.

### CLI boundaries

The CLI does not expose:

- Docker targets;
- Replace;
- Windows Search;
- culture, normalization, or diacritic flags;
- GUI history, profiles, preview, or export.

## Operational caps

| Resource | Cap |
| --- | --- |
| Local file search/replacement concurrency | 8 files |
| Direct Docker grep workers | 4 |
| Match tooltip preview | 400 characters |
| PDF extraction input | 50 MB |
| Local replace file size | 100 MB |
| Encoding-detection read | 64 KB |
| Context preview seek read | 1 MB |
| Context lines before/after | 1 to 20 each |
| Recent paths | 20 |
| Recent searches | 20 |
| Search profiles | 50 |
| AI UI/history messages | 200 |
| `.gitignore` cache | 100 |
| Docker grep cache | 50 containers |
| Localization resource contexts | 20 |
| Application log | about 1 MB |

## Current limitations

- Windows-only GUI and CLI
- Published release only for win-x64
- No replace undo or rollback
- No empty-string replacement through the GUI
- No Docker replacement
- Direct Docker size filtering unavailable
- Docker direct `.gitignore` support is partial
- WSL/Docker Regex syntax differs from .NET Regex
- Best-effort document extraction, no OCR
- No automatic updater
- Non-English localization catalogs are incomplete
- AI keys and settings exports are plain text
- CLI MB/GB size conversion issue described above
