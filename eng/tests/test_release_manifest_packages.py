#!/usr/bin/env python3

from __future__ import annotations

import json
import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


def built_package_ids() -> set[str]:
    # eng/build-packages.sh packs the projects its root_projects and dependent_projects arrays list.
    script = (ROOT / "eng/build-packages.sh").read_text()
    projects = []
    for name in ("root_projects", "dependent_projects"):
        match = re.search(rf"^{name}=\((.*?)^\)", script, re.M | re.S)
        assert match is not None, f"eng/build-packages.sh has no {name} array"
        projects += match.group(1).split()
    ids = set()
    for project in projects:
        package_id = re.search(r"<PackageId>([^<]+)</PackageId>", (ROOT / project).read_text())
        assert package_id is not None, f"{project} declares no PackageId"
        ids.add(package_id.group(1))
    return ids


class ReleaseManifestPackageTests(unittest.TestCase):
    # The release accepts exactly the manifest's packages, so a project added to or removed from
    # the build without the manifest fails only when a release is cut.
    def test_manifest_lists_exactly_the_packages_the_build_produces(self) -> None:
        manifest = json.loads((ROOT / "eng/release-manifest.json").read_text())
        self.assertEqual(sorted(manifest["packages"]), sorted(built_package_ids()))


if __name__ == "__main__":
    unittest.main()
