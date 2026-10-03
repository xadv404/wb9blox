import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("monitor", Path(__file__).parents[2] / "tools/roblox_update_monitor.py")
monitor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(monitor)


class MonitorTests(unittest.TestCase):
    client = {"version": "0.700.0.123", "clientVersionUpload": "version-1111111111111111"}

    def test_first_release(self):
        plan = monitor.create_plan(self.client, None, "1.1.0", None)
        self.assertTrue(plan["release_needed"])
        self.assertEqual(plan["version"], "1.1.0")

    def test_same_deployment_no_release(self):
        plan = monitor.create_plan(self.client, {"tag_name": "v1.1.3"}, "1.1.0", self.client)
        self.assertFalse(plan["release_needed"])

    def test_new_deployment_increments_published_version(self):
        plan = monitor.create_plan(self.client, {"tag_name": "v1.1.3"}, "1.1.0", {"clientVersionUpload": "version-2222222222222222"})
        self.assertTrue(plan["release_needed"])
        self.assertEqual(plan["version"], "1.1.4")

    def test_new_base_version_preserved(self):
        self.assertEqual(monitor.create_plan(self.client, {"tag_name": "v1.0.33"}, "1.1.0", None)["version"], "1.1.0")

    def test_invalid_roblox_metadata_rejected(self):
        for client in [{}, {"version": "unexpected", "clientVersionUpload": self.client["clientVersionUpload"]},
                       {**self.client, "clientVersionUpload": "../../path"}]:
            with self.subTest(client=client), self.assertRaises(ValueError):
                monitor.create_plan(client, None, "1.1.0", None)

    def test_invalid_release_version_rejected(self):
        with self.assertRaises(ValueError):
            monitor.create_plan(self.client, {"tag_name": "latest"}, "1.1.0", None)

class MonitorFailureTests(unittest.TestCase):
    def test_unavailable_deployment_does_not_create_plan(self):
        import tempfile
        import urllib.error
        from unittest.mock import patch
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "plan.json"
            def unavailable(url, token=None):
                if "api.github.com" in url:
                    return {"tag_name": "v1.1.0", "assets": []}
                raise urllib.error.URLError("fixture unavailable")
            with patch.object(monitor, "get_json", side_effect=unavailable), patch("sys.argv", ["monitor", "--repo", "xadv404/wb9blox", "--output", str(path)]):
                with self.assertRaises(urllib.error.URLError):
                    monitor.main()
            self.assertFalse(path.exists())

    def test_previous_metadata_cannot_come_from_another_host(self):
        from unittest.mock import patch
        def bad_release(url, token=None):
            if "api.github.com" in url:
                return {"tag_name": "v1.1.0", "assets": [{"name": "roblox-compatibility.json", "browser_download_url": "https://example.org/metadata"}]}
            return MonitorTests.client
        with patch.object(monitor, "get_json", side_effect=bad_release), patch("sys.argv", ["monitor", "--repo", "xadv404/wb9blox"]):
            with self.assertRaises(ValueError):
                monitor.main()

class ReleaseMetadataTests(unittest.TestCase):
    def test_checksum_and_metadata_match_fixture(self):
        import hashlib
        import json
        import subprocess
        import sys
        import tempfile
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            exe = root / "Angestrap.exe"
            exe.write_bytes(b"fixture only, not an executable")
            plan = root / "plan.json"
            plan.write_text(json.dumps({"roblox": MonitorTests.client}), encoding="utf-8")
            subprocess.run([sys.executable, str(Path(__file__).parents[2] / "tools/write_release_metadata.py"),
                            "--plan", str(plan), "--exe", str(exe), "--version", "1.1.0", "--commit", "fixture"], check=True)
            expected = hashlib.sha256(exe.read_bytes()).hexdigest()
            self.assertEqual((root / "Angestrap.exe.sha256").read_text().strip(), f"{expected}  Angestrap.exe")
            metadata = json.loads((root / "roblox-compatibility.json").read_text())
            self.assertEqual(metadata["sha256"], expected)
            self.assertEqual(metadata["clientVersionUpload"], MonitorTests.client["clientVersionUpload"])
            self.assertFalse(metadata["clientLaunchTested"])


if __name__ == "__main__":
    unittest.main()
