#!/usr/bin/env python3
"""
Version Update Script for Grex

Updates the version number across all relevant project files.

Usage:
    python update_version.py <new_version>

Examples:
    python update_version.py 1.1.0
    python update_version.py 2.0.0

The version uses 3-part SemVer (X.Y.Z). A 2-part version (e.g. "1.2") is
accepted and normalized to "1.2.0". Files that require a 4-part assembly
version (the manifests and AssemblyInfo) use the "X.Y.Z.0" form.
"""

import re
import sys
from pathlib import Path


def get_project_root() -> Path:
    """Get the project root directory (parent of Scripts folder)."""
    return Path(__file__).parent.parent


def read_file_binary(file_path: Path) -> bytes:
    """Read file content as binary to preserve exact byte content."""
    with open(file_path, "rb") as f:
        return f.read()


def write_file_binary(file_path: Path, content: bytes) -> None:
    """Write file content as binary to preserve exact byte content."""
    with open(file_path, "wb") as f:
        f.write(content)


def regex_replace_binary(content: bytes, pattern: str, replacement: str) -> tuple[bytes, int]:
    """Perform regex replacement on binary content, preserving line endings."""
    text = content.decode("utf-8")
    new_text, count = re.subn(pattern, replacement, text)
    return new_text.encode("utf-8"), count


def update_directory_build_props(project_root: Path, version: str) -> bool:
    """Update Directory.Build.props <Version> (3-part SemVer; solution-wide)."""
    file_path = project_root / "Directory.Build.props"

    if not file_path.exists():
        print(f"ERROR: File not found: {file_path}")
        return False

    content = read_file_binary(file_path)

    # Pattern to match: <Version>X.Y[.Z]</Version>
    pattern = r"<Version>[0-9]+(?:\.[0-9]+)+</Version>"
    replacement = f"<Version>{version}</Version>"

    new_content, count = regex_replace_binary(content, pattern, replacement)

    if count == 0:
        print(f"WARNING: No version pattern found in {file_path}")
        return False

    write_file_binary(file_path, new_content)
    print(f"Updated {file_path} ({count} replacement(s))")
    return True


def update_about_view(project_root: Path, version: str) -> bool:
    """Update the hard-coded fallback version in Controls/AboutView.xaml.cs.

    The version shown in the About dialog is normally read from the assembly
    at runtime; this only updates the fallback string used if that fails.
    """
    file_path = project_root / "Controls" / "AboutView.xaml.cs"

    if not file_path.exists():
        print(f"ERROR: File not found: {file_path}")
        return False

    content = read_file_binary(file_path)

    # Pattern to match: VersionTextBlock.Text = "Version X.Y[.Z]";
    pattern = r'VersionTextBlock\.Text = "Version [0-9]+(?:\.[0-9]+)+";'
    replacement = f'VersionTextBlock.Text = "Version {version}";'

    new_content, count = regex_replace_binary(content, pattern, replacement)

    if count == 0:
        print(f"WARNING: No version pattern found in {file_path}")
        return False

    write_file_binary(file_path, new_content)
    print(f"Updated {file_path} ({count} replacement(s))")
    return True


def update_package_manifest(project_root: Path, version: str) -> bool:
    """Update Package.appxmanifest with the new version (4-part X.Y.Z.0)."""
    file_path = project_root / "Package.appxmanifest"

    if not file_path.exists():
        print(f"ERROR: File not found: {file_path}")
        return False

    content = read_file_binary(file_path)

    # Match the standalone package Version="X.Y.Z.0" attribute only.
    # The negative lookbehind avoids clobbering MinVersion / MaxVersionTested.
    pattern = r'(?<![A-Za-z])Version="[0-9]+(?:\.[0-9]+){3}"'
    replacement = f'Version="{version}.0"'

    new_content, count = regex_replace_binary(content, pattern, replacement)

    if count == 0:
        print(f"WARNING: No version pattern found in {file_path}")
        return False

    write_file_binary(file_path, new_content)
    print(f"Updated {file_path} ({count} replacement(s))")
    return True


def update_assembly_info(project_root: Path, version: str) -> bool:
    """Update Properties/AssemblyInfo.cs with the new version.

    AssemblyVersion / AssemblyFileVersion use the 4-part X.Y.Z.0 form;
    AssemblyInformationalVersion uses the 3-part SemVer X.Y.Z.
    """
    file_path = project_root / "Properties" / "AssemblyInfo.cs"

    if not file_path.exists():
        print(f"ERROR: File not found: {file_path}")
        return False

    text = read_file_binary(file_path).decode("utf-8")
    total_count = 0

    # 4-part assembly + file version
    text, count = re.subn(
        r'\[assembly: AssemblyVersion\("[0-9]+(?:\.[0-9]+){1,3}"\)\]',
        f'[assembly: AssemblyVersion("{version}.0")]',
        text,
    )
    total_count += count

    text, count = re.subn(
        r'\[assembly: AssemblyFileVersion\("[0-9]+(?:\.[0-9]+){1,3}"\)\]',
        f'[assembly: AssemblyFileVersion("{version}.0")]',
        text,
    )
    total_count += count

    # 3-part informational (SemVer) version
    text, count = re.subn(
        r'\[assembly: AssemblyInformationalVersion\("[0-9]+(?:\.[0-9]+)+"\)\]',
        f'[assembly: AssemblyInformationalVersion("{version}")]',
        text,
    )
    total_count += count

    if total_count == 0:
        print(f"WARNING: No version patterns found in {file_path}")
        return False

    write_file_binary(file_path, text.encode("utf-8"))
    print(f"Updated {file_path} ({total_count} replacement(s))")
    return True


def update_app_manifest(project_root: Path, version: str) -> bool:
    """Update app.manifest with the new version (4-part X.Y.Z.0)."""
    file_path = project_root / "app.manifest"

    if not file_path.exists():
        print(f"ERROR: File not found: {file_path}")
        return False

    content = read_file_binary(file_path)

    # Pattern to match: <assemblyIdentity version="X.Y.Z.0"
    pattern = r'<assemblyIdentity version="[0-9]+(?:\.[0-9]+){3}"'
    replacement = f'<assemblyIdentity version="{version}.0"'

    new_content, count = regex_replace_binary(content, pattern, replacement)

    if count == 0:
        print(f"WARNING: No version pattern found in {file_path}")
        return False

    write_file_binary(file_path, new_content)
    print(f"Updated {file_path} ({count} replacement(s))")
    return True


def normalize_version(version: str):
    """Validate and normalize the version to 3-part X.Y.Z.

    Accepts X.Y (normalized to X.Y.0) or X.Y.Z. Returns the normalized
    string, or None if the input is not a valid version.
    """
    if re.match(r"^[0-9]+\.[0-9]+$", version):
        return f"{version}.0"
    if re.match(r"^[0-9]+\.[0-9]+\.[0-9]+$", version):
        return version
    return None


def main():
    if len(sys.argv) != 2:
        print("Usage: python update_version.py <new_version>")
        print("Example: python update_version.py 1.1.0")
        sys.exit(1)

    version = normalize_version(sys.argv[1])

    if version is None:
        print(f"ERROR: Invalid version format '{sys.argv[1]}'")
        print("Version must be X.Y or X.Y.Z (e.g., 1.1.0, 2.0.0, 10.5.3)")
        sys.exit(1)

    project_root = get_project_root()
    print(f"Project root: {project_root}")
    print(f"Updating to version: {version}")
    print("-" * 50)

    results = []
    results.append(("Directory.Build.props", update_directory_build_props(project_root, version)))
    results.append(("AboutView.xaml.cs", update_about_view(project_root, version)))
    results.append(("Package.appxmanifest", update_package_manifest(project_root, version)))
    results.append(("AssemblyInfo.cs", update_assembly_info(project_root, version)))
    results.append(("app.manifest", update_app_manifest(project_root, version)))

    print("-" * 50)

    success_count = sum(1 for _, success in results if success)
    total_count = len(results)

    if success_count == total_count:
        print(f"SUCCESS: All {total_count} files updated to version {version}")
        print("Next steps:")
        print(f'  git commit -am "Update version to {version}"')
        print(f'  git tag -m "Grex {version}" v{version}   # tags are signed')
        print(f"  git push && git push origin v{version}")
        sys.exit(0)
    else:
        print(f"PARTIAL: {success_count}/{total_count} files updated")
        for name, success in results:
            status = "OK" if success else "FAILED"
            print(f"  {status}: {name}")
        sys.exit(1)


if __name__ == "__main__":
    main()
