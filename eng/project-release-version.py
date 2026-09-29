#!/usr/bin/env python3

"""Project the Libraries package version into a disposable source tree."""

from __future__ import annotations

import argparse
import json
import re
import subprocess
from pathlib import Path


VERSION_PATTERN = re.compile(
    r"^[0-9]+\.[0-9]+\.[0-9]+"
    r"(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?"
    r"(?:\+[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$"
)
VERSION_FILE = Path("eng/NetWasm.ReleaseVersion.txt")
PACKAGE_VERSIONS_FILE = Path("eng/NetWasm.PublicPackageVersions.props")
PACKAGE_VERSION_FIELD = re.compile(
    r"(<(?P<name>NetWasm[A-Za-z0-9]+PackageVersion)>)(?P<value>[^<]+)(</(?P=name)>)"
)


def run_git(source_root: Path, *arguments: str) -> bytes:
    return subprocess.check_output(
        ["git", "-C", str(source_root), *arguments], stderr=subprocess.STDOUT
    )


def tracked_files(source_root: Path) -> list[Path]:
    output = run_git(source_root, "ls-files", "-z")
    return [source_root / Path(value.decode("utf-8")) for value in output.split(b"\0") if value]


def project_release_fields(
    source_root: Path,
    source_version: str,
    target_version: str,
    files: list[Path],
) -> list[dict[str, object]]:
    available = {path.relative_to(source_root): path for path in files}
    changed: list[dict[str, object]] = []
    version_path = available.get(VERSION_FILE)
    if version_path is None:
        return changed
    if source_version != target_version:
        version_path.write_text(target_version + "\n", encoding="utf-8")
        changed.append({"path": VERSION_FILE.as_posix(), "replacements": 1})

    props_path = available.get(PACKAGE_VERSIONS_FILE)
    if props_path is None:
        return changed
    document = props_path.read_text(encoding="utf-8")
    replacement_count = 0

    def replace(match: re.Match[str]) -> str:
        nonlocal replacement_count
        name = match.group("name")
        value = match.group("value")
        if name == "NetWasmTUnitPackageVersion" or value != source_version:
            return match.group(0)
        replacement_count += 1
        return match.group(1) + target_version + match.group(4)

    projected = PACKAGE_VERSION_FIELD.sub(replace, document)
    if source_version != target_version:
        if replacement_count == 0:
            raise ValueError("No production package-version fields matched the source version.")
        props_path.write_text(projected, encoding="utf-8")
        changed.append(
            {"path": PACKAGE_VERSIONS_FILE.as_posix(), "replacements": replacement_count}
        )
    return changed


def project_version(
    source_root: Path, target_version: str, receipt_path: Path
) -> dict[str, object]:
    if not VERSION_PATTERN.fullmatch(target_version):
        raise ValueError(f"Invalid release version: {target_version}")
    source_root = source_root.resolve()
    if run_git(source_root, "status", "--porcelain=v1").strip():
        raise ValueError("Release-version projection requires a clean Git worktree.")
    source_version = (source_root / VERSION_FILE).read_text(encoding="utf-8").strip()
    if not VERSION_PATTERN.fullmatch(source_version):
        raise ValueError(f"Invalid source release version in {VERSION_FILE}: {source_version}")

    changed = project_release_fields(
        source_root, source_version, target_version, tracked_files(source_root)
    )
    receipt = {
        "schemaVersion": 1,
        "repositoryCommit": run_git(source_root, "rev-parse", "HEAD").decode("ascii").strip(),
        "sourceVersion": source_version,
        "targetVersion": target_version,
        "changedFiles": changed,
        "replacementCount": sum(int(item["replacements"]) for item in changed),
    }
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    arguments = parser.parse_args()
    receipt = project_version(arguments.source_root, arguments.version, arguments.receipt)
    print(
        f"Projected {receipt['sourceVersion']} to {receipt['targetVersion']} "
        f"with {receipt['replacementCount']} replacements in "
        f"{len(receipt['changedFiles'])} tracked files."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
