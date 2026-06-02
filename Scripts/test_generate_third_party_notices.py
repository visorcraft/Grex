#!/usr/bin/env python3
"""Tests for generate_third_party_notices.py.

Run from the repo root: python Scripts/test_generate_third_party_notices.py
or as a module:        python -m unittest Scripts.test_generate_third_party_notices
"""
import os
import sys
import unittest

# Ensure Scripts/ is importable regardless of how the test is invoked
# (direct, `-m unittest` module mode, or pytest) — mirrors test_add_localization_entry.py.
sys.path.insert(0, os.path.dirname(__file__))

import generate_third_party_notices as gen


def good_manifest():
    return {
        "schemaVersion": 1,
        "licenses": {"MIT": "The MIT License\n\nfull license body text\n"},
        "components": [
            {
                "name": "Sample.Lib",
                "version": "1.0.0",
                "license": "MIT",
                "copyright": "Copyright (c) Example",
                "url": "https://example.com",
                "category": "library",
            },
        ],
    }


class ValidateTests(unittest.TestCase):
    def test_accepts_good_manifest(self):
        self.assertEqual(gen.validate(good_manifest()), [])

    def test_rejects_bad_schema_version(self):
        m = good_manifest()
        m["schemaVersion"] = 2
        self.assertTrue(any("schemaVersion" in e for e in gen.validate(m)))

    def test_rejects_unknown_license_key(self):
        m = good_manifest()
        m["components"][0]["license"] = "NOPE"
        self.assertTrue(any("unknown license key" in e for e in gen.validate(m)))

    def test_rejects_empty_license_text(self):
        m = good_manifest()
        m["licenses"]["MIT"] = "   "
        self.assertTrue(any("empty" in e for e in gen.validate(m)))

    def test_rejects_missing_required_field(self):
        m = good_manifest()
        m["components"][0]["url"] = ""
        self.assertTrue(any("url" in e for e in gen.validate(m)))


class RenderTests(unittest.TestCase):
    def test_render_is_deterministic_lf_single_trailing_newline(self):
        m = good_manifest()
        out1 = gen.render(m)
        out2 = gen.render(m)
        self.assertEqual(out1, out2)
        self.assertTrue(out1.endswith("\n"))
        self.assertFalse(out1.endswith("\n\n"))
        self.assertNotIn("\r", out1)
        self.assertIn("Sample.Lib v1.0.0 (MIT)", out1)
        self.assertIn("full license body text", out1)
        self.assertIn("Copyright (c) Example", out1)


class RealManifestTests(unittest.TestCase):
    def test_real_manifest_valid_and_renders(self):
        mpath = gen.manifest_path()
        if not mpath.exists():
            self.skipTest("manifest not found")
        manifest = gen.load_manifest(mpath)
        self.assertEqual(gen.validate(manifest), [])
        out = gen.render(manifest)
        self.assertTrue(out.endswith("\n"))
        self.assertNotIn("\r", out)


if __name__ == "__main__":
    unittest.main()
