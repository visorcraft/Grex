---
title: Build and Test
layout: default
---

# Build and Test

Grex's GUI, CLI, and .NET tests build only on Windows. WinUI XAML compilation, Windows App SDK, MSIX, and PRI tooling prevent a complete build on Linux or macOS, even with `-p:EnableWindowsTargeting=true`.

Only the Python tools under `Scripts/` are cross-platform.

## Prerequisites

Required:

- Windows 10 version 1809, build 17763, or later
- Git
- .NET 8 SDK
- a concrete target platform: x86, x64, or ARM64

Recommended:

- Windows 11
- Visual Studio 2022 with .NET desktop development tools, or VS Code with C# tooling
- Windows App Runtime 1.8 for running and notification tests
- PowerShell 5.1 or later

Optional:

- Docker Desktop for Docker integration
- WSL plus `grep`, `find`, `sed`, and `stat` for WSL integration
- Inno Setup 6 or 7 to build `setup.exe`
- ImageMagick when replacing icon assets

Install common prerequisites:

```powershell
winget install --id Microsoft.DotNet.SDK.8 -e --source winget
winget install --id Microsoft.WindowsAppRuntime.1.8 -e --source winget
```

The Windows App SDK NuGet packages are restored by the solution. The Windows App Runtime is needed to run the unpackaged GUI.

## Clone and inspect

The repository intentionally tracks C#, XAML, project, and resource files as CRLF. Python, JSON, and generated notices use LF.

```powershell
git clone https://github.com/visorcraft/Grex.git
cd Grex
git config core.autocrlf false
dotnet --info
```

Verify a critical resource file:

```powershell
git ls-files --eol Strings/en-US/Resources.resw
```

Expected index form is `i/crlf`.

## Restore

```powershell
dotnet restore grex.sln
```

Do not omit the concrete platform from build, run, or test commands.

## Build

### Debug x64

```powershell
dotnet build grex.sln -p:Platform=x64
```

### Release x64

```powershell
dotnet build grex.sln -c Release -p:Platform=x64
```

### Other declared platforms

```powershell
dotnet build grex.sln -p:Platform=x86
dotnet build grex.sln -p:Platform=ARM64
```

The project declares x86, x64, and ARM64. The maintained release workflow publishes win-x64 only.

### Build individual projects

```powershell
dotnet build Grex.csproj -p:Platform=x64
dotnet build Grex.Cli/Grex.Cli.csproj -p:Platform=x64
```

## Run

### GUI

```powershell
dotnet run --project Grex.csproj -p:Platform=x64
```

After a Debug build:

```powershell
.\bin\x64\Debug\net8.0-windows10.0.19041.0\Grex.exe
```

Avoid running elevated if testing notifications. Windows toasts can fail in an administrator session.

### CLI

After a Debug build:

```powershell
.\Grex.Cli\bin\x64\Debug\net8.0-windows10.0.19041.0\grex-cli.exe --help
.\Grex.Cli\bin\x64\Debug\net8.0-windows10.0.19041.0\grex-cli.exe "C:\repo" "TODO"
```

Through `dotnet run`, put CLI arguments after `--`:

```powershell
dotnet run --project Grex.Cli/Grex.Cli.csproj -p:Platform=x64 -- "C:\repo" "TODO"
```

## Test

### Entire solution

```powershell
dotnet test grex.sln -p:Platform=x64
```

### Release configuration

```powershell
dotnet test grex.sln -c Release -p:Platform=x64
```

### Individual projects

```powershell
dotnet test Tests/Grex.Tests.csproj -p:Platform=x64
dotnet test Tests/Grex.Cli.Tests/Grex.Cli.Tests.csproj -p:Platform=x64
dotnet test IntegrationTests/Grex.IntegrationTests.csproj -p:Platform=x64
dotnet test UITests/Grex.UITests.csproj -p:Platform=x64
```

### One class or method

```powershell
dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~SearchServiceTests"
dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~TestMethodName"
```

xUnit 2.9.3 does not provide `Assert.Skip` or `Assert.SkipUnless`. For a runtime condition, return early or use `Xunit.SkippableFact`. Static `[Fact(Skip = "...")]` is used for tests that require unavailable WinUI initialization.

### Test scope

| Project | Scope |
| --- | --- |
| Grex.Tests | Services, models, ViewModels, control helpers, credits/license coverage |
| Grex.Cli.Tests | CLI runner and output formatters |
| Grex.IntegrationTests | Real filesystem and app integration |
| Grex.UITests | ViewModel-driven UI behavior |

UI tests do not use WinAppDriver. Some integration facts that instantiate full WinUI context are skipped.

## Python script checks

These use the Python standard library and can run on Windows, Linux, or macOS:

```powershell
python Scripts/test_add_localization_entry.py
python Scripts/test_remove_localization_entry.py
python Scripts/test_generate_third_party_notices.py
```

Pytest can also discover them when installed:

```powershell
python -m pytest Scripts/test_add_localization_entry.py Scripts/test_remove_localization_entry.py Scripts/test_generate_third_party_notices.py
```

## Publish

### GUI

```powershell
dotnet publish Grex.csproj -c Release -p:Platform=x64 --self-contained -r win-x64
```

Default output:

```text
bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\
```

### CLI

```powershell
dotnet publish Grex.Cli/Grex.Cli.csproj -c Release -p:Platform=x64 --self-contained -r win-x64
```

Default output:

```text
Grex.Cli\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\
```

These publishes include the .NET runtime. The GUI still depends on Windows App Runtime 1.8 unless its deployment model changes.

## One-command package build

Run from a non-elevated Windows PowerShell:

```powershell
.\build.ps1
```

The script has no parameters and uses Release, x64, and win-x64. It:

1. reads the version from `Directory.Build.props`;
2. restores `grex.sln`;
3. builds the solution;
4. runs all solution tests;
5. publishes self-contained GUI and CLI folders;
6. creates versioned ZIP files in `dist\`;
7. creates an Inno Setup installer when `ISCC.exe` is available.

Artifacts:

```text
dist\grex-<version>-win-x64.zip
dist\grex-cli-<version>-win-x64.zip
dist\grex-<version>-setup.exe
```

If tests fail, `build.ps1` still produces artifacts for inspection, then exits with code 1. Do not release those artifacts.

If Inno Setup is unavailable, ZIP creation succeeds and setup creation is skipped with a warning.

## Installer behavior

`Grex.iss` creates a per-user x64-compatible installer:

- install root: `%LocalAppData%\Programs\Grex`;
- GUI in the root;
- CLI in `cli\`;
- Start menu shortcut;
- optional desktop shortcut;
- optional current-user `PATH` entry for the CLI;
- built-in uninstaller that removes that PATH entry.

The script does not code-sign the installer or install Windows App Runtime.

## Versioning

Never edit version strings by hand:

```powershell
python Scripts/update_version.py 1.5.0
```

The script synchronizes:

- `Directory.Build.props`;
- `Properties/AssemblyInfo.cs`;
- `Package.appxmanifest`;
- `app.manifest`;
- `Controls/AboutView.xaml.cs`.

Versions are three-part SemVer. Manifests and assembly versions receive the required fourth `.0` component.

## Third-party notices

When adding, removing, or updating a GUI NuGet dependency:

1. Update `Assets/third-party-licenses.json`.
2. Regenerate notices:

   ```powershell
   python Scripts/generate_third_party_notices.py
   ```

3. Run:

   ```powershell
   python Scripts/test_generate_third_party_notices.py
   dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 --filter "FullyQualifiedName~CreditsLicenseCoverageTests"
   ```

Never hand-edit `THIRD-PARTY-NOTICES.txt`.

Build/test-only packages belong in `KnownBuildOnlyExclusions` inside `Tests/Controls/CreditsLicenseCoverageTests.cs`, with a reason, rather than in the redistributed-component manifest.

## Localization maintenance

Add one key to every catalog:

```powershell
python Scripts/add_localization_entry.py "KeyName" "English text"
```

Remove one key from every catalog:

```powershell
python Scripts/remove_localization_entry.py "KeyName"
```

Report status:

```powershell
python Scripts/generate_translation_status.py
```

Review [Translations](translations.md) before running bulk translation. `.resw` files must remain CRLF.

## Assets

`Assets/Grex.png` is the source artwork. `Grex.csproj`, `Package.appxmanifest`, and `Grex.iss` reference the generated PNG and ICO variants.

There is no maintained icon-generation project in this repository. When replacing artwork:

1. regenerate every referenced size with a deterministic image tool;
2. preserve transparency and exact dimensions;
3. verify manifest and project paths;
4. inspect the installer icon and running app;
5. avoid touching unrelated binary assets.

## Continuous integration

`.github/workflows/ci.yml` runs on pull requests, pushes to `master`, and manual dispatches. Its Windows x64 job restores the solution, builds Release without a second restore, and tests every solution project without rebuilding. Test projects run serially to avoid shared-machine state collisions. A three-minute per-test hang detector stops deadlocked test hosts, and the job has a 30-minute ceiling.

## Release workflow

`.github/workflows/release.yml` runs when a `v*` tag is pushed. On `windows-latest`, it:

1. checks out the tag;
2. installs .NET 8;
3. restores and builds Release x64;
4. tests the full solution;
5. publishes self-contained win-x64 GUI and CLI;
6. creates both ZIP files;
7. compiles the Inno Setup installer;
8. uploads artifacts;
9. creates a GitHub Release.

The release workflow fails before packaging if any solution test fails. Maintainers should still run the same test gate before tagging.

Release tags are signed and annotated:

```powershell
git tag -m "Grex 1.5.0" v1.5.0
git push origin master
git push origin v1.5.0
```

See [Contributing](../CONTRIBUTING.md#release-process) for the full maintainer checklist.

## Troubleshooting

| Error or symptom | Cause and fix |
| --- | --- |
| XAML compiler failure on Linux/macOS | Unsupported host. Build on Windows. |
| Any CPU failure | Add `-p:Platform=x64`, x86, or ARM64. |
| App starts but notifications fail | Install Windows App Runtime 1.8 and launch non-elevated. |
| `Grex.exe` missing resources | Run from the complete build/publish directory; verify PRI and `Assets\` copies. |
| Docker tests/features unavailable | Start Docker and confirm `docker version`. |
| WSL search unavailable | Confirm `wsl --status` and target command availability. |
| Massive `.resw` diff | Restore CRLF handling and rerun only the intended script. |
| Credits coverage test fails | Update the license manifest or justified build-only exclusions, then regenerate notices. |
| Release setup missing | Install Inno Setup or use the release workflow runner. |

Application runtime logs are written to `%Temp%\Grex.log`.
