#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).parents[1] / "project-release-version.py"
SPEC = importlib.util.spec_from_file_location("project_release_version", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class ProjectReleaseVersionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        subprocess.run(["git", "init", "--quiet", str(self.root)], check=True)
        subprocess.run(["git", "-C", str(self.root), "config", "user.name", "Test"], check=True)
        subprocess.run(
            ["git", "-C", str(self.root), "config", "user.email", "test@example.invalid"],
            check=True,
        )
        (self.root / "eng").mkdir()
        (self.root / "src/Example").mkdir(parents=True)
        (self.root / "eng/NetWasm.ReleaseVersion.txt").write_text("0.1.0\n")
        (self.root / "eng/NetWasm.PublicPackageVersions.props").write_text(
            "Package=0.1.0\nInternalDependency=0.1.0\n"
            "TUnitDependency=0.2.1\nAssembly=0.1.0.4\n"
        )
        (self.root / "global.json").write_text('{"msbuild-sdks":{"NetWasm.Sdk":"0.1.0"}}\n')
        (self.root / "src/Example/protocol.wit").write_text("package wasi:io@0.1.0;\n")
        subprocess.run(["git", "-C", str(self.root), "add", "."], check=True)
        subprocess.run(["git", "-C", str(self.root), "commit", "--quiet", "-m", "fixture"], check=True)

    def project(self, version: str) -> dict[str, object]:
        return MODULE.project_version(self.root, version, self.root.parent / "receipt.json")

    def test_projects_only_package_version_inputs(self) -> None:
        receipt = self.project("0.2.0-preview.1")

        self.assertEqual("0.2.0-preview.1\n", (self.root / "eng/NetWasm.ReleaseVersion.txt").read_text())
        self.assertIn("Package=0.2.0-preview.1", (self.root / "eng/NetWasm.PublicPackageVersions.props").read_text())
        self.assertIn("InternalDependency=0.2.0-preview.1", (self.root / "eng/NetWasm.PublicPackageVersions.props").read_text())
        self.assertIn("TUnitDependency=0.2.1", (self.root / "eng/NetWasm.PublicPackageVersions.props").read_text())
        self.assertIn('"NetWasm.Sdk":"0.2.0-preview.1"', (self.root / "global.json").read_text())
        self.assertEqual("package wasi:io@0.1.0;\n", (self.root / "src/Example/protocol.wit").read_text())
        self.assertEqual(4, receipt["replacementCount"])

    def test_preserves_larger_dotted_numeric_tokens(self) -> None:
        self.project("0.2.0")

        props = (self.root / "eng/NetWasm.PublicPackageVersions.props").read_text()
        self.assertIn("Assembly=0.1.0.4", props)

    def test_rejects_invalid_version(self) -> None:
        with self.assertRaisesRegex(ValueError, "Invalid release version"):
            self.project("not-a-version")


if __name__ == "__main__":
    unittest.main()
