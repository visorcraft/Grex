---
title: Translation and Localization
layout: default
---

# Translation and Localization

Grex uses Windows `.resw` resource catalogs under `Strings/<culture>/Resources.resw`.

The repository currently contains:

- one source catalog: `Strings/en-US/Resources.resw`;
- 107 non-English catalogs;
- 476 English resource entries at the time of this documentation audit.

Non-English catalogs are not complete. Do not describe every listed locale as fully translated. Run the status script for current counts.

## Runtime behavior

`LocalizationService`:

- loads strings for the selected culture;
- falls back to `en-US` when possible;
- returns the resource key when lookup fails;
- formats parameterized strings;
- updates thread culture;
- raises `PropertyChanged` when culture changes;
- caps cached resource contexts at 20.

The Settings language list is built from culture directories that contain `Resources.resw`. If packaged resource files cannot be enumerated, the UI uses a fallback culture list.

Views refresh dynamic text after a culture change. `LocalizedToolTipRegistry` refreshes registered tooltips. Some WinUI resource behavior may still require an application restart.

## Resource lookup patterns

### XAML resources

Use `x:Uid` when WinUI can populate a static property:

```xml
<Button x:Name="SearchButton"
        x:Uid="SearchButton" />
```

The resource key includes the property:

```xml
<data name="SearchButton.Content" xml:space="preserve">
  <value>Search</value>
  <comment>status:complete</comment>
</data>
```

### C# resources

Use the localization service for runtime strings:

```csharp
var text = LocalizationService.Instance.GetLocalizedString("SearchErrorTitle");
var formatted = LocalizationService.Instance.GetLocalizedString(
    "FoundMatchesStatus",
    matchCount,
    fileCount,
    elapsed);
```

Do not hard-code user-facing English in C# or XAML fallback paths unless an existing compatibility pattern requires it.

Regex Builder strings follow this same system. See [Regex Builder Localization](regex-localization.md) for its resource families and focused validation steps.

## Status comments

Every `<data>` entry should have one status comment.

| Comment | Meaning |
| --- | --- |
| `status:complete` | Reviewed translation |
| `status:incomplete` | English placeholder or unreviewed translation |
| `status:error:<details>` | Automatic translation failed |
| `status:error:permanent` | Do not retry automatically |

English entries use `status:complete`.

Example:

```xml
<data name="NewFeatureButton.Content" xml:space="preserve">
  <value>New feature</value>
  <comment>status:incomplete</comment>
</data>
```

## Add a resource key

English is the source of truth.

1. Add the key and English text to `Strings/en-US/Resources.resw` with `status:complete`.
2. From the repository root, propagate the same key:

   ```powershell
   python Scripts/add_localization_entry.py "NewFeatureButton.Content" "New feature"
   ```

3. The script skips the existing English key and adds an English placeholder with `status:incomplete` to other catalogs.
4. Translate and review the affected non-English entries.
5. Run script tests and the Windows .NET tests.

The add script:

- discovers `Strings/*/Resources.resw`;
- does not overwrite an existing key;
- escapes XML through `xml.etree.ElementTree`;
- reports added and skipped catalogs.

## Remove a resource key

Use the removal script rather than hand-editing 108 files:

```powershell
python Scripts/remove_localization_entry.py "OldFeatureButton.Content"
```

Then search for remaining code/XAML references:

```powershell
rg -n "OldFeatureButton" Controls ViewModels Services MainWindow.xaml MainWindow.xaml.cs
```

Review the diff before committing.

## Update existing text

When changing an English message:

1. update `en-US`;
2. identify every catalog containing the key;
3. preserve placeholders and XML whitespace;
4. mark stale non-English values `status:incomplete`;
5. translate and review;
6. run the status report.

Do not silently leave a previously translated value attached to changed English semantics.

## Formatting rules for translators

Preserve:

- format placeholders such as `{0}`, `{1}`, and `{2}`;
- literal command names, flags, paths, and file extensions;
- keyboard names;
- XML escaping;
- leading/trailing whitespace when meaningful;
- punctuation required by the UI;
- accelerator or glyph text used by controls.

Do not translate:

- `Grex`;
- API model ids;
- command names such as `grep`, `docker`, `wsl`, and `grex-cli`;
- CLI options such as `--gitignore`;
- technical units such as KB, MB, and GB;
- file or resource keys.

Keep translations concise enough for the existing control. Test long strings and right-to-left scripts in the running app.

## Automated translation

`Scripts/translate_remaining_entries.py` can process incomplete entries with the optional `googletrans` package:

```powershell
python -m pip install googletrans==4.0.0rc1
python Scripts/translate_remaining_entries.py
```

Without `googletrans`, the script identifies work but cannot translate it.

The script:

- compares non-English values with English;
- skips complete and permanent-error entries;
- maps Windows culture codes to provider language codes;
- marks successful values complete;
- records transient or permanent errors;
- avoids overwriting values already different from English.

Machine translation is a draft, not approval. Review terminology, placeholders, directionality, truncation, and feature context before changing a status to complete.

The translation provider is external. Do not place secrets or private product text in resource values submitted to it.

## Report translation status

Run:

```powershell
python Scripts/generate_translation_status.py
```

The report:

- parses the English key set;
- counts complete, incomplete, and error entries per locale;
- summarizes locale status;
- lists locales nearest completion;
- lists translation errors.

This output is authoritative. Counts in prose can become stale.

## Test localization scripts

Standard library:

```powershell
python Scripts/test_add_localization_entry.py
python Scripts/test_remove_localization_entry.py
```

With pytest:

```powershell
python -m pytest Scripts/test_add_localization_entry.py Scripts/test_remove_localization_entry.py
```

On Windows, run localization-focused .NET tests:

```powershell
dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~Localization"
dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~RegexBuilderLanguage"
dotnet test IntegrationTests/Grex.IntegrationTests.csproj -p:Platform=x64 --filter "FullyQualifiedName~AiSearchLocalization"
```

Then manually verify:

1. Settings language list;
2. Search, Regex Builder, Settings, About, and Credits;
3. tooltips;
4. dialogs and notifications;
5. result/status pluralization;
6. restart persistence;
7. fallback for an intentionally missing key.

## Line endings

All `.resw` files are CRLF. Python's XML writer emits platform-native newlines, so running mutation scripts on Linux or macOS can turn every line into LF.

Before editing on any platform:

```powershell
git config core.autocrlf false
git ls-files --eol Strings/en-US/Resources.resw
```

Expected index form:

```text
i/crlf
```

After running a script:

```powershell
git diff --stat -- Strings
git ls-files --eol Strings/en-US/Resources.resw
```

On Linux or macOS, convert changed `.resw` files back to CRLF before committing. Avoid a repository-wide line-ending diff.

## Add a language

1. Choose a valid Windows culture code, such as `xx-YY`.
2. Copy the English catalog to `Strings/xx-YY/Resources.resw`.
3. Keep the same key set and XML structure.
4. Set non-English entries to `status:incomplete`.
5. Translate and review.
6. Run the status report.
7. Build on Windows so PRI resource generation validates the culture.
8. Verify the language appears in Settings and survives restart.

`Grex.csproj` includes `Strings\**\Resources.resw` as PRI resources, so no per-language project entry is normally needed.

## Review checklist

- Every English key exists once.
- Every changed key exists in every catalog.
- Format placeholders match English.
- No user-facing text was added outside localization.
- Status comments match review state.
- `.resw` files remain CRLF.
- Script and .NET localization tests pass.
- Dynamic language switching was checked.
- Machine-translated text received human review.
