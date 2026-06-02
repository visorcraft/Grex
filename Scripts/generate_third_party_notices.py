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
