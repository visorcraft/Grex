<!-- SPDX-FileCopyrightText: 2026 VisorCraft LLC -->
<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Contributing to Grex

Issues, documentation, translations, tests, and focused code changes are welcome.

Grex is a Windows-only WinUI 3 application with a Windows CLI. Read [Build and Test](docs/build-and-test.md) before starting. The .NET solution cannot be built or tested on Linux or macOS; only Python tools under `Scripts/` are cross-platform.

## Before opening work

- Search [existing issues](https://github.com/visorcraft/Grex/issues).
- For a bug, include Grex version, Windows build, target type, exact path form, query mode, filters, and reproduction steps.
- For a feature, explain the user problem and the smallest useful behavior.
- Do not disclose vulnerabilities publicly. Use [private vulnerability reporting](https://github.com/visorcraft/Grex/security/advisories/new).

Small, single-purpose changes are easier to validate and merge.

## Contribution workflow

1. Fork the repository.
2. Clone your fork:

   ```powershell
   git clone https://github.com/<you>/Grex.git
   cd Grex
   git config core.autocrlf false
   ```

3. Create a focused branch:

   ```powershell
   git switch -c fix-search-filter
   ```

4. Restore and build on Windows:

   ```powershell
   dotnet restore grex.sln
   dotnet build grex.sln -p:Platform=x64
   ```

5. Make the smallest complete change.
6. Add or update tests and public docs.
7. Run the appropriate checks.
8. Push the branch and open a pull request against `master`.

A pull request should state:

- the problem;
- the behavior change;
- tests run;
- known limitations;
- screenshots for visible UI changes.

## Repository layout

| Path | Purpose |
| --- | --- |
| `Grex.csproj` | WinUI 3 GUI project |
| `Grex.Cli/` | `grex-cli` parser, runner, options, and formatters |
| `Controls/` | XAML controls and UI-specific code-behind |
| `ViewModels/` | Tab and shell state/orchestration |
| `Services/` | Search, replace, platform, persistence, localization, AI, and export logic |
| `Models/` | Result and persistence DTOs |
| `Converters/` | WinUI value converters |
| `Strings/` | 108 `.resw` resource catalogs |
| `Assets/` | Product artwork and third-party license manifest |
| `Tests/` | Unit and CLI test projects |
| `IntegrationTests/` | Filesystem and app integration tests |
| `UITests/` | ViewModel-driven UI tests |
| `Scripts/` | Python maintenance tools |
| `docs/` | Public user and developer documentation |

Read [Architecture](docs/architecture.md) before changing component boundaries.

## Development commands

```powershell
dotnet build grex.sln -c Debug -p:Platform=x64
dotnet build grex.sln -c Release -p:Platform=x64
dotnet test grex.sln -c Release -p:Platform=x64 -p:WindowsAppSdkBootstrapInitialize=false
dotnet run --project Grex.csproj -p:Platform=x64
dotnet run --project Grex.Cli/Grex.Cli.csproj -p:Platform=x64 -- "C:\repo" "TODO"
```

Always use a concrete platform. Any CPU fails with WinUI tooling.

## Code expectations

- Follow existing namespace, formatting, and naming patterns.
- Keep engine and platform work in Services.
- Keep observable tab state in ViewModels.
- Keep WinUI-only events, dialogs, pickers, and dynamic menus in view code-behind.
- Use async/await for I/O.
- Pass `CancellationToken` through async call chains.
- Do not block the UI thread.
- Prefer stateless services where practical.
- Use existing helpers and dependencies before adding abstractions.
- Do not add a dependency for a small standard-library task.
- Route user-facing strings through `LocalizationService`.
- Update public docs with user-visible behavior.

### Performance and lifecycle rules

These rules prevent known hangs and memory growth:

- Never use `RegexOptions.Compiled` for dynamic or user-supplied patterns.
- Give dynamic Regex operations an explicit timeout.
- Cancel and dispose owned `CancellationTokenSource` instances.
- Unsubscribe handlers that can outlive their subscriber.
- Store a handler when a lambda must later be removed.
- Put explicit caps and eviction on caches, histories, buffers, and retained conversations.
- Bound prefix reads when full content is unnecessary.
- Protect mutable caches used by parallel search.
- Dispose processes, streams, HTTP responses, and other handles.

## Tests

Match tests to the changed boundary:

| Change | Test location |
| --- | --- |
| Search, replace, filtering, encoding, settings, persistence | `Tests/Grex.Tests.csproj` |
| CLI parsing, runner, output | `Tests/Grex.Cli.Tests/Grex.Cli.Tests.csproj` |
| Real filesystem/app integration | `IntegrationTests/Grex.IntegrationTests.csproj` |
| ViewModel-driven UI behavior | `UITests/Grex.UITests.csproj` |
| Python maintenance tools | `Scripts/test_*.py` |

Full gate:

```powershell
dotnet test grex.sln -c Release -p:Platform=x64 -p:WindowsAppSdkBootstrapInitialize=false
```

`.github/workflows/ci.yml` runs the Release x64 build and full solution test gate on pull requests, pushes to `master`, and manual dispatches.

Focused test:

```powershell
dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 -p:WindowsAppSdkBootstrapInitialize=false --filter "FullyQualifiedName~SearchServiceTests"
```

xUnit 2.9.3 has no `Assert.Skip` or `Assert.SkipUnless`. Return early for a runtime condition or use `Xunit.SkippableFact`. Use static `[Fact(Skip = "...")]` only when the reason is fixed and explicit.

Bug fixes should add one test that fails before the fix and exercises the shared root cause.

## UI changes

- Preserve keyboard access and readable focus states.
- Localize labels, placeholders, errors, menus, tooltips, and accessibility names.
- Check narrow and wide window layouts.
- Check the affected built-in themes.
- Verify light/dark readability where applicable.
- Include screenshots for visible changes.
- Dispose or unregister view-owned resources on unload/close.

Do not invent a new control abstraction for one use.

## Localization

English is the source catalog:

```text
Strings/en-US/Resources.resw
```

To add text:

1. Add the English key with `status:complete`.
2. Propagate it:

   ```powershell
   python Scripts/add_localization_entry.py "KeyName" "English text"
   ```

3. Review non-English placeholders and statuses.

To remove text:

```powershell
python Scripts/remove_localization_entry.py "KeyName"
```

To inspect current coverage:

```powershell
python Scripts/generate_translation_status.py
```

See [Translation and Localization](docs/translations.md) for key patterns, automated translation, tests, and review rules.

### Resource line endings

`.resw` files are CRLF. Python on Linux/macOS emits LF and can rewrite every line.

Verify:

```powershell
git ls-files --eol Strings/en-US/Resources.resw
```

Expected index form is `i/crlf`. Convert changed resource files back to CRLF before committing.

## Other line endings

Repository convention:

- `.cs`, `.xaml`, `.csproj`, `.resw`: CRLF
- `.py`, `.json`, generated `.txt`: LF

Check touched files:

```powershell
git ls-files --eol <path>
```

Avoid unrelated whole-file newline changes.

## Documentation

Update the existing source for the behavior:

| Topic | File |
| --- | --- |
| Public landing/install overview | `README.md` |
| User workflow and troubleshooting | `docs/usage.md` |
| Capability and target matrix | `docs/features.md` |
| Exact options, schemas, paths, limits | `docs/reference.md` |
| Components and data flow | `docs/architecture.md` |
| Build, test, package | `docs/build-and-test.md` |
| Localization workflow | `docs/translations.md` |
| Security and privacy | `SECURITY.md` |

Documentation must describe current code. Do not claim encryption, transactions, rollback, platform support, complete translations, or compatibility that tests/source do not provide.

Use relative repository links where possible and verify every local link.

## Dependencies and licenses

Avoid new dependencies unless they remove more complexity than they add.

For any GUI NuGet change:

1. Check license compatibility with GPL-3.0.
2. Update `Assets/third-party-licenses.json`.
3. Regenerate:

   ```powershell
   python Scripts/generate_third_party_notices.py
   ```

4. Run:

   ```powershell
   python Scripts/test_generate_third_party_notices.py
   dotnet test Tests/Grex.Tests.csproj -p:Platform=x64 -p:WindowsAppSdkBootstrapInitialize=false --filter "FullyQualifiedName~CreditsLicenseCoverageTests"
   ```

Never hand-edit `THIRD-PARTY-NOTICES.txt`.

Build/test-only packages belong in `KnownBuildOnlyExclusions` in `Tests/Controls/CreditsLicenseCoverageTests.cs`, with a documented reason.

## Commit and pull request hygiene

- Keep commits focused.
- Do not mix formatting churn with behavior.
- Do not commit secrets, settings exports, logs, Docker mirrors, build output, or release archives.
- Use the human committer's authorship only.
- Do not add AI co-author trailers, generated-by footers, bot attribution, or similar metadata.
- Explain migrations, compatibility risks, and destructive behavior.
- Update tests, localization, docs, and third-party notices in the same pull request when required.

## Release process

Maintainers only.

1. Start from a clean, current `master`.
2. Run the full Windows Release build and test gate.
3. Check vulnerable packages:

   ```powershell
   dotnet list package --vulnerable
   ```

4. Update versions with the script:

   ```powershell
   python Scripts/update_version.py 1.5.0
   ```

5. Review synchronized changes in:

   - `Directory.Build.props`
   - `Properties/AssemblyInfo.cs`
   - `Package.appxmanifest`
   - `app.manifest`
   - `Controls/AboutView.xaml.cs`

6. Commit the version.
7. Create a signed annotated tag:

   ```powershell
   git tag -m "Grex 1.5.0" v1.5.0
   ```

8. Push branch and tag:

   ```powershell
   git push origin master
   git push origin v1.5.0
   ```

9. Confirm `.github/workflows/release.yml` publishes:

   - `grex-<version>-setup.exe`
   - `grex-<version>-win-x64.zip`
   - `grex-cli-<version>-win-x64.zip`

The release workflow runs the full solution test command before packaging. Run the same test gate locally before tagging.

## Pull request checklist

- [ ] One clear problem and focused solution
- [ ] Windows build passes with a concrete platform
- [ ] Relevant tests pass
- [ ] Regression test added for non-trivial bug fix
- [ ] User-facing strings localized
- [ ] Public docs updated
- [ ] Security/privacy impact reviewed
- [ ] Dependency notices updated when needed
- [ ] Required line endings preserved
- [ ] UI screenshots included when applicable
- [ ] No secrets, generated artifacts, or attribution trailers
