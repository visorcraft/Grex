# Third-Party Credits Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a new left-nav **Credits** page to the Grex WinUI 3 GUI that lists every redistributed third-party component with its copyright, project link, and full verbatim license text, driven by a single bundled JSON manifest, with a generated `THIRD-PARTY-NOTICES.txt` and a drift test that fails when a resolved package is undocumented.

**Architecture:** A curated `Assets/third-party-licenses.json` (a `licenses` map of verbatim license texts keyed by license id + a `components` array referencing them) is the single source of truth. A new theme-aware, localized `CreditsView` user control (modeled on the existing `AboutView`) loads the JSON at runtime and renders one `Expander` per component. A Python script regenerates the root `THIRD-PARTY-NOTICES.txt` from the same JSON. An xUnit drift test reads the GUI's `obj/project.assets.json` and fails if any resolved package is neither documented in the JSON nor on a build-only exclusion allowlist.

**Tech Stack:** .NET 8, WinUI 3 (`Microsoft.WindowsAppSDK`), C#, `System.Text.Json`, xUnit + FluentAssertions, Python 3.12, WinUI `NavigationView` MVVM shell.

**Reference spec:** `docs/superpowers/specs/2026-06-02-third-party-credits-design.md` (read §5 Coverage for the authoritative component/license data).

**Platform note:** WinUI 3 builds require a concrete platform — always pass `-p:Platform=x64`. "Any CPU" fails. All `dotnet` commands below assume Windows (the GUI is `net8.0-windows10.0.19041.0`). The two Python tasks (notices generator) are cross-platform.

---

## File Structure

**Created:**
- `Assets/third-party-licenses.json` — single source of truth: `licenses` map (verbatim texts) + `components` array.
- `Controls/CreditsView.xaml` — the page: heading, intro, and an `ItemsControl` host for component expanders.
- `Controls/CreditsView.xaml.cs` — loads the JSON, builds expanders, mirrors `AboutView` theme + localization plumbing.
- `Scripts/generate_third_party_notices.py` — sole writer of `THIRD-PARTY-NOTICES.txt`; validates + renders deterministically.
- `Scripts/test_generate_third_party_notices.py` — unit tests for the generator (validation + deterministic render).
- `THIRD-PARTY-NOTICES.txt` — generated artifact (committed, never hand-edited).
- `Tests/Controls/CreditsLicenseCoverageTests.cs` — drift test + JSON-integrity test.
- `Tests/Controls/CreditsViewLocalizationTests.cs` — localization-key test (mirrors `AboutViewLocalizationTests`).

**Modified:**
- `Grex.csproj` — add the JSON asset as `Content` (`CopyToOutputDirectory=PreserveNewest`).
- `MainWindow.xaml` — add `CreditsNavItem` (footer, after About) and `CreditsContentGrid` hosting `CreditsView`.
- `MainWindow.xaml.cs` — `Credits` branch in `NavigationView_SelectionChanged`; add `CreditsContentGrid`/`CreditsView` to the theme + localization lists alongside `AboutContentGrid`/`AboutView`.
- `Strings/en-US/Resources.resw` (then all cultures via script) — 3 new keys.

**Task order (each ends in a commit):**
1. License data manifest + drift/integrity test (test-first).
2. Notices generator + tests + generated `THIRD-PARTY-NOTICES.txt`.
3. Localization strings + localization test.
4. `CreditsView` control + structure test.
5. `MainWindow` wiring (nav, theme, localization).
6. Full verification + finish branch.

---

## Task 1: License data manifest + drift/integrity test

Write the test first (it fails because the JSON does not exist), then author the JSON to satisfy it.

**Files:**
- Create: `Tests/Controls/CreditsLicenseCoverageTests.cs`
- Create: `Assets/third-party-licenses.json`
- Modify: `Grex.csproj` (add the `Content` include)

- [ ] **Step 1: Write the failing test**

Create `Tests/Controls/CreditsLicenseCoverageTests.cs`:

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
    /// <summary>
    /// Guards the third-party license manifest: every package the GUI resolves must be
    /// documented in Assets/third-party-licenses.json or explicitly excluded, and the
    /// manifest itself must be internally consistent.
    /// </summary>
    public class CreditsLicenseCoverageTests
    {
        // Build/test-only packages that are never shipped in the redistributed GUI artifact.
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

            root.GetProperty("schemaVersion").GetInt32().Should().Be(1, "schemaVersion must be 1");

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

> `Assert.SkipUnless(condition, reason)` is built into xUnit 2.9.x (already referenced by `Grex.Tests`). Do **not** add `Xunit.SkippableFact`.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~CreditsLicenseCoverageTests"`
Expected: FAIL — `FindRepoRoot` throws `DirectoryNotFoundException` because `Assets/third-party-licenses.json` does not exist yet.

- [ ] **Step 3: Create the manifest with all component metadata and the inline license bodies**

Create `Assets/third-party-licenses.json`. Fill the `components` array exactly as below (21 entries — 19 `library` + 2 `platform`). For `licenses`, the three OSI bodies below (`MIT`, `MIT-Newtonsoft`, plus the authored `Microsoft-Segoe-Fluent-Icons` note) are provided in full; the three remaining keys (`BSD-3-Clause-WebView2`, `Microsoft-WindowsAppSDK`, `Microsoft-DotNet-Library`) are transcribed in Step 4.

JSON forbids literal newlines inside strings — encode every line break in a license body as `\n`.

```jsonc
{
  "schemaVersion": 1,
  "licenses": {
    "MIT": "The MIT License (MIT)\n\nCopyright (c) .NET Foundation and Contributors\n\nAll rights reserved.\n\nPermission is hereby granted, free of charge, to any person obtaining a copy\nof this software and associated documentation files (the \"Software\"), to deal\nin the Software without restriction, including without limitation the rights\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\ncopies of the Software, and to permit persons to whom the Software is\nfurnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all\ncopies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\nSOFTWARE.",
    "MIT-Newtonsoft": "The MIT License (MIT)\n\nCopyright (c) 2007 James Newton-King\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of\nthis software and associated documentation files (the \"Software\"), to deal in\nthe Software without restriction, including without limitation the rights to\nuse, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of\nthe Software, and to permit persons to whom the Software is furnished to do so,\nsubject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all\ncopies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS\nFOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR\nCOPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER\nIN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN\nCONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
    "Microsoft-Segoe-Fluent-Icons": "Segoe Fluent Icons\n\nSegoe Fluent Icons is a proprietary Microsoft icon font that ships with\nWindows 11. Grex uses the operating-system-provided font for navigation and UI\nglyphs and does NOT bundle or redistribute the font file. The font is licensed\nas part of Windows; the Microsoft download/redistribution terms restrict use of\nthe font on other platforms. See the Microsoft reference for details:\nhttps://learn.microsoft.com/windows/apps/design/iconography/segoe-fluent-icons-font\n\nCopyright (c) Microsoft Corporation. All rights reserved.",
    "BSD-3-Clause-WebView2": "TRANSCRIBE IN STEP 4",
    "Microsoft-WindowsAppSDK": "TRANSCRIBE IN STEP 4",
    "Microsoft-DotNet-Library": "TRANSCRIBE IN STEP 4"
  },
  "components": [
    { "name": "Docker.DotNet", "version": "3.125.15", "license": "MIT", "copyright": "Copyright (c) .NET Foundation and Contributors", "url": "https://github.com/dotnet/Docker.DotNet", "category": "library" },
    { "name": "Newtonsoft.Json", "version": "13.0.1", "license": "MIT-Newtonsoft", "copyright": "Copyright © James Newton-King 2008", "url": "https://www.newtonsoft.com/json", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK", "version": "1.8.250907003", "license": "Microsoft-WindowsAppSDK", "copyright": "Copyright (c) Microsoft Corporation.", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK.Base", "version": "1.8.250831001", "license": "Microsoft-WindowsAppSDK", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK.Foundation", "version": "1.8.250906002", "license": "Microsoft-WindowsAppSDK", "copyright": "", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK.WinUI", "version": "1.8.250906003", "license": "Microsoft-WindowsAppSDK", "copyright": "Copyright (c) Microsoft Corporation.", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK.Runtime", "version": "1.8.250907003", "license": "Microsoft-WindowsAppSDK", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK.DWrite", "version": "1.8.25090401", "license": "Microsoft-WindowsAppSDK", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK.InteractiveExperiences", "version": "1.8.250906004", "license": "Microsoft-WindowsAppSDK", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK.Widgets", "version": "1.8.250904007", "license": "Microsoft-WindowsAppSDK", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.WindowsAppSDK.AI", "version": "1.8.37", "license": "Microsoft-WindowsAppSDK", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/microsoft/WindowsAppSDK", "category": "library" },
    { "name": "Microsoft.Web.WebView2", "version": "1.0.3179.45", "license": "BSD-3-Clause-WebView2", "copyright": "Copyright (C) Microsoft Corporation. All rights reserved.", "url": "https://learn.microsoft.com/microsoft-edge/webview2/", "category": "library" },
    { "name": "System.Data.OleDb", "version": "8.0.0", "license": "MIT", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/dotnet/runtime", "category": "library" },
    { "name": "System.Configuration.ConfigurationManager", "version": "8.0.0", "license": "MIT", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/dotnet/runtime", "category": "library" },
    { "name": "System.Diagnostics.EventLog", "version": "8.0.0", "license": "MIT", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/dotnet/runtime", "category": "library" },
    { "name": "System.Diagnostics.PerformanceCounter", "version": "8.0.0", "license": "MIT", "copyright": "Copyright (c) .NET Foundation and Contributors", "url": "https://github.com/dotnet/runtime", "category": "library" },
    { "name": "System.Security.Cryptography.ProtectedData", "version": "8.0.0", "license": "MIT", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/dotnet/runtime", "category": "library" },
    { "name": "System.Buffers", "version": "4.5.1", "license": "MIT", "copyright": "Copyright (c) .NET Foundation and Contributors", "url": "https://github.com/dotnet/runtime", "category": "library" },
    { "name": "System.Threading.Tasks.Extensions", "version": "4.5.4", "license": "MIT", "copyright": "© Microsoft Corporation. All rights reserved.", "url": "https://github.com/dotnet/runtime", "category": "library" },
    { "name": ".NET 8 Runtime", "version": "8.0", "license": "Microsoft-DotNet-Library", "copyright": "Copyright (c) .NET Foundation and Contributors", "url": "https://dotnet.microsoft.com/", "category": "platform" },
    { "name": "Segoe Fluent Icons", "version": "Windows 11 system font", "license": "Microsoft-Segoe-Fluent-Icons", "copyright": "Copyright (c) Microsoft Corporation. All rights reserved.", "url": "https://learn.microsoft.com/windows/apps/design/iconography/segoe-fluent-icons-font", "category": "platform" }
  ]
}
```

> **Why two MIT keys.** Docker.DotNet and all `System.*` packages share the verbatim `dotnet/runtime` MIT text (whose embedded notice is "Copyright (c) .NET Foundation and Contributors"). Newtonsoft.Json ships its *own* MIT text whose embedded notice is "Copyright (c) 2007 James Newton-King" — folding it under the shared key would display a contradictory copyright, so it gets its own `MIT-Newtonsoft` key. The per-component `copyright` field (shown above the license body in the UI) is each package's NuGet-declared copyright and may legitimately differ in year/format from the license body's embedded notice.

- [ ] **Step 4: Transcribe the three remaining license bodies verbatim from their authoritative URLs**

Replace each `"TRANSCRIBE IN STEP 4"` value with the verbatim text fetched from the exact URL below. Encode line breaks as `\n` and escape any `"` as `\"`. Do not edit, summarize, or reword the legal text.

| License key | Fetch verbatim from |
| --- | --- |
| `BSD-3-Clause-WebView2` | https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.3179.45/License |
| `Microsoft-WindowsAppSDK` | https://www.nuget.org/packages/Microsoft.WindowsAppSDK/1.8.250907003/License |
| `Microsoft-DotNet-Library` | https://dotnet.microsoft.com/en-us/dotnet_library_license.htm |

The `Microsoft-WindowsAppSDK` EULA is identical across the meta-package and every `*.WindowsAppSDK.*` sub-package, so it is stored once and referenced by all of them. After pasting, confirm no `"TRANSCRIBE IN STEP 4"` value remains (the `Manifest_IsInternallyValid` test only checks non-empty, so do a manual grep too):

Run: `grep -c "TRANSCRIBE IN STEP 4" Assets/third-party-licenses.json`
Expected: `0`

- [ ] **Step 5: Add the asset to `Grex.csproj` as copied Content**

In `Grex.csproj`, locate the existing `<ItemGroup>` that includes `Assets\` content (it already copies `Assets\Grex.png`). Add:

```xml
<Content Include="Assets\third-party-licenses.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

If no such Content `<ItemGroup>` exists, add a new one inside the top-level `<Project>` element:

```xml
<ItemGroup>
  <Content Include="Assets\third-party-licenses.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet build grex.sln -p:Platform=x64`
Then: `dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~CreditsLicenseCoverageTests"`
Expected: PASS — both `Manifest_IsInternallyValid` and `EveryResolvedPackage_IsDocumentedOrExcluded` pass. The coverage test confirms all 21 GUI-resolved packages are covered (19 documented libraries + the 2 `Microsoft.Windows.SDK.BuildTools*` packages on the exclusion allowlist).

- [ ] **Step 7: Commit**

```bash
git add Assets/third-party-licenses.json Tests/Controls/CreditsLicenseCoverageTests.cs Grex.csproj
git commit -m "Add third-party license manifest and coverage drift test"
```

---

## Task 2: Notices generator + tests + generated `THIRD-PARTY-NOTICES.txt`

**Files:**
- Create: `Scripts/generate_third_party_notices.py`
- Create: `Scripts/test_generate_third_party_notices.py`
- Create: `THIRD-PARTY-NOTICES.txt` (generated)

- [ ] **Step 1: Write the failing test**

Create `Scripts/test_generate_third_party_notices.py`:

```python
#!/usr/bin/env python3
"""Tests for generate_third_party_notices.py.

Run from the repo root: python Scripts/test_generate_third_party_notices.py
(Running the file directly puts Scripts/ on sys.path so the import resolves.)
"""
import unittest

import generate_third_party_notices as gen


def good_manifest():
    return {
        "schemaVersion": 1,
        "licenses": {"MIT": "The MIT License\n\nfull license body text\n"},
        "components": [
            {
                "name": "Sample.Lib",
                "version": "1.0.0",
                "license": "MIT",
                "copyright": "Copyright (c) Example",
                "url": "https://example.com",
                "category": "library",
            },
        ],
    }


class ValidateTests(unittest.TestCase):
    def test_accepts_good_manifest(self):
        self.assertEqual(gen.validate(good_manifest()), [])

    def test_rejects_bad_schema_version(self):
        m = good_manifest()
        m["schemaVersion"] = 2
        self.assertTrue(any("schemaVersion" in e for e in gen.validate(m)))

    def test_rejects_unknown_license_key(self):
        m = good_manifest()
        m["components"][0]["license"] = "NOPE"
        self.assertTrue(any("unknown license key" in e for e in gen.validate(m)))

    def test_rejects_empty_license_text(self):
        m = good_manifest()
        m["licenses"]["MIT"] = "   "
        self.assertTrue(any("empty" in e for e in gen.validate(m)))

    def test_rejects_missing_required_field(self):
        m = good_manifest()
        m["components"][0]["url"] = ""
        self.assertTrue(any("url" in e for e in gen.validate(m)))


class RenderTests(unittest.TestCase):
    def test_render_is_deterministic_lf_single_trailing_newline(self):
        m = good_manifest()
        out1 = gen.render(m)
        out2 = gen.render(m)
        self.assertEqual(out1, out2)
        self.assertTrue(out1.endswith("\n"))
        self.assertFalse(out1.endswith("\n\n"))
        self.assertNotIn("\r", out1)
        self.assertIn("Sample.Lib v1.0.0 (MIT)", out1)
        self.assertIn("full license body text", out1)
        self.assertIn("Copyright (c) Example", out1)


class RealManifestTests(unittest.TestCase):
    def test_real_manifest_valid_and_renders(self):
        mpath = gen.manifest_path()
        if not mpath.exists():
            self.skipTest("manifest not found")
        manifest = gen.load_manifest(mpath)
        self.assertEqual(gen.validate(manifest), [])
        out = gen.render(manifest)
        self.assertTrue(out.endswith("\n"))
        self.assertNotIn("\r", out)


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `python Scripts/test_generate_third_party_notices.py`
Expected: FAIL — `ModuleNotFoundError: No module named 'generate_third_party_notices'` (the generator does not exist yet).

- [ ] **Step 3: Write the generator**

Create `Scripts/generate_third_party_notices.py`:

```python
#!/usr/bin/env python3
"""Generate THIRD-PARTY-NOTICES.txt from Assets/third-party-licenses.json.

This script is the ONLY writer of THIRD-PARTY-NOTICES.txt. Do not hand-edit that
file; edit the JSON manifest (the single source of truth) and re-run this script.

Usage (from anywhere):
    python Scripts/generate_third_party_notices.py
"""
import json
import sys
from pathlib import Path

SCHEMA_VERSION = 1
REQUIRED_FIELDS = ("name", "version", "license", "url", "category")
VALID_CATEGORIES = ("library", "platform")


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def manifest_path() -> Path:
    return repo_root() / "Assets" / "third-party-licenses.json"


def output_path() -> Path:
    return repo_root() / "THIRD-PARTY-NOTICES.txt"


def load_manifest(path: Path) -> dict:
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def validate(manifest: dict) -> list:
    errors = []
    if manifest.get("schemaVersion") != SCHEMA_VERSION:
        errors.append(f"schemaVersion must be {SCHEMA_VERSION}, got {manifest.get('schemaVersion')!r}")

    licenses = manifest.get("licenses") or {}
    components = manifest.get("components") or []

    if not licenses:
        errors.append("licenses map is empty")
    if not components:
        errors.append("components array is empty")

    for key, text in licenses.items():
        if not (isinstance(text, str) and text.strip()):
            errors.append(f"license text for key '{key}' is empty")

    for c in components:
        name = c.get("name", "?")
        for field in REQUIRED_FIELDS:
            value = c.get(field)
            if not (isinstance(value, str) and value.strip()):
                errors.append(f"component {name!r} missing required field '{field}'")
        if c.get("category") not in VALID_CATEGORIES:
            errors.append(f"component {name!r} has invalid category {c.get('category')!r}")
        if c.get("license") not in licenses:
            errors.append(f"component {name!r} references unknown license key {c.get('license')!r}")

    return errors


def render(manifest: dict) -> str:
    components = sorted(
        manifest["components"],
        key=lambda c: (c["category"], c["name"].lower(), c["version"]),
    )
    licenses = manifest["licenses"]
    rule = "=" * 72
    sub = "-" * 72
    lines = [
        "THIRD-PARTY NOTICES",
        rule,
        "",
        "This file is generated from Assets/third-party-licenses.json by",
        "Scripts/generate_third_party_notices.py. Do not edit it by hand.",
        "",
        "Grex bundles or relies on the following third-party components.",
        "",
    ]
    for c in components:
        lines.append(sub)
        lines.append(f"{c['name']} v{c['version']} ({c['license']})")
        lines.append(c["url"])
        if c.get("copyright", "").strip():
            lines.append(c["copyright"])
        lines.append("")
        lines.append(licenses[c["license"]].rstrip("\n"))
        lines.append("")
    return "\n".join(lines).rstrip("\n") + "\n"


def main() -> int:
    mpath = manifest_path()
    if not mpath.exists():
        print(f"ERROR: manifest not found: {mpath}", file=sys.stderr)
        return 1

    manifest = load_manifest(mpath)
    errors = validate(manifest)
    if errors:
        print("ERROR: invalid manifest:", file=sys.stderr)
        for e in errors:
            print(f"  - {e}", file=sys.stderr)
        return 1

    text = render(manifest)
    # newline="\n" + single trailing newline keeps output byte-stable across platforms
    # so the `git diff --exit-code` cleanliness gate is reliable.
    with open(output_path(), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)

    print(f"Wrote {output_path()} ({len(manifest['components'])} components)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `python Scripts/test_generate_third_party_notices.py`
Expected: PASS (`OK` — all tests; `RealManifestTests` passes against the real manifest from Task 1).

- [ ] **Step 5: Generate the notices file**

Run: `python Scripts/generate_third_party_notices.py`
Expected: `Wrote .../THIRD-PARTY-NOTICES.txt (21 components)` and `THIRD-PARTY-NOTICES.txt` appears at the repo root.

- [ ] **Step 6: Pin line endings (so the diff-clean gate is stable)**

If a `.gitattributes` exists, add this line; otherwise create `.gitattributes` with it:

```
THIRD-PARTY-NOTICES.txt text eol=lf
```

- [ ] **Step 7: Verify regeneration is idempotent**

Run: `python Scripts/generate_third_party_notices.py && git add THIRD-PARTY-NOTICES.txt && git diff --cached --quiet THIRD-PARTY-NOTICES.txt; echo "clean=$?"`
Expected: after a second generate, re-staging produces no change (`git status` shows the file staged once, and a follow-up generate leaves it byte-identical).

- [ ] **Step 8: Commit**

```bash
git add Scripts/generate_third_party_notices.py Scripts/test_generate_third_party_notices.py THIRD-PARTY-NOTICES.txt .gitattributes
git commit -m "Generate THIRD-PARTY-NOTICES.txt from license manifest"
```

---

## Task 3: Localization strings + localization test

**Files:**
- Modify: `Strings/en-US/Resources.resw` (then all cultures via script)
- Create: `Tests/Controls/CreditsViewLocalizationTests.cs`

- [ ] **Step 1: Write the failing localization test**

Create `Tests/Controls/CreditsViewLocalizationTests.cs` (mirrors `AboutViewLocalizationTests` — same five-segment `..` repo-root resolution and the same `CheckResourceKeyExists`/`GetResourceValue` helpers, verbatim):

```csharp
using System;
using System.IO;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace Grex.Tests.Controls
{
    /// <summary>
    /// Tests for Credits page localization keys.
    /// </summary>
    public class CreditsViewLocalizationTests
    {
        private readonly string _reswPath;

        public CreditsViewLocalizationTests()
        {
            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            _reswPath = Path.Combine(projectRoot, "Strings", "en-US", "Resources.resw");
        }

        [Fact]
        public void Resources_ShouldContain_CreditsNavItemContent()
        {
            CheckResourceKeyExists("CreditsNavItem.Content")
                .Should().BeTrue("CreditsNavItem.Content should exist in Resources.resw");
        }

        [Fact]
        public void Resources_ShouldContain_CreditsHeadingText()
        {
            CheckResourceKeyExists("CreditsHeadingTextBlock.Text")
                .Should().BeTrue("CreditsHeadingTextBlock.Text should exist in Resources.resw");
        }

        [Fact]
        public void Resources_ShouldContain_CreditsIntroText()
        {
            CheckResourceKeyExists("CreditsIntroTextBlock.Text")
                .Should().BeTrue("CreditsIntroTextBlock.Text should exist in Resources.resw");
        }

        [Fact]
        public void CreditsNavItem_ShouldEqual_Credits()
        {
            GetResourceValue("CreditsNavItem.Content").Should().Be("Credits");
        }

        [Fact]
        public void CreditsHeading_ShouldContain_Licenses()
        {
            var value = GetResourceValue("CreditsHeadingTextBlock.Text");
            value.Should().NotBeNull();
            value.Should().Contain("Licenses", "Credits heading should mention Licenses");
        }

        [Fact]
        public void CreditsIntro_ShouldNotBeEmpty()
        {
            GetResourceValue("CreditsIntroTextBlock.Text").Should().NotBeNullOrWhiteSpace();
        }

        private bool CheckResourceKeyExists(string key)
        {
            if (!File.Exists(_reswPath))
            {
                return false;
            }

            try
            {
                var doc = XDocument.Load(_reswPath);
                foreach (var data in doc.Descendants("data"))
                {
                    var nameAttr = data.Attribute("name");
                    if (nameAttr != null && nameAttr.Value == key)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private string? GetResourceValue(string key)
        {
            if (!File.Exists(_reswPath))
            {
                return null;
            }

            try
            {
                var doc = XDocument.Load(_reswPath);
                foreach (var data in doc.Descendants("data"))
                {
                    var nameAttr = data.Attribute("name");
                    if (nameAttr != null && nameAttr.Value == key)
                    {
                        return data.Element("value")?.Value;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
```

> **Do not unify repo-root resolution between the two new test files.** `CreditsViewLocalizationTests` reuses the five-segment `..` approach above; `CreditsLicenseCoverageTests` (Task 1) intentionally uses the more robust `FindRepoRoot` walk because it must also locate `obj/project.assets.json`.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~CreditsViewLocalizationTests"`
Expected: FAIL — keys do not exist yet (`CreditsNavItem_ShouldEqual_Credits` returns null; existence checks return false).

- [ ] **Step 3: Add the three keys to en-US, then propagate to all cultures**

Add to `Strings/en-US/Resources.resw` first (place the `<data>` entries among the other `About*` entries, matching the file's existing `<data name="...">/<value>...</value>` style), then run the propagation script:

Run:
```bash
python Scripts/add_localization_entry.py "CreditsNavItem.Content" "Credits"
python Scripts/add_localization_entry.py "CreditsHeadingTextBlock.Text" "Open-Source Licenses"
python Scripts/add_localization_entry.py "CreditsIntroTextBlock.Text" "Grex includes the following third-party components. Each is shown with its copyright, project link, and full license text."
```

Expected: the script reports adding each key to all 100+ culture `Resources.resw` files.

> Only these three UI-chrome strings are localized. License bodies, copyrights, names, versions, and URLs come from the JSON and are **not** localized.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~CreditsViewLocalizationTests"`
Expected: PASS — all six facts pass.

- [ ] **Step 5: Commit**

```bash
git add Strings Tests/Controls/CreditsViewLocalizationTests.cs
git commit -m "Add Credits page localization strings"
```

---

## Task 4: `CreditsView` control + structure test

**Files:**
- Create: `Controls/CreditsView.xaml`
- Create: `Controls/CreditsView.xaml.cs`
- Create: `IntegrationTests/CreditsPageTests.cs`

- [ ] **Step 1: Write the failing structure test**

Create `IntegrationTests/CreditsPageTests.cs` (mirrors `IntegrationTests/AboutPageTests.cs` file-existence checks):

```csharp
using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Grex.IntegrationTests
{
    /// <summary>
    /// Integration tests for the Credits page assets.
    /// </summary>
    public class CreditsPageTests
    {
        private static string ProjectRoot()
        {
            var baseDir = AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        }

        [Fact]
        public void CreditsPage_XamlFile_Exists()
        {
            var xamlPath = Path.Combine(ProjectRoot(), "Controls", "CreditsView.xaml");
            File.Exists(xamlPath).Should().BeTrue("CreditsView.xaml should exist in Controls folder");
        }

        [Fact]
        public void CreditsPage_CodeBehindFile_Exists()
        {
            var csPath = Path.Combine(ProjectRoot(), "Controls", "CreditsView.xaml.cs");
            File.Exists(csPath).Should().BeTrue("CreditsView.xaml.cs should exist in Controls folder");
        }

        [Fact]
        public void CreditsPage_Manifest_Exists()
        {
            var jsonPath = Path.Combine(ProjectRoot(), "Assets", "third-party-licenses.json");
            File.Exists(jsonPath).Should().BeTrue("third-party-licenses.json should exist in Assets folder");
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test IntegrationTests/Grex.IntegrationTests.csproj -p:Platform=x64 --filter "FullyQualifiedName~CreditsPageTests"`
Expected: FAIL — `CreditsView.xaml`/`CreditsView.xaml.cs` do not exist (the manifest test already passes from Task 1).

- [ ] **Step 3: Create `Controls/CreditsView.xaml`**

```xml
<UserControl x:Class="Grex.Controls.CreditsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Name="CreditsControl"
             x:Uid="CreditsView"
             VerticalAlignment="Stretch"
             HorizontalAlignment="Stretch">
    <Grid VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Padding="24">
        <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
            <StackPanel HorizontalAlignment="Stretch" VerticalAlignment="Top" Spacing="16" MaxWidth="900">
                <TextBlock x:Name="CreditsHeadingTextBlock"
                           x:Uid="CreditsHeadingTextBlock"
                           FontSize="28"
                           FontWeight="Bold"/>
                <TextBlock x:Name="CreditsIntroTextBlock"
                           x:Uid="CreditsIntroTextBlock"
                           FontSize="14"
                           TextWrapping="Wrap"
                           Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
                <ItemsControl x:Name="ComponentsItemsControl"/>
            </StackPanel>
        </ScrollViewer>
    </Grid>
</UserControl>
```

- [ ] **Step 4: Create `Controls/CreditsView.xaml.cs`**

This mirrors `AboutView.xaml.cs` for theme/localization, adds `LoadLicenses()`, and extends the high-contrast tree walk with a `TextBox` branch.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Grex.Services;

namespace Grex.Controls
{
    public sealed partial class CreditsView : UserControl
    {
        public CreditsView()
        {
            this.InitializeComponent();
            LoadLicenses();
            RefreshLocalization();
            this.Loaded += CreditsView_Loaded;
            this.Unloaded += CreditsView_Unloaded;
        }

        private sealed class LicenseManifest
        {
            [JsonPropertyName("licenses")]
            public Dictionary<string, string> Licenses { get; set; } = new();

            [JsonPropertyName("components")]
            public List<LicenseComponent> Components { get; set; } = new();
        }

        private sealed class LicenseComponent
        {
            [JsonPropertyName("name")] public string Name { get; set; } = "";
            [JsonPropertyName("version")] public string Version { get; set; } = "";
            [JsonPropertyName("license")] public string License { get; set; } = "";
            [JsonPropertyName("copyright")] public string Copyright { get; set; } = "";
            [JsonPropertyName("url")] public string Url { get; set; } = "";
            [JsonPropertyName("category")] public string Category { get; set; } = "";
        }

        private void LoadLicenses()
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "third-party-licenses.json");
                if (!File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine($"CreditsView: manifest not found at {path}");
                    return;
                }

                var json = File.ReadAllText(path);
                var manifest = JsonSerializer.Deserialize<LicenseManifest>(json);
                if (manifest == null)
                {
                    return;
                }

                ComponentsItemsControl.Items.Clear();
                foreach (var c in manifest.Components)
                {
                    ComponentsItemsControl.Items.Add(BuildComponentExpander(c, manifest.Licenses));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreditsView: Failed to load licenses: {ex.Message}");
            }
        }

        private Expander BuildComponentExpander(LicenseComponent c, Dictionary<string, string> licenses)
        {
            var expander = new Expander
            {
                Header = $"{c.Name}  v{c.Version} — {c.License}",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 4),
            };

            var panel = new StackPanel { Spacing = 8, Padding = new Thickness(4, 8, 4, 4) };

            if (!string.IsNullOrWhiteSpace(c.Copyright))
            {
                panel.Children.Add(new TextBlock { Text = c.Copyright, TextWrapping = TextWrapping.Wrap });
            }

            if (!string.IsNullOrWhiteSpace(c.Url))
            {
                var link = new HyperlinkButton { Content = c.Url };
                if (Uri.TryCreate(c.Url, UriKind.Absolute, out var uri))
                {
                    link.NavigateUri = uri;
                }
                link.PointerEntered += HyperlinkButton_PointerEntered;
                link.PointerExited += HyperlinkButton_PointerExited;
                panel.Children.Add(link);
            }

            var body = licenses.TryGetValue(c.License, out var text) ? text : string.Empty;
            panel.Children.Add(new TextBox
            {
                Text = body,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                IsSpellCheckEnabled = false,
                FontFamily = new FontFamily("Consolas"),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                MaxHeight = 360,
            });

            expander.Content = panel;
            return expander;
        }

        private void CreditsView_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow.ThemeChanged += OnThemeChanged;
            DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                ApplyCurrentThemeColors();
            });
        }

        private void CreditsView_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            try
            {
                if (DispatcherQueue == null || !DispatcherQueue.TryEnqueue(() => ApplyThemeColors(e)))
                {
                    ApplyThemeColors(e);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnThemeChanged ERROR: {ex}");
            }
        }

        private void ApplyCurrentThemeColors()
        {
            try
            {
                var currentTheme = MainWindow.CurrentTheme;
                if (!IsHighContrastTheme(currentTheme))
                {
                    ClearHighContrastColors();
                    return;
                }

                var colors = MainWindow.GetCurrentThemeColors();
                ApplyThemeColors(new ThemeChangedEventArgs(currentTheme, colors.background, colors.secondary, colors.tertiary, colors.text, colors.accent));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyCurrentThemeColors ERROR: {ex}");
            }
        }

        public void ApplyThemeFromHost(ThemeChangedEventArgs e)
        {
            ApplyThemeColors(e);
        }

        private static bool IsHighContrastTheme(Services.ThemePreference preference)
        {
            return preference == Services.ThemePreference.BlackKnight ||
                   preference == Services.ThemePreference.Paranoid ||
                   preference == Services.ThemePreference.Diamond ||
                   preference == Services.ThemePreference.Subspace ||
                   preference == Services.ThemePreference.RedVelvet ||
                   preference == Services.ThemePreference.Dreams ||
                   preference == Services.ThemePreference.Tiefling ||
                   preference == Services.ThemePreference.Vibes;
        }

        private void ApplyThemeColors(ThemeChangedEventArgs e)
        {
            try
            {
                if (!IsHighContrastTheme(e.Theme))
                {
                    ClearHighContrastColors();
                    return;
                }

                ApplyForegroundToAllTextBlocks(this, e.TextBrush, e.AccentBrush);
                this.Background = e.BackgroundBrush;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyThemeColors ERROR: {ex}");
            }
        }

        private void ApplyForegroundToAllTextBlocks(DependencyObject parent, SolidColorBrush foreground, SolidColorBrush accent)
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBlock textBlock)
                {
                    textBlock.Foreground = foreground;
                }
                else if (child is TextBox textBox)
                {
                    textBox.Foreground = foreground;
                    textBox.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                }
                else if (child is ContentPresenter contentPresenter)
                {
                    contentPresenter.Foreground = foreground;
                }
                else if (child is Button button)
                {
                    button.Foreground = foreground;
                }

                ApplyForegroundToAllTextBlocks(child, foreground, accent);
            }
        }

        private void ClearHighContrastColors()
        {
            try
            {
                this.ClearValue(BackgroundProperty);
                this.Resources?.Clear();
                ClearForegroundFromVisualTree(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearHighContrastColors ERROR: {ex}");
            }
        }

        private void ClearForegroundFromVisualTree(DependencyObject parent)
        {
            try
            {
                var count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is TextBlock textBlock)
                    {
                        textBlock.ClearValue(TextBlock.ForegroundProperty);
                    }
                    else if (child is TextBox textBox)
                    {
                        textBox.ClearValue(TextBox.ForegroundProperty);
                        textBox.ClearValue(TextBox.BackgroundProperty);
                    }
                    else if (child is ContentPresenter contentPresenter)
                    {
                        contentPresenter.ClearValue(ContentPresenter.ForegroundProperty);
                    }
                    else if (child is Button button)
                    {
                        button.ClearValue(Button.ForegroundProperty);
                        button.ClearValue(Button.BackgroundProperty);
                    }

                    ClearForegroundFromVisualTree(child);
                }
            }
            catch
            {
                // Ignore errors during visual tree traversal
            }
        }

        public void RefreshLocalization()
        {
            try
            {
                var locService = LocalizationService.Instance;

                if (CreditsHeadingTextBlock != null)
                {
                    CreditsHeadingTextBlock.Text = locService.GetLocalizedString("CreditsHeadingTextBlock.Text");
                }

                if (CreditsIntroTextBlock != null)
                {
                    CreditsIntroTextBlock.Text = locService.GetLocalizedString("CreditsIntroTextBlock.Text");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreditsView: RefreshLocalization error: {ex.Message}");
            }
        }

        private void HyperlinkButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement element)
            {
                try
                {
                    var prop = typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var cursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
                        prop.SetValue(element, cursor);
                    }
                }
                catch
                {
                    // If reflection fails, do nothing
                }
            }
        }

        private void HyperlinkButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement element)
            {
                try
                {
                    var prop = typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        var cursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
                        prop.SetValue(element, cursor);
                    }
                }
                catch
                {
                    // If reflection fails, do nothing
                }
            }
        }
    }
}
```

> This relies on the same public `MainWindow` members `AboutView` already uses: the static `ThemeChanged` event, `CurrentTheme`, `GetCurrentThemeColors()`, and `ThemeChangedEventArgs`. If any of these are not accessible from `CreditsView`'s namespace, copy the exact access pattern from `Controls/AboutView.xaml.cs` (they are identical controls in the same `Grex.Controls` namespace, so they will resolve).

- [ ] **Step 5: Build and run the structure test**

Run: `dotnet build grex.sln -p:Platform=x64`
Then: `dotnet test IntegrationTests/Grex.IntegrationTests.csproj -p:Platform=x64 --filter "FullyQualifiedName~CreditsPageTests"`
Expected: PASS — build succeeds (the control compiles) and all three file-existence facts pass.

- [ ] **Step 6: Commit**

```bash
git add Controls/CreditsView.xaml Controls/CreditsView.xaml.cs IntegrationTests/CreditsPageTests.cs
git commit -m "Add CreditsView control rendering third-party licenses"
```

---

## Task 5: `MainWindow` wiring (nav, theme, localization)

The `CreditsView` exists but is not reachable. Wire it into the navigation shell and the theme/localization fan-out. There is no isolated unit test for the shell wiring (consistent with how the existing nav items are tested); verification is by build + the manual checklist in Task 6.

**Files:**
- Modify: `MainWindow.xaml`
- Modify: `MainWindow.xaml.cs`

- [ ] **Step 1: Add the nav item to `MainWindow.xaml`**

In `MainWindow.xaml`, inside `<NavigationView.FooterMenuItems>`, add `CreditsNavItem` immediately **after** the existing `AboutNavItem` `</NavigationViewItem>` closing tag:

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

- [ ] **Step 2: Add the content host to `MainWindow.xaml`**

In `MainWindow.xaml`, inside the inner `Grid` under `<SplitView.Content>`, add immediately **after** the `AboutContentGrid` `</Grid>`:

```xml
<Grid x:Name="CreditsContentGrid" Visibility="Collapsed">
    <controls:CreditsView x:Name="CreditsView"/>
</Grid>
```

- [ ] **Step 3: Add the `Credits` branch to `NavigationView_SelectionChanged` and collapse the grid elsewhere**

In `MainWindow.xaml.cs`, in `NavigationView_SelectionChanged`, add `CreditsContentGrid.Visibility = Visibility.Collapsed;` to each of the existing `Search`, `RegexBuilder`, `Settings`, and `About` branches (next to the other `*.Visibility = Visibility.Collapsed;` lines), then add a new branch after the `About` branch (immediately before the closing `}` of the `if (... is string tag)` block):

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

For reference, the `About` branch should look like this after editing (the new line is the `CreditsContentGrid` collapse):

```csharp
else if (tag == "About")
{
    SearchContentGrid.Visibility = Visibility.Collapsed;
    RegexBuilderContentGrid.Visibility = Visibility.Collapsed;
    SettingsContentGrid.Visibility = Visibility.Collapsed;
    AboutContentGrid.Visibility = Visibility.Visible;
    CreditsContentGrid.Visibility = Visibility.Collapsed;
    // Hide InfoBar when on About page
    if (StatusInfoBar != null)
    {
        StatusInfoBar.Visibility = Visibility.Collapsed;
    }
}
```

Apply the same one-line `CreditsContentGrid.Visibility = Visibility.Collapsed;` addition to the `Search`, `RegexBuilder`, and `Settings` branches.

- [ ] **Step 4: Add `CreditsContentGrid`/`CreditsView` to the theme + localization fan-out**

Make these five additions in `MainWindow.xaml.cs`, each placed directly after the matching `About*` line:

1. In the light/dark theme block, after `ApplyThemeToElement(AboutContentGrid, elementTheme, applyBackground: true);`:
```csharp
ApplyThemeToElement(CreditsContentGrid, elementTheme, applyBackground: true);
```

2. In `ApplyHighContrastTheme`, after `ApplyThemeToElement(AboutContentGrid, elementTheme, applyBackground: false);`:
```csharp
ApplyThemeToElement(CreditsContentGrid, elementTheme, applyBackground: false);
```

3. In `ApplyHighContrastTheme`, after `if (AboutContentGrid != null) AboutContentGrid.Background = backgroundBrush;`:
```csharp
if (CreditsContentGrid != null) CreditsContentGrid.Background = backgroundBrush;
```

4. In `NotifyThemeAwareControls`, after the `AboutView?.ApplyThemeFromHost(args);` try/catch block, add a matching block:
```csharp
try
{
    CreditsView?.ApplyThemeFromHost(args);
}
catch (Exception ex)
{
    Log($"NotifyThemeAwareControls CreditsView ERROR: {ex}");
}
```

5. In the high-contrast clear block, after `AboutContentGrid?.ClearValue(Grid.BackgroundProperty);`:
```csharp
CreditsContentGrid?.ClearValue(Grid.BackgroundProperty);
```

6. In `RefreshChildViews`, after `AboutView?.RefreshLocalization();`:
```csharp
CreditsView?.RefreshLocalization();
```

- [ ] **Step 5: Build to verify it compiles**

Run: `dotnet build grex.sln -p:Platform=x64`
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add MainWindow.xaml MainWindow.xaml.cs
git commit -m "Wire Credits page into navigation, theme, and localization"
```

---

## Task 6: Full verification + finish branch

**Files:** none created — verification only.

- [ ] **Step 1: Build the whole solution**

Run: `dotnet build grex.sln -p:Platform=x64`
Expected: Build succeeded, 0 errors, 0 new warnings.

- [ ] **Step 2: Run the full unit-test suite**

Run: `dotnet test Tests/Grex.Tests.csproj -p:Platform=x64`
Then: `dotnet test IntegrationTests/Grex.IntegrationTests.csproj -p:Platform=x64`
Expected: PASS — including `CreditsLicenseCoverageTests` (both facts), `CreditsViewLocalizationTests` (six facts), and `CreditsPageTests` (three facts). No regressions in `AboutView*` tests.

- [ ] **Step 3: Confirm the notices file is in sync with the manifest**

Run:
```bash
python Scripts/generate_third_party_notices.py
git diff --exit-code THIRD-PARTY-NOTICES.txt
```
Expected: generator prints `Wrote ... (21 components)` and `git diff --exit-code` returns 0 (no changes — the committed file already matches the manifest).

- [ ] **Step 4: Manual UI verification**

Run: `dotnet run --project Grex.csproj -p:Platform=x64`, then:

1. Click the new **Credits** footer nav item (below About). The page shows the localized heading "Open-Source Licenses" and the intro line.
2. Confirm one `Expander` per documented component (21 total) with header `Name vVersion — license-key` (e.g. `Newtonsoft.Json  v13.0.1 — MIT-Newtonsoft`).
3. Expand several entries: confirm the copyright line, a working project link, and the full verbatim license text in a read-only, selectable, monospaced text box that wraps and scrolls. Confirm the `Microsoft.WindowsAppSDK` family entries all show the same Microsoft EULA, and `Microsoft.WindowsAppSDK.Foundation` shows no copyright line (it has none).
4. Switch themes — Light, Dark, and at least one high-contrast theme (e.g. BlackKnight): the page foreground/background update, including the license text boxes; switching back to a non-high-contrast theme clears the overrides.
5. Switch app language (a couple of cultures): the nav label, heading, and intro localize; component names, versions, copyrights, URLs, and license bodies stay verbatim.
6. Navigate Search → Credits → About → Credits: visibility toggling and InfoBar hiding behave like the other footer pages.

- [ ] **Step 5: Final attribution scan (repo policy)**

Run: `git diff master --stat` then `git log master..HEAD -p | grep -niE "claude|anthropic|copilot|gemini|co-authored|generated with" || echo clean`
Expected: `clean` — no AI/assistant attribution in any commit or change on the branch.

- [ ] **Step 6: Finish the branch**

Use the superpowers:finishing-a-development-branch skill to choose how to integrate `feature/third-party-credits` (merge / PR / cleanup). All work is committed; the spec lives at `docs/superpowers/specs/2026-06-02-third-party-credits-design.md`.

---

## Self-Review

**Spec coverage** — every spec section maps to a task:
- §4.1 data model + `Grex.csproj` Content → Task 1 (Steps 3–5).
- §4.2 `CreditsView` control → Task 4.
- §4.3 wiring (nav, theme, localization) → Task 5; localization strings → Task 3.
- §4.4 generator + determinism → Task 2.
- §5 coverage (19 libraries + 2 platform notes + exclusions) → Task 1 manifest + drift-test allowlist.
- §6 drift test (`Assert.SkipUnless`, JSON integrity incl. `schemaVersion`) → Task 1 test.
- §6 localization test (five-segment `..`, distinct from `FindRepoRoot`) → Task 3.
- §7 files (added/edited) → File Structure + per-task file lists.
- §8 testing → Task 6.
- §9 out-of-scope (no CLI Credits, no auto-gen, generated notices) → honored throughout.

**Placeholder scan** — the only externally-sourced data is the three verbatim Microsoft/BSD license bodies in Task 1 Step 4, each with an exact pinned URL and a verifiable `grep -c "TRANSCRIBE IN STEP 4"` → `0` gate plus the `Manifest_IsInternallyValid` non-empty assertion. No vague "add error handling"-style steps; every code step shows complete code.

**Type/name consistency** — JSON fields (`schemaVersion`, `licenses`, `components`, `name`/`version`/`license`/`copyright`/`url`/`category`) are identical across the manifest (Task 1), the C# model `LicenseComponent`/`LicenseManifest` (Task 4), the Python `validate`/`render` (Task 2), and the drift test (Task 1). License keys (`MIT`, `MIT-Newtonsoft`, `BSD-3-Clause-WebView2`, `Microsoft-WindowsAppSDK`, `Microsoft-DotNet-Library`, `Microsoft-Segoe-Fluent-Icons`) referenced by components all exist in the `licenses` map. Control/element names (`CreditsHeadingTextBlock`, `CreditsIntroTextBlock`, `ComponentsItemsControl`, `CreditsContentGrid`, `CreditsView`, `CreditsNavItem`) and localization keys (`CreditsNavItem.Content`, `CreditsHeadingTextBlock.Text`, `CreditsIntroTextBlock.Text`) match across XAML, code-behind, wiring, and tests.
