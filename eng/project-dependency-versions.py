#!/usr/bin/env python3

"""Update the declared NetWasm and TUnit dependency versions by field."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


VERSION_PATTERN = re.compile(
    r"^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$"
)
DEPENDENCIES_PATH = Path("eng/dependencies.json")
GLOBAL_JSON_PATH = Path("global.json")
PACKAGE_VERSIONS_PATH = Path("eng/NetWasm.PublicPackageVersions.props")


def replace_xml_field(document: str, field: str, value: str) -> str:
    pattern = re.compile(rf"(<{re.escape(field)}>)([^<]+)(</{re.escape(field)}>)")
    projected, count = pattern.subn(rf"\g<1>{value}\g<3>", document)
    if count != 1:
        raise ValueError(f"Expected exactly one {field} field, found {count}.")
    return projected


def read_dependencies(root: Path) -> dict[str, object]:
    dependencies = json.loads((root / DEPENDENCIES_PATH).read_text(encoding="utf-8"))
    if set(dependencies) != {"schemaVersion", "netwasm", "tunit"} or dependencies["schemaVersion"] != 1:
        raise ValueError("Dependency manifest does not match schema version 1.")
    for name in ("netwasm", "tunit"):
        value = dependencies[name]
        if not isinstance(value, str) or not VERSION_PATTERN.fullmatch(value):
            raise ValueError(f"Invalid {name} dependency version: {value}")
    return dependencies


def validate_projection(root: Path) -> None:
    dependencies = read_dependencies(root)
    global_json = json.loads((root / GLOBAL_JSON_PATH).read_text(encoding="utf-8"))
    if global_json.get("msbuild-sdks", {}).get("NetWasm.Sdk") != dependencies["netwasm"]:
        raise ValueError("global.json NetWasm.Sdk does not match eng/dependencies.json.")
    props = (root / PACKAGE_VERSIONS_PATH).read_text(encoding="utf-8")
    match = re.search(
        r"<NetWasmTUnitPackageVersion>([^<]+)</NetWasmTUnitPackageVersion>",
        props,
    )
    if match is None or match.group(1) != dependencies["tunit"]:
        raise ValueError("NetWasmTUnitPackageVersion does not match eng/dependencies.json.")


def project_dependencies(root: Path, netwasm: str, tunit: str) -> list[str]:
    for name, value in (("netwasm", netwasm), ("tunit", tunit)):
        if not VERSION_PATTERN.fullmatch(value):
            raise ValueError(f"Invalid {name} dependency version: {value}")

    dependencies_path = root / DEPENDENCIES_PATH
    dependencies = read_dependencies(root)
    dependencies.update(netwasm=netwasm, tunit=tunit)
    dependencies_path.write_text(json.dumps(dependencies, indent=2) + "\n", encoding="utf-8")

    global_path = root / GLOBAL_JSON_PATH
    global_json = json.loads(global_path.read_text(encoding="utf-8"))
    sdk_versions = global_json.get("msbuild-sdks")
    if not isinstance(sdk_versions, dict) or "NetWasm.Sdk" not in sdk_versions:
        raise ValueError("global.json does not declare NetWasm.Sdk.")
    sdk_versions["NetWasm.Sdk"] = netwasm
    global_path.write_text(json.dumps(global_json, indent=2) + "\n", encoding="utf-8")

    props_path = root / PACKAGE_VERSIONS_PATH
    props_path.write_text(
        replace_xml_field(
            props_path.read_text(encoding="utf-8"),
            "NetWasmTUnitPackageVersion",
            tunit,
        ),
        encoding="utf-8",
    )
    validate_projection(root)
    return [
        DEPENDENCIES_PATH.as_posix(),
        GLOBAL_JSON_PATH.as_posix(),
        PACKAGE_VERSIONS_PATH.as_posix(),
    ]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root", type=Path, default=Path.cwd())
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--netwasm")
    parser.add_argument("--tunit")
    arguments = parser.parse_args()
    root = arguments.source_root.resolve()
    if arguments.check:
        if arguments.netwasm is not None or arguments.tunit is not None:
            parser.error("--check cannot be combined with dependency versions")
        validate_projection(root)
        print("Dependency projections match eng/dependencies.json.")
        return 0
    if arguments.netwasm is None or arguments.tunit is None:
        parser.error("--netwasm and --tunit are required unless --check is used")
    changed = project_dependencies(root, arguments.netwasm, arguments.tunit)
    print(f"Updated NetWasm {arguments.netwasm} and TUnit {arguments.tunit} in {len(changed)} files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
