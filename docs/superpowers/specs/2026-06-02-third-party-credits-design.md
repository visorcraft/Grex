# Third-Party Credits Page — Implementation Spec

**Summary:** Add a new left-nav **Credits** page (footer, after About) that renders a curated, theme-aware, localized list of the GUI's redistributed open-source/third-party components — each with copyright, project link, and full verbatim license text — driven by a single bundled JSON manifest that also generates the root `THIRD-PARTY-NOTICES.txt`, and guarded by a drift test that fails when a resolved package is neither documented nor explicitly excluded.

---

## 1. Background / Current State

Grex is a .NET 8 + WinUI 3 (MVVM) Windows grep GUI, licensed **GPL-3.0** (`LICENSE` at the repo root; the About page shows "Licensed under GPL 3.0"). It ships a GUI (`Grex.csproj`) and a headless CLI (`Grex.Cli`).

There is **no Credits feature today**. The nearest analogue is the **About page** (`Controls/AboutView.xaml` / `.xaml.cs`):

- A scrollable, theme-aware, localized `StackPanel` inside a `ScrollViewer` (logo, app name, version, created-by line, license line, GitHub `HyperlinkButton`, keyboard-shortcut hint).
- Loads its logo from `Assets/Grex.png` via `AppContext.BaseDirectory` (`Path.Combine(AppContext.BaseDirectory, "Assets", "Grex.png")`).
- Subscribes to `MainWindow.ThemeChanged` on `Loaded`, unsubscribes on `Unloaded`, exposes `public void ApplyThemeFromHost(ThemeChangedEventArgs e)` and `public void RefreshLocalization()`, and handles the eight high-contrast themes (`IsHighContrastTheme`) by walking the visual tree to set/clear foreground and background.
- Retrieves strings via `LocalizationService.Instance.GetLocalizedString("Key")`.

**Navigation** lives in `MainWindow.xaml`'s `NavigationView`:

- `NavigationView.MenuItems` = `SearchNavItem` (Tag `Search`), `RegexBuilderNavItem` (Tag `RegexBuilder`), `SettingsNavItem` (Tag `Settings`).
- `NavigationView.FooterMenuItems` = `AboutNavItem` (Tag `About`, `x:Uid="AboutNavItem"`, glyph `&#xE946;`, with `NavigationItem_PointerEntered` / `NavigationItem_PointerExited` handlers).
- Each item has a string `Tag` and an `x:Uid` (localized via `<Tag>.Content` in `Strings/<culture>/Resources.resw`).
- `MainWindow.xaml.cs` `NavigationView_SelectionChanged` (line ~1013) is an `if/else if` ladder on `item.Tag` that toggles `Visibility` of sibling content grids: `SearchContentGrid`, `RegexBuilderContentGrid`, `SettingsContentGrid`, `AboutContentGrid`.
- Theme application enumerates each content grid/view in several lists: `ApplyThemeToElement(...)` calls (lines ~1352–1355 and ~1435–1438), the high-contrast background-brush block (`if (AboutContentGrid != null) AboutContentGrid.Background = backgroundBrush;`, line ~1447), `NotifyThemeAwareControls` → `AboutView?.ApplyThemeFromHost(args)` (line ~1946), the `ClearValue(Grid.BackgroundProperty)` block (line ~2140), and `RefreshLocalization` → `AboutView?.RefreshLocalization()` (line ~2413).

**Localization:** strings live in `Strings/<culture>/Resources.resw` across 100+ cultures. New strings are added to `Strings/en-US/Resources.resw` **first**, then propagated with `python Scripts/add_localization_entry.py "<key>" "<value>"`. Retrieved at runtime via `LocalizationService.GetLocalizedString("Key")` / `GetString`. **License *texts* are legal/verbatim and MUST NOT be localized**; only UI chrome (nav label, page heading, intro line) is localized.

The GUI's redistributed `PackageReference`s in `Grex.csproj` are:

```xml
<PackageReference Include="Docker.DotNet" Version="3.125.15" />
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250907003" />
<PackageReference Include="System.Data.OleDb" Version="8.0.0" />
```

`Microsoft.WindowsAppSDK` is a meta-package that pulls in numerous transitive runtime sub-packages, and `System.Data.OleDb` drags in additional `System.*` runtime packages — all of which are documented here.

---

## 2. Goals & Non-Goals

### Goals

1. Add a dedicated, full-page **Credits** view that lists every redistributed runtime dependency of the **GUI**, plus platform notes (.NET 8 runtime, Segoe Fluent Icons), each with copyright, project link, and **full verbatim license text**.
2. Make a single bundled **JSON manifest** the single source of truth for license data; load it at runtime exactly the way `AboutView` loads its logo (via `AppContext.BaseDirectory`).
3. Generate the root `THIRD-PARTY-NOTICES.txt` **from the same JSON** via a new Python script (never hand-maintained), for source-distribution / GPL-compliance hygiene.
4. Add a **drift test** that fails when any resolved GUI package is neither documented in the JSON nor on an explicit build-only exclusion allowlist, and that validates the JSON's internal integrity.
5. Replicate the existing About theme/localization plumbing exactly so the page works across all themes (including the eight high-contrast themes) and all 100+ cultures.

### Non-Goals

- No build-time license auto-generation/scraping (YAGNI). The JSON is curated by hand.
- No Credits UI in the **CLI** (`Grex.Cli` / `System.CommandLine` is out of scope for this feature).
- No network calls at runtime — all license text ships inside the app.
- `THIRD-PARTY-NOTICES.txt` is **generated** from the JSON, never hand-edited.
- License texts are **not** localized (legal/verbatim).

---

## 3. Approved Decisions

- **Placement:** a new **Credits** item in `NavigationView.FooterMenuItems`, immediately **after** About, with its own full-page scrollable `CreditsView`.
- **License source:** a single **curated JSON manifest** (`Assets/third-party-licenses.json`) bundled with the app as the single source of truth, plus a **drift test** that fails if any resolved package is neither documented nor explicitly excluded.
- **Scope:** redistributed **runtime** dependencies of the **GUI** + **platform notes** (.NET 8 runtime, Segoe Fluent Icons). Build-only tooling (`Microsoft.Windows.SDK.BuildTools`, `Microsoft.Windows.SDK.BuildTools.MSIX`) is explicitly **excluded** with documented reasons. The CLI is **out of scope**.
- **Generated notices file:** a root `THIRD-PARTY-NOTICES.txt` produced **from the same JSON** via a new `Scripts/generate_third_party_notices.py`.
- **Identical license texts are grouped** under one license key (e.g. a single `MIT` key) so the JSON does not duplicate the same text many times.

---

## 4. Architecture

### 4.1 Data model — `Assets/third-party-licenses.json`

Add the asset as `Content` with `CopyToOutputDirectory=PreserveNewest` in `Grex.csproj`, so it lands next to the executable and can be loaded via `AppContext.BaseDirectory` (mirroring `Assets/Grex.png`):

```xml
<!-- Grex.csproj, in the existing <ItemGroup> that holds Content assets -->
<Content Include="Assets\third-party-licenses.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

**Shape.** An object with two top-level members:

1. `licenses` — a map from a **license key** (e.g. `"MIT"`, `"Apache-2.0"`, `"BSD-3-Clause-WebView2"`, `"Microsoft-WindowsAppSDK"`, `"Microsoft-DotNet-Library"`, `"Microsoft-Segoe-Fluent-Icons"`) to the **full verbatim license text** for that key. Identical texts are stored once and referenced by key.
2. `components` — an array of objects `{ name, version, license, copyright, url, category }`, where `license` is a key into `licenses`.

```jsonc
{
  "schemaVersion": 1,
  "licenses": {
    "MIT": "The MIT License (MIT)\n\nCopyright (c) ...\n\nPermission is hereby granted, free of charge, ...",
    "Apache-2.0": "Apache License\nVersion 2.0, January 2004\n...",
    "BSD-3-Clause-WebView2": "Copyright (C) Microsoft Corporation. All rights reserved.\n\nRedistribution and use in source and binary forms, ...",
    "Microsoft-WindowsAppSDK": "MICROSOFT SOFTWARE LICENSE TERMS\nMICROSOFT WINDOWS APP SDK\n...",
    "Microsoft-DotNet-Library": "MICROSOFT SOFTWARE LICENSE TERMS\nMICROSOFT .NET LIBRARY\n...",
    "Microsoft-Segoe-Fluent-Icons": "Segoe Fluent Icons is a proprietary Microsoft font that ships with Windows 11. ..."
  },
  "components": [
    {
      "name": "Docker.DotNet",
      "version": "3.125.15",
      "license": "MIT",
      "copyright": "Copyright (c) .NET Foundation and Contributors",
      "url": "https://github.com/dotnet/Docker.DotNet",
      "category": "library"
    },
    {
      "name": "Microsoft.WindowsAppSDK",
      "version": "1.8.250907003",
      "license": "Microsoft-WindowsAppSDK",
      "copyright": "Copyright (c) Microsoft Corporation.",
      "url": "https://github.com/microsoft/WindowsAppSDK",
      "category": "library"
    }
    // ... one entry per row in §5.1 (libraries) and §5.2 (platform notes) ...
  ]
}
```

**Field semantics:**

- `name`, `version` — exact package id and version (used by the drift test to match resolved packages and to build the Expander header).
- `license` — a key that **must** exist in `licenses`.
- `copyright` — the verbatim copyright line for the component (see the §5 tables for the authoritative values).
- `url` — the project/home URL shown as a clickable link.
- `category` — `"library"` (a redistributed NuGet runtime dependency) or `"platform"` (a platform note that Grex relies on but does not itself redistribute via NuGet, e.g. the .NET 8 runtime, the Segoe Fluent Icons font). The `CreditsView` uses this to optionally group/sort; the drift test treats both as documented.

**Verbatim license text:** the implementation must transcribe each license body verbatim from the **license-text source URL** in the §5.1 / §5.2 tables. Group all MIT-licensed components under a single `MIT` key, all Apache-2.0 under one `Apache-2.0` key, and so on. The Microsoft Windows App SDK proprietary EULA — identical across the meta-package and every sub-package — is stored **once** under `Microsoft-WindowsAppSDK` and referenced by all of them.

> **Note on per-component copyright vs. shared license text:** the `copyright` lives on each **component**, while the verbatim **license body** lives once per **license key**. This is deliberate: several components share the exact MIT text but carry different copyright lines (e.g. `.NET Foundation and Contributors` vs. `James Newton-King` vs. `2015 Microsoft`), so the copyright must not be folded into the shared license body.

### 4.2 New control — `Controls/CreditsView.xaml` / `Controls/CreditsView.xaml.cs`

Modeled directly on `AboutView`. A `UserControl` named `CreditsControl`, `x:Uid="CreditsView"`, `VerticalAlignment="Stretch"`, `HorizontalAlignment="Stretch"`, whose root `Grid` (Padding `24`) hosts a vertical `ScrollViewer` (`VerticalScrollBarVisibility="Auto"`, `HorizontalScrollBarVisibility="Disabled"`).

Inside the `ScrollViewer`, a `StackPanel` (`MaxWidth="900"`, `Spacing="16"`) contains:

1. A localized **page heading** `TextBlock` (`x:Name="CreditsHeadingTextBlock"`, `x:Uid="CreditsHeadingTextBlock"`, e.g. font size 28, bold).
2. A short localized **intro line** `TextBlock` (`x:Name="CreditsIntroTextBlock"`, `x:Uid="CreditsIntroTextBlock"`, secondary foreground).
3. An `ItemsControl` (`x:Name="ComponentsItemsControl"`) — populated in code-behind — whose items each render one `Expander` per component.

**Expander per component (built in code-behind, not data-bound XAML):**

- `Header`: `"{name} v{version} — {licenseShortId}"` (e.g. `Docker.DotNet v3.125.15 — MIT`). The short id is the component's `license` key.
- Expanded `Content` (a `StackPanel`, `Spacing="8"`, `Padding` left/top a little):
  - A `TextBlock` with the `copyright` line.
  - A `HyperlinkButton` to `url` (with `PointerEntered`/`PointerExited` hand-cursor handlers copied verbatim from `AboutView`).
  - The **full verbatim license text** in a read-only, selectable, monospaced `TextBox`:

```xml
<TextBox IsReadOnly="True"
         TextWrapping="Wrap"
         AcceptsReturn="True"
         IsSpellCheckEnabled="False"
         FontFamily="Consolas"
         BorderThickness="0"
         Background="Transparent"
         MaxHeight="360"
         ScrollViewer.VerticalScrollBarVisibility="Auto"/>
```

Building the Expanders in code-behind (rather than XAML `DataTemplate` binding) keeps the high-contrast visual-tree walk identical to `AboutView` — the theme code only needs `VisualTreeHelper` traversal, which already covers dynamically created `TextBlock`/`Button`/`ContentPresenter` children.

**Code-behind responsibilities (`CreditsView.xaml.cs`):**

- Constructor: `InitializeComponent()`, `LoadLicenses()`, `RefreshLocalization()`, then wire `Loaded += CreditsView_Loaded; Unloaded += CreditsView_Unloaded;` — same order/shape as `AboutView`.
- `LoadLicenses()`: read `Path.Combine(AppContext.BaseDirectory, "Assets", "third-party-licenses.json")`, deserialize (System.Text.Json), and for each component create an `Expander` and add it to `ComponentsItemsControl.Items`. Wrap in try/catch with `System.Diagnostics.Debug.WriteLine` on failure (same defensive style as `AboutView.LoadAppLogo`). The license body for each component is `licenses[component.license]`.
- Theme plumbing — copy verbatim from `AboutView`:
  - `CreditsView_Loaded`: `MainWindow.ThemeChanged += OnThemeChanged;` then a low-priority `DispatcherQueue.TryEnqueue` calling `ApplyCurrentThemeColors()`.
  - `CreditsView_Unloaded`: `MainWindow.ThemeChanged -= OnThemeChanged;`.
  - `OnThemeChanged`, `ApplyCurrentThemeColors`, `public void ApplyThemeFromHost(ThemeChangedEventArgs e)`, `ApplyThemeColors`, `IsHighContrastTheme` (the same eight themes: `BlackKnight`, `Paranoid`, `Diamond`, `Subspace`, `RedVelvet`, `Dreams`, `Tiefling`, `Vibes`), `ApplyForegroundToAllTextBlocks`, `ClearHighContrastColors`, `ClearForegroundFromVisualTree` — identical to `AboutView`.
  - In high-contrast mode, the read-only license `TextBox` controls should also receive the foreground/background brushes; extend `ApplyForegroundToAllTextBlocks` / `ClearForegroundFromVisualTree` with a `TextBox` branch (set/clear `TextBox.ForegroundProperty` and `TextBox.BackgroundProperty`). This is the only material addition over `AboutView`'s tree walk.
- `public void RefreshLocalization()`: set `CreditsHeadingTextBlock.Text = loc.GetLocalizedString("CreditsHeadingTextBlock.Text")` and `CreditsIntroTextBlock.Text = loc.GetLocalizedString("CreditsIntroTextBlock.Text")`. **Do not** localize any license body, copyright line, component name, version, or URL.
- `HyperlinkButton_PointerEntered` / `HyperlinkButton_PointerExited`: copy verbatim from `AboutView` (the `ProtectedCursor` reflection trick for hand/arrow cursors).

### 4.3 Wiring

#### (1) `MainWindow.xaml`

Add `CreditsNavItem` to `NavigationView.FooterMenuItems`, **after** `AboutNavItem`:

```xml
<NavigationViewItem x:Name="CreditsNavItem"
                  x:Uid="CreditsNavItem"
                  Tag="Credits"
                  PointerEntered="NavigationItem_PointerEntered"
                  PointerExited="NavigationItem_PointerExited">
    <NavigationViewItem.Icon>
        <FontIcon Glyph="&#xE8A5;" FontFamily="{StaticResource SymbolThemeFontFamily}"/>
    </NavigationViewItem.Icon>
</NavigationViewItem>
```

(`&#xE8A5;` is the Segoe "Document"/list glyph, distinct from About's `&#xE946;` info glyph.)

Add a `CreditsContentGrid` (collapsed) hosting the view, placed immediately after `AboutContentGrid` inside `SplitView.Content`'s inner `Grid`:

```xml
<Grid x:Name="CreditsContentGrid" Visibility="Collapsed">
    <controls:CreditsView x:Name="CreditsView"/>
</Grid>
```

#### (2) `MainWindow.xaml.cs`

**`NavigationView_SelectionChanged` (line ~1013):** add a `Credits` branch and collapse `CreditsContentGrid` in every other branch.

- New branch:

```csharp
else if (tag == "Credits")
{
    SearchContentGrid.Visibility = Visibility.Collapsed;
    RegexBuilderContentGrid.Visibility = Visibility.Collapsed;
    SettingsContentGrid.Visibility = Visibility.Collapsed;
    AboutContentGrid.Visibility = Visibility.Collapsed;
    CreditsContentGrid.Visibility = Visibility.Visible;
    // Hide InfoBar when on Credits page
    if (StatusInfoBar != null)
    {
        StatusInfoBar.Visibility = Visibility.Collapsed;
    }
}
```

- In the existing `Search`, `RegexBuilder`, `Settings`, and `About` branches, add `CreditsContentGrid.Visibility = Visibility.Collapsed;` alongside the existing collapse lines.

**Theme application — add `CreditsContentGrid` / `CreditsView` everywhere `AboutContentGrid` / `AboutView` appears:**

- After `ApplyThemeToElement(AboutContentGrid, elementTheme, applyBackground: true);` (line ~1355): add `ApplyThemeToElement(CreditsContentGrid, elementTheme, applyBackground: true);`.
- After `ApplyThemeToElement(AboutContentGrid, elementTheme, applyBackground: false);` (line ~1438): add `ApplyThemeToElement(CreditsContentGrid, elementTheme, applyBackground: false);`.
- After `if (AboutContentGrid != null) AboutContentGrid.Background = backgroundBrush;` (line ~1447): add `if (CreditsContentGrid != null) CreditsContentGrid.Background = backgroundBrush;`.
- In `NotifyThemeAwareControls`, after `AboutView?.ApplyThemeFromHost(args);` (line ~1946): add `CreditsView?.ApplyThemeFromHost(args);` (inside the same try/catch, mirroring the existing log line).
- In the `ClearValue` block, after `AboutContentGrid?.ClearValue(Grid.BackgroundProperty);` (line ~2140): add `CreditsContentGrid?.ClearValue(Grid.BackgroundProperty);`.

**Localization — `RefreshLocalization` (line ~2349 / ~2413):** after `AboutView?.RefreshLocalization();` add `CreditsView?.RefreshLocalization();`.

#### (3) Localization

Add to `Strings/en-US/Resources.resw` **first**, then propagate with the script:

| Key | en-US value |
| --- | --- |
| `CreditsNavItem.Content` | `Credits` |
| `CreditsHeadingTextBlock.Text` | `Open-Source Licenses` |
| `CreditsIntroTextBlock.Text` | `Grex includes the following third-party components. Each is shown with its copyright, project link, and full license text.` |

```bash
python Scripts/add_localization_entry.py "CreditsNavItem.Content" "Credits"
python Scripts/add_localization_entry.py "CreditsHeadingTextBlock.Text" "Open-Source Licenses"
python Scripts/add_localization_entry.py "CreditsIntroTextBlock.Text" "Grex includes the following third-party components. Each is shown with its copyright, project link, and full license text."
```

> Only these three UI-chrome strings are localized. License bodies, copyrights, names, versions, and URLs come from the JSON and are **not** localized.

### 4.4 `Scripts/generate_third_party_notices.py`

A standalone Python 3 script (no third-party deps; standard library only) that reads `Assets/third-party-licenses.json` and writes the root `THIRD-PARTY-NOTICES.txt`. It is the only writer of that file.

Behavior:

- Resolve paths relative to the script location: repo root = parent of `Scripts/`; input = `<root>/Assets/third-party-licenses.json`; output = `<root>/THIRD-PARTY-NOTICES.txt`.
- Load the JSON, sort `components` by (`category`, `name`, `version`) for deterministic output.
- Emit a header explaining the file is generated and lists third-party components bundled with the Grex GUI, plus a pointer back to `Assets/third-party-licenses.json` as the source of truth.
- For each component, emit a section: a separator rule, `Name vVersion (license-key)`, the project URL, the copyright line, a blank line, then the verbatim license body looked up from `licenses[component.license]`.
- To avoid printing the same long license text many times, the script may print each shared license body once in a trailing "License texts" section and reference it by key in each component block; the simplest faithful implementation prints the body inline per component (acceptable since the file is generated). Either is allowed; the inline form is the default.
- Validate before writing: assert top-level `schemaVersion == 1`; every `component.license` must exist in `licenses`; required fields non-empty; exit non-zero with a clear message if not. This mirrors the drift test's JSON validation so a malformed manifest fails fast at generation time too.
- Write deterministically so the `git diff --exit-code` gate is stable across platforms/CI: open the output with explicit `encoding="utf-8"` (no BOM) and `newline="\n"`, and end the file with exactly one trailing newline. Optionally add a `THIRD-PARTY-NOTICES.txt text eol=lf` entry to `.gitattributes` to pin line endings.

---

## 5. Coverage

`category` legend: **documented** = redistributed GUI runtime dependency shown on the Credits page (`"library"` in JSON, except .NET runtime / font which are `"platform"`); **excluded** = build/test-only, not shipped, on the drift-test allowlist (not shown on the page).

### 5.1 Documented components (libraries shown on the Credits page)

| Name | Version | License | Copyright | Verbatim license source URL |
| --- | --- | --- | --- | --- |
| Docker.DotNet | 3.125.15 | MIT License | `Copyright (c) .NET Foundation and Contributors` | https://raw.githubusercontent.com/dotnet/Docker.DotNet/master/LICENSE |
| Newtonsoft.Json | 13.0.1 | MIT License | `Copyright © James Newton-King 2008` | https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/13.0.1/LICENSE.md |
| Microsoft.WindowsAppSDK | 1.8.250907003 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | `Copyright (c) Microsoft Corporation.` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK/1.8.250907003/License |
| Microsoft.WindowsAppSDK.Base | 1.8.250831001 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | `© Microsoft Corporation. All rights reserved.` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Base/1.8.250831001/License |
| Microsoft.WindowsAppSDK.Foundation | 1.8.250906002 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | *(EULA carries no copyright notice)* | https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Foundation/1.8.250906002/License |
| Microsoft.WindowsAppSDK.WinUI | 1.8.250906003 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | `Copyright (c) Microsoft Corporation.` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK.WinUI/1.8.250906003/License |
| Microsoft.WindowsAppSDK.Runtime | 1.8.250907003 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | `© Microsoft Corporation. All rights reserved.` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Runtime/1.8.250907003/License |
| Microsoft.WindowsAppSDK.DWrite | 1.8.25090401 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | `© Microsoft Corporation. All rights reserved.` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK.DWrite/1.8.25090401/License |
| Microsoft.WindowsAppSDK.InteractiveExperiences | 1.8.250906004 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | `© Microsoft Corporation. All rights reserved.` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK.InteractiveExperiences/1.8.250906004/License |
| Microsoft.WindowsAppSDK.Widgets | 1.8.250904007 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | `© Microsoft Corporation. All rights reserved.` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK.Widgets/1.8.250904007/License |
| Microsoft.WindowsAppSDK.AI | 1.8.37 | MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK (proprietary) | `© Microsoft Corporation. All rights reserved.` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK.AI/1.8.37/License |
| Microsoft.Web.WebView2 | 1.0.3179.45 | BSD 3-Clause "New" or "Revised" License | `Copyright (C) Microsoft Corporation. All rights reserved.` | https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.3179.45/License |
| System.Data.OleDb | 8.0.0 | MIT License | `© Microsoft Corporation. All rights reserved.` | https://raw.githubusercontent.com/dotnet/runtime/main/LICENSE.TXT |
| System.Configuration.ConfigurationManager | 8.0.0 | MIT License | `© Microsoft Corporation. All rights reserved.` | https://raw.githubusercontent.com/dotnet/runtime/main/LICENSE.TXT |
| System.Diagnostics.EventLog | 8.0.0 | MIT License | `© Microsoft Corporation. All rights reserved.` | https://raw.githubusercontent.com/dotnet/runtime/main/LICENSE.TXT |
| System.Diagnostics.PerformanceCounter | 8.0.0 | MIT License | `Copyright (c) .NET Foundation and Contributors` | https://raw.githubusercontent.com/dotnet/runtime/main/LICENSE.TXT |
| System.Security.Cryptography.ProtectedData | 8.0.0 | MIT License | `© Microsoft Corporation. All rights reserved.` | https://raw.githubusercontent.com/dotnet/runtime/main/LICENSE.TXT |
| System.Buffers | 4.5.1 | MIT License | `Copyright (c) .NET Foundation and Contributors` | https://raw.githubusercontent.com/dotnet/runtime/main/LICENSE.TXT |
| System.Threading.Tasks.Extensions | 4.5.4 | MIT License | `© Microsoft Corporation. All rights reserved.` | https://raw.githubusercontent.com/dotnet/runtime/main/LICENSE.TXT |

**Notes on the data above (cross-checked, adversarially reviewed):**

- **Microsoft.WindowsAppSDK and every `*.WindowsAppSDK.*` sub-package:** the upstream GitHub repo's `LICENSE` is MIT, but the redistributed **NuGet binaries** are governed by the proprietary **"MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK"** EULA. Since Grex redistributes the binaries, the proprietary terms govern (`isProprietary = true`). The EULA text is **identical** across all of them — store it **once** under the `Microsoft-WindowsAppSDK` license key. The EULA body itself carries **no copyright notice**; the per-component copyright values come from each package's NuGet metadata (`© Microsoft Corporation. All rights reserved.` for the sub-packages that declare one; the MIT source notice `Copyright (c) Microsoft Corporation.` for the meta-package / WinUI / Foundation, where Foundation declares none). GPL-3.0 redistribution of these proprietary binaries is a legal question outside this metadata task and is flagged for separate review.
- **Microsoft.Web.WebView2:** the SDK NuGet package (what Grex transitively redistributes) is **BSD-3-Clause** (verbatim BSD text with the Microsoft copyright). The separately-installed WebView2 **Runtime** browser engine is under proprietary Microsoft Edge WebView2 Runtime terms — that is a *different* component (an OS/platform prerequisite), not the redistributed SDK package, and is not listed as a bundled component here.
- **`System.*` dotnet-runtime / corefx packages:** all MIT. The license **body** is the verbatim MIT text from `dotnet/runtime` (`Copyright (c) .NET Foundation and Contributors`); the per-component **copyright** field reflects each package's own declared NuGet `<copyright>` metadata, which is `© Microsoft Corporation. All rights reserved.` for most, except `System.Buffers` and `System.Diagnostics.PerformanceCounter`, whose recorded copyright is the `.NET Foundation and Contributors` form. Group all of these under the single `MIT` license key. (`System.Buffers` and `System.Threading.Tasks.Extensions` historically shipped from the now-archived `dotnet/corefx` repo; transcribe the identical MIT body from the `dotnet/runtime` URL rather than the dead corefx `master` URL.)
- **Newtonsoft.Json:** MIT; recorded copyright is the package's declared NuGet metadata form `Copyright © James Newton-King 2008` (the LICENSE.md body itself reads `Copyright (c) 2007 James Newton-King`). Shares the `MIT` key for license body.

### 5.2 Platform notes (`category: "platform"` — shown on the page, not NuGet-redistributed)

| Name | Version | License | Copyright | Verbatim/reference license source URL |
| --- | --- | --- | --- | --- |
| .NET 8 Runtime | 8.0 (target `net8.0-windows10.0.19041.0`) | Source: MIT License. Windows binary as distributed: MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT .NET LIBRARY (proprietary) | `Copyright (c) .NET Foundation and Contributors` (source); binary distribution: Microsoft Corporation | https://dotnet.microsoft.com/en-us/dotnet_library_license.htm |
| Segoe Fluent Icons (font) | Windows 11 system font (no NuGet version) | Microsoft proprietary font license (ships with Windows; download EULA restricts redistribution) | `Copyright (c) Microsoft Corporation. All rights reserved.` | https://learn.microsoft.com/en-us/windows/apps/design/iconography/segoe-fluent-icons-font |

**Notes on platform components:**

- **.NET 8 Runtime:** the `dotnet/runtime` **source** is MIT, but the **Windows runtime binary** that a Windows-only WinUI 3 app relies on (framework-dependent or self-contained `win-x64`) is governed by the proprietary **Microsoft .NET Library** license. Record both facts: MIT for source provenance and the .NET Library license as the as-distributed Windows binary terms. Store the proprietary body under `Microsoft-DotNet-Library`. This is a **platform** note — Grex does not itself NuGet-redistribute the runtime; it is provided by the user's .NET install or a self-contained publish.
- **Segoe Fluent Icons:** a Microsoft proprietary font that **ships with Windows 11**; Grex relies on the OS-provided font and does **not** bundle it. The Microsoft download EULA restricts redistribution to other platforms. There is no standalone canonical verbatim-EULA URL (the EULA travels with the download), so the Microsoft Learn iconography page is the authoritative **reference** for this platform note; the JSON's `Microsoft-Segoe-Fluent-Icons` body should be a short, accurate platform statement summarizing that the font is OS-provided, proprietary, and not redistributed by Grex, with the reference link.

### 5.3 Explicitly excluded — build/test-only (NOT shown on the Credits page; on the drift-test allowlist)

These are referenced only by build/test tooling or test projects (`PrivateAssets=all` / test-only `PackageReference`s) and are **never** shipped in the redistributed GUI artifact. They are excluded with reasons and listed in the drift test's `KnownBuildOnlyExclusions` allowlist so the test passes.

| Name | Version | License | Reason for exclusion |
| --- | --- | --- | --- |
| Microsoft.Windows.SDK.BuildTools | 10.0.26100.4654 | MICROSOFT SOFTWARE LICENSE TERMS (Windows SDK, proprietary) | Build-time-only Windows SDK tooling (rc.exe, mc.exe, etc.), transitively pulled by the Windows App SDK; surfaces no runtime assemblies into the shipped app. |
| Microsoft.Windows.SDK.BuildTools.MSIX | 1.7.20250829.1 | MICROSOFT SOFTWARE LICENSE TERMS (Windows SDK, proprietary) | Build-time-only MSIX packaging tooling; transitive via the WindowsAppSDK/MSIX tooling chain; contributes no runtime assemblies. |
| Microsoft.NET.Test.Sdk | 18.0.1 | MIT License | Test-host SDK referenced only by test projects; not shipped. |
| xunit | 2.9.3 | Apache License 2.0 | Unit-test framework; test projects only; not shipped. |
| xunit.runner.visualstudio | 3.1.5 | Apache License 2.0 | Test runner (`PrivateAssets=all`); test projects only; not shipped. |
| coverlet.collector | 6.0.4 | MIT License | Code-coverage collector (`PrivateAssets=all`, development dependency); test projects only; not shipped. |
| Moq | 4.20.72 | BSD 3-Clause License | Mocking library; test projects only; not shipped. |
| FluentAssertions | 8.8.0 | Xceed Community License (Fluent Assertions, Non-Commercial Use); commercial use requires a paid Xceed license | Assertion library; test projects only; not shipped. **Note:** v8 changed from Apache-2.0 to a proprietary Xceed Community License — flagged for compliance awareness even though it is test-only. |
| Microsoft.Xaml.Behaviors.Wpf | 1.1.77 | MIT License | WPF behaviors used only by `UITests/`; not referenced by the shipping GUI/CLI; not shipped. |

> The CLI runtime dependency **System.CommandLine** (`2.0.0-beta4.22272.1`, MIT) is **out of scope** for this GUI feature. The drift test reads the **GUI** `obj/project.assets.json` only, so the CLI package never appears in its package set and does not need a documented entry or an exclusion.

---

## 6. Drift Test

New xUnit test class **`Tests/Controls/CreditsLicenseCoverageTests.cs`** (in `Grex.Tests`, which already references `..\Grex.csproj`). Robust repo-root resolution: walk **up** from the test assembly location (`AppContext.BaseDirectory`) until a directory containing both `Grex.csproj` and `Assets/third-party-licenses.json` is found (this is the repo root). The GUI's restored assets file is at `<repoRoot>/obj/project.assets.json`.

**Responsibilities:**

1. **Collect resolved packages.** Read `<repoRoot>/obj/project.assets.json` (the **GUI** project's restore output). Parse the `libraries` object and collect every entry whose `"type"` is `"package"`; the package id is the part of the key before the `/` (e.g. `"System.Buffers/4.5.1"` → `System.Buffers`). `project.assets.json` only exists after a restore — present in CI after `dotnet build`/`dotnet restore`. If the file is missing, **skip** the package-coverage assertion with a clear `Skip` reason ("project.assets.json not found — run a restore/build first"); the JSON-validation assertions (below) still run unconditionally.

2. **Assert coverage.** Load `<repoRoot>/Assets/third-party-licenses.json`. Build the documented set = `components[].name`. Build the exclusion allowlist = a hardcoded `KnownBuildOnlyExclusions` string set containing exactly the §5.3 package ids. Assert that **every** resolved package id is either in the documented set **or** in `KnownBuildOnlyExclusions`. On failure, the message must name the offending package(s) and instruct the maintainer:

   > "Package '{id}' is resolved by the GUI build but is neither documented in Assets/third-party-licenses.json nor listed in the build-only exclusion allowlist. Add it to the JSON (with verbatim license text) or, if it is build/test-only and not redistributed, add it to KnownBuildOnlyExclusions with a documented reason."

3. **Validate the JSON itself** (runs even when `project.assets.json` is absent):
   - Deserializes successfully, and top-level `schemaVersion == 1`.
   - Has non-empty `licenses` and `components`.
   - Every `component.license` references an **existing** key in `licenses`.
   - Every component's `name`, `version`, `license`, `url`, and `category` are non-empty; `category` is one of `library` / `platform`. (`copyright` may be empty for components whose proprietary EULA carries no copyright notice — e.g. `Microsoft.WindowsAppSDK.Foundation`.)
   - Every license **text** in `licenses` is non-empty.

**Sketch:**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Grex.Tests.Controls
{
    public class CreditsLicenseCoverageTests
    {
        private static readonly HashSet<string> KnownBuildOnlyExclusions = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.Windows.SDK.BuildTools",
            "Microsoft.Windows.SDK.BuildTools.MSIX",
            "Microsoft.NET.Test.Sdk",
            "xunit",
            "xunit.runner.visualstudio",
            "coverlet.collector",
            "Moq",
            "FluentAssertions",
            "Microsoft.Xaml.Behaviors.Wpf",
        };

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Grex.csproj")) &&
                    File.Exists(Path.Combine(dir.FullName, "Assets", "third-party-licenses.json")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "Could not locate repo root (expected Grex.csproj + Assets/third-party-licenses.json by walking up from the test assembly).");
        }

        private static JsonElement LoadManifest(string repoRoot)
        {
            var path = Path.Combine(repoRoot, "Assets", "third-party-licenses.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.Clone();
        }

        [Fact]
        public void Manifest_IsInternallyValid()
        {
            var root = LoadManifest(FindRepoRoot());
            var licenses = root.GetProperty("licenses");
            var components = root.GetProperty("components");

            licenses.EnumerateObject().Any().Should().BeTrue("licenses map must not be empty");
            components.GetArrayLength().Should().BeGreaterThan(0, "components array must not be empty");

            foreach (var lic in licenses.EnumerateObject())
            {
                lic.Value.GetString().Should().NotBeNullOrWhiteSpace(
                    $"license text for key '{lic.Name}' must be non-empty");
            }

            foreach (var c in components.EnumerateArray())
            {
                c.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
                c.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
                c.GetProperty("url").GetString().Should().NotBeNullOrWhiteSpace();
                var category = c.GetProperty("category").GetString();
                category.Should().BeOneOf("library", "platform");

                var key = c.GetProperty("license").GetString();
                key.Should().NotBeNullOrWhiteSpace();
                licenses.TryGetProperty(key!, out _).Should().BeTrue(
                    $"component '{c.GetProperty("name").GetString()}' references license key '{key}' which must exist in licenses");
            }
        }

        [Fact]
        public void EveryResolvedPackage_IsDocumentedOrExcluded()
        {
            var repoRoot = FindRepoRoot();
            var assets = Path.Combine(repoRoot, "obj", "project.assets.json");
            Assert.SkipUnless(File.Exists(assets),
                "project.assets.json not found — run dotnet build grex.sln -p:Platform=x64 first.");

            using var doc = JsonDocument.Parse(File.ReadAllText(assets));
            var resolved = doc.RootElement.GetProperty("libraries").EnumerateObject()
                .Where(p => p.Value.TryGetProperty("type", out var t) && t.GetString() == "package")
                .Select(p => p.Name.Split('/')[0])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var root = LoadManifest(repoRoot);
            var documented = root.GetProperty("components").EnumerateArray()
                .Select(c => c.GetProperty("name").GetString()!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var undocumented = resolved
                .Where(id => !documented.Contains(id) && !KnownBuildOnlyExclusions.Contains(id))
                .OrderBy(id => id)
                .ToList();

            undocumented.Should().BeEmpty(
                "every package the GUI build resolves must be documented in Assets/third-party-licenses.json " +
                "or listed in KnownBuildOnlyExclusions. Undocumented: " + string.Join(", ", undocumented) +
                ". Add each to the JSON with verbatim license text, or (if build/test-only and not redistributed) " +
                "to KnownBuildOnlyExclusions with a documented reason.");
        }
    }
}
```

> `Assert.SkipUnless(condition, reason)` is the built-in dynamic-skip API in xUnit **2.9.x** (the version `Grex.Tests` already references), so no extra package is needed — do **not** use the third-party `Xunit.SkippableFact` / `Skip.IfNot`. `Manifest_IsInternallyValid` is a plain `[Fact]` with no skip and always runs.

**Localization test — `Tests/Controls/CreditsViewLocalizationTests.cs`**, mirroring `Tests/Controls/AboutViewLocalizationTests.cs` (same `_reswPath` resolution: walk five `..` up from `AppContext.BaseDirectory` to the repo root, then `Strings/en-US/Resources.resw`; same `CheckResourceKeyExists` / `GetResourceValue` helpers). Assert that:

- `CreditsNavItem.Content` exists and equals/contains `Credits`.
- `CreditsHeadingTextBlock.Text` exists and contains `Licenses`.
- `CreditsIntroTextBlock.Text` exists and is non-empty.

Optionally add a small cross-culture check: enumerate `Strings/*/Resources.resw` and assert each contains `CreditsNavItem.Content` (confirming the propagation script ran across all cultures), matching how the repo verifies localized keys exist everywhere.

> **Repo-root resolution differs intentionally between the two new test files.** `CreditsViewLocalizationTests` reuses `AboutViewLocalizationTests`' exact five-segment `..` resolution verbatim; `CreditsLicenseCoverageTests` instead uses the more robust `FindRepoRoot` walk because it must also locate `obj/project.assets.json`. Do not unify them.

---

## 7. Files

### Added

- `Controls/CreditsView.xaml`
- `Controls/CreditsView.xaml.cs`
- `Assets/third-party-licenses.json`
- `Scripts/generate_third_party_notices.py`
- `THIRD-PARTY-NOTICES.txt` *(generated from the JSON by the script — committed, never hand-edited)*
- `Tests/Controls/CreditsLicenseCoverageTests.cs`
- `Tests/Controls/CreditsViewLocalizationTests.cs`

### Edited

- `MainWindow.xaml` — add `CreditsNavItem` (footer, after About) and `CreditsContentGrid` hosting `<controls:CreditsView x:Name="CreditsView"/>`.
- `MainWindow.xaml.cs` — add the `Credits` branch to `NavigationView_SelectionChanged`, collapse `CreditsContentGrid` in the other branches, and add `CreditsContentGrid` / `CreditsView` to every theme/localization list that currently references `AboutContentGrid` / `AboutView` (ApplyThemeToElement at ~1355 and ~1438, background-brush block at ~1447, `NotifyThemeAwareControls` at ~1946, `ClearValue` block at ~2140, `RefreshLocalization` at ~2413).
- `Grex.csproj` — add `Assets\third-party-licenses.json` as `Content` with `CopyToOutputDirectory=PreserveNewest`.
- `Strings/**/Resources.resw` — add `CreditsNavItem.Content`, `CreditsHeadingTextBlock.Text`, `CreditsIntroTextBlock.Text` to `en-US` first, then propagate to all cultures via `python Scripts/add_localization_entry.py`.

---

## 8. Testing

**Automated (per AGENTS.md):**

```powershell
# Build (x64) — "Any CPU" will fail for WinUI 3
dotnet build grex.sln -p:Platform=x64

# Run the unit tests (drift + JSON validation + localization)
dotnet test Tests/Grex.Tests.csproj -p:Platform=x64

# Run only the new coverage test by name
dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~CreditsLicenseCoverageTests"
```

The drift test depends on `obj/project.assets.json`, which exists after the build/restore (so the standard CI sequence of build-then-test satisfies it). The `Manifest_IsInternallyValid` and localization tests run even without a restore.

**Notices generation (verify the generated file matches the JSON):**

```bash
python Scripts/generate_third_party_notices.py
git diff --exit-code THIRD-PARTY-NOTICES.txt   # should be clean after regeneration
```

**Manual verification:**

1. `dotnet build grex.sln -p:Platform=x64` then `dotnet run --project Grex.csproj -p:Platform=x64`.
2. Click the new **Credits** footer item; confirm the page heading and intro line render localized, and that one Expander appears per documented component with the correct `Name vVersion — license-key` header.
3. Expand several entries; confirm the copyright line, working project `HyperlinkButton`, and the full verbatim license text in a read-only, selectable, monospaced `TextBox` (wraps, scrolls, not spell-checked).
4. Switch themes (light, dark, and at least one high-contrast theme such as BlackKnight) and confirm foreground/background apply to the page **and** to the license `TextBox` controls, and that switching back to a non-high-contrast theme clears the overrides.
5. Switch the app language (a couple of cultures) and confirm the nav label, heading, and intro localize while names, versions, copyrights, URLs, and license bodies stay verbatim.
6. Navigate away and back (Search → Credits → About → Credits) and confirm visibility toggling and the InfoBar hiding behave like the other footer pages.

---

## 9. Out of Scope (YAGNI)

- No build-time license auto-generation/scraping — the JSON is curated by hand.
- No Credits UI in the CLI (`Grex.Cli` / `System.CommandLine` excluded from this feature).
- No runtime network calls — all license text ships in the app.
- `THIRD-PARTY-NOTICES.txt` is generated from the JSON, not hand-maintained.
- The WebView2 **Runtime** browser engine and other OS prerequisites are not enumerated as bundled components (only the redistributed WebView2 **SDK** package and the two platform notes appear).
