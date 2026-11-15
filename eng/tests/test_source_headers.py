#!/usr/bin/env python3
"""NetWasm's own C# sources carry the NetWasm notice, with its link; ported sources keep upstream's notice."""

from __future__ import annotations

import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
NOTICE = (
    "// Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm).",
    "// SPDX-License-Identifier: MIT",
)
UPSTREAM = "// Licensed to the .NET Foundation under one or more agreements."
# Tools ported from upstream, which keep upstream's notice.
PORTED_TOOLS = ("tools/NetWasm.Microsoft.Extensions.Logging.Generator/",)
COPYRIGHT = "Copyright (c) 2026 Zion Sati and NetWasm contributors (https://github.com/zion-sati/NetWasm)"


def is_original(relative: str) -> bool:
    """NetWasm's own code: the tools it wrote, every test, and the generated-activation runtime."""
    if relative.startswith("tools/"):
        return not relative.startswith(PORTED_TOOLS)
    return relative.startswith("tests/") or (relative.startswith("src/") and "/Generated/" in relative)


def missing_notices(root: Path) -> tuple[int, list[str]]:
    checked, missing = 0, []
    for path in sorted(root.rglob("*.cs")):
        relative = path.relative_to(root)
        if {"bin", "obj"} & set(relative.parts) or not is_original(relative.as_posix()):
            continue
        checked += 1
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        if lines[:1] != [UPSTREAM] and tuple(lines[:2]) != NOTICE:
            missing.append(relative.as_posix())
    return checked, missing


class SourceHeaderTests(unittest.TestCase):
    def test_original_sources_carry_the_netwasm_notice(self) -> None:
        checked, missing = missing_notices(ROOT)
        self.assertGreater(checked, 0)
        self.assertEqual(missing, [])

    def test_the_license_names_its_copyright_holder(self) -> None:
        self.assertIn(COPYRIGHT, (ROOT / "LICENSES" / "MIT.txt").read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
