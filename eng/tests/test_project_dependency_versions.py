#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).parents[1] / "project-dependency-versions.py"
SPEC = importlib.util.spec_from_file_location("project_dependency_versions", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class ProjectDependencyVersionsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / "eng").mkdir()
        (self.root / "eng/dependencies.json").write_text(
            '{"schemaVersion":1,"netwasm":"0.4.3","tunit":"0.4.3"}\n'
        )
        (self.root / "global.json").write_text(
            '{"sdk":{"version":"10.0.401"},"msbuild-sdks":{"NetWasm.Sdk":"0.4.3"}}\n'
        )
        (self.root / "eng/NetWasm.PublicPackageVersions.props").write_text(
            "<Project><PropertyGroup>"
            "<NetWasmSystemLinqPackageVersion>0.1.0</NetWasmSystemLinqPackageVersion>"
            "<NetWasmTUnitPackageVersion>0.4.3</NetWasmTUnitPackageVersion>"
            "</PropertyGroup></Project>\n"
        )

    def test_updates_only_dependency_fields(self) -> None:
        MODULE.project_dependencies(self.root, "0.5.0", "0.5.1-preview.1")

        dependencies = json.loads((self.root / "eng/dependencies.json").read_text())
        self.assertEqual("0.5.0", dependencies["netwasm"])
        self.assertEqual("0.5.1-preview.1", dependencies["tunit"])
        global_json = json.loads((self.root / "global.json").read_text())
        self.assertEqual("0.5.0", global_json["msbuild-sdks"]["NetWasm.Sdk"])
        props = (self.root / "eng/NetWasm.PublicPackageVersions.props").read_text()
        self.assertIn("<NetWasmTUnitPackageVersion>0.5.1-preview.1<", props)
        self.assertIn("<NetWasmSystemLinqPackageVersion>0.1.0<", props)

    def test_rejects_invalid_versions(self) -> None:
        with self.assertRaisesRegex(ValueError, "Invalid netwasm"):
            MODULE.project_dependencies(self.root, "latest", "0.5.0")

    def test_detects_projection_drift(self) -> None:
        dependencies = json.loads((self.root / "eng/dependencies.json").read_text())
        dependencies["netwasm"] = "0.5.0"
        (self.root / "eng/dependencies.json").write_text(json.dumps(dependencies))

        with self.assertRaisesRegex(ValueError, "global.json"):
            MODULE.validate_projection(self.root)


if __name__ == "__main__":
    unittest.main()
