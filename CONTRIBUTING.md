<!-- SPDX-FileCopyrightText: 2026 VisorCraft LLC -->
<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Contributing to Grex

Thank you for helping improve Grex. This project is a WinUI 3 / .NET 8
desktop application with an MVVM core, a scriptable CLI
(`Grex.Cli`), and a search engine that targets native drives, UNC
shares, Docker containers, and WSL distributions. Changes should be
small, tested, and aligned with the existing project boundaries.

## Contribution workflow

1. Fork the repository on GitHub.
2. Clone your fork:

   ```powershell
   git clone https://github.com/<you>/grex.git
   cd grex
   ```

3. Create a focused branch:

   ```powershell
   git checkout -b fix-search-filter
   ```

4. Install the development prerequisites from
   [docs/build-and-test.md](docs/build-and-test.md).
5. Make the smallest change that fully solves the issue.
6. Add or update tests and documentation.
7. Run the local gate:

   ```powershell
   dotnet build grex.sln -c Release -p:Platform=x64
   dotnet test grex.sln -c Release -p:Platform=x64
   ```

8. Push your branch and open a pull request against `master`.

Pull requests should include a clear summary, the tests you ran, and
screenshots when the change affects the GUI.

## Project layout

- `Grex.csproj` - the WinUI 3 desktop app (root project).
- `Grex.Cli/` - the `grex-cli.exe` command-line companion.
- `Models/` - POCO data types used by both the UI and the engine.
- `Services/` - search, replace, settings, history, container, WSL,
  encoding, document-extraction, localization, and journaling logic.
- `ViewModels/` - observable VMs bound to XAML. No engine logic.
- `Controls/` - reusable XAML controls.
- `Converters/` - `IValueConverter` implementations for XAML bindings.
- `Strings/` - Windows resource files (`.resw`) for 100+ locales.
- `Tests/` - unit tests (`Grex.Tests`, `Grex.Cli.Tests`).
- `IntegrationTests/` - integration tests that touch the real
  filesystem, Docker, or WSL.
- `UITests/` - ViewModel-driven UI tests.
- `Scripts/` - Python helpers for localization and version bumping.
- `docs/` - feature reference, architecture, usage guide, and audits.

Keep algorithmic behavior in `Services/`. ViewModels should orchestrate
service calls and surface state for binding - they should not
re-implement search, replace, or filtering logic.

## Local development

Use the .NET CLI:

```powershell
dotnet restore
dotnet build grex.sln -c Debug   -p:Platform=x64   # debug build
dotnet build grex.sln -c Release -p:Platform=x64   # release build
dotnet test  grex.sln -c Release -p:Platform=x64   # all tests

# Run the GUI:
dotnet run --project Grex.csproj -c Debug -p:Platform=x64

# Run a CLI search:
dotnet run --project Grex.Cli/Grex.Cli.csproj -- "needle" .
```

The WinApp SDK requires a concrete `-p:Platform` (x86 / x64 / ARM64).
"Any CPU" builds will fail.

## Coding standards

- Target .NET 8 with C# 12. Use `nullable` annotations.
- Follow MVVM strictly: keep search / filesystem / Docker / WSL code in
  `Services/`, observable state in `ViewModels/`, and bind from XAML.
- Use `async`/`await` end-to-end for I/O-bound work. Do not block the
  UI thread.
- Route all user-facing strings through the localization service -
  never hard-code English in XAML or C#. New keys must be added to the
  English `Strings/en-US/Resources.resw` and to every other locale
  catalog with a placeholder if the translation is not ready.
- Prefer explicit, focused code over speculative abstraction.
- Add comments only when the reason is not obvious from the code.
- Do not hand-edit `.csproj` files for refactors that the IDE can do.
- Do not add a code-behind override that mutates a ViewModel - go
  through bindings or expose a command.
- Do not require nightly tooling. CI runs the public .NET 8 SDK.

Every new source file must include the SPDX short header used by the
repository:

```text
SPDX-FileCopyrightText: 2026 VisorCraft LLC
SPDX-License-Identifier: GPL-3.0-only
```

Use the comment syntax appropriate for the file type (`//` for C# and
`<!-- ... -->` for XAML/XML).

## XAML and UI changes

- Reusable controls live under `Controls/`.
- New `IValueConverter` types belong in `Converters/`.
- Theme-aware brushes should follow the existing pattern (Mica
  backdrop, light/dark variants).
- Touch UI changes need a screenshot in the PR - both light and dark
  themes when applicable.

## Tests

Match test coverage to the risk of the change:

- Search, replace, encoding, filtering, and settings changes need unit
  tests under `Tests/Grex.Tests/`.
- CLI flag or output changes need integration coverage under
  `Tests/Grex.Cli.Tests/`.
- Real-filesystem behavior, Docker exec, or WSL search changes belong
  in `IntegrationTests/` with `tempfile`-style fixtures.
- UI behavior changes should add or extend `UITests/`.

The full test gate before opening a pull request:

```powershell
dotnet test grex.sln -c Release -p:Platform=x64
```

## Localization

English (`en-US`) is the source catalog:

```text
Strings/en-US/Resources.resw
```

When adding or renaming a resource key:

1. Add the English entry.
2. Run the locale-sync script to propagate the key to every shipped
   locale with a placeholder:

   ```powershell
   python Scripts\add_localization_entry.py "ResourceKey" "English text"
   ```

3. Mark the new entry `status:incomplete` in non-English locales so the
   translation queue can pick it up.

To remove a key, use `Scripts\remove_localization_entry.py`. The
translation conventions are documented in
[docs/translations.md](docs/translations.md).

## Documentation

Update documentation in the same pull request when behavior changes.

- User workflows belong in [docs/usage.md](docs/usage.md).
- Settings, CLI flags, shortcuts, and reference tables belong in
  [docs/reference.md](docs/reference.md).
- Architecture or service-boundary changes belong in
  [docs/architecture.md](docs/architecture.md).
- New features should be mentioned in [docs/features.md](docs/features.md).

## Releasing

The product version is declared in several files. Do **not** edit them by
hand - run the helper, which keeps every location in sync:

```powershell
python Scripts\update_version.py 1.2.0
```

Grex uses 3-part SemVer (`X.Y.Z`). A 2-part value (`1.2`) is accepted and
normalized to `1.2.0`. The script updates every version location:

- `Directory.Build.props` - `<Version>X.Y.Z</Version>` (solution-wide; drives
  the CLI's informational version).
- `Properties/AssemblyInfo.cs` - `AssemblyVersion` / `AssemblyFileVersion`
  (4-part `X.Y.Z.0`) and `AssemblyInformationalVersion` (`X.Y.Z`).
- `Package.appxmanifest` - `Version="X.Y.Z.0"` (the MSIX schema requires four
  parts, so it cannot be 3-part).
- `app.manifest` - `<assemblyIdentity version="X.Y.Z.0" />`.
- `Controls/AboutView.xaml.cs` - the hard-coded fallback shown in the About
  dialog (the live value is read from the assembly at runtime).

After bumping, commit and tag the release commit (tags are signed):

```powershell
git commit -am "Update version to 1.2.0"
git tag -m "Grex 1.2.0" v1.2.0
git push && git push origin v1.2.0
```

Pushing a `v*` tag triggers the release workflow
(`.github/workflows/release.yml`), which builds, packages, and publishes the
`win-x64` artifact.

## Dependency policy

Grex is GPL-3.0-only. New NuGet packages must use licenses compatible
with GPL-3.0 (MIT, Apache-2.0, BSD-*, MS-PL, etc.). If a dependency
needs license clarification, explain the reason in the pull request.

Avoid new dependencies unless they clearly reduce complexity or
provide well-tested domain behavior that should not be maintained
locally.

## Pull request expectations

A good pull request:

- Has one clear purpose.
- Describes user-visible behavior changes.
- Calls out migrations or compatibility risks.
- Includes tests, or explains why tests are not practical.
- Updates docs and localization when needed.
- Builds and tests cleanly under `-p:Platform=x64`.
- Avoids unrelated formatting or refactoring churn.

Maintainers may ask for smaller commits, additional tests, or docs
updates before merging.

## Security

Do not report security issues through public issues or pull requests.
Follow the disclosure policy in [SECURITY.md](SECURITY.md).
