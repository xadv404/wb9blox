#!/usr/bin/env python3
"""Create release checksum and deployment metadata after a successful build."""
import argparse
import hashlib
import json
from pathlib import Path
from datetime import datetime, timezone

parser = argparse.ArgumentParser()
parser.add_argument("--plan", default="roblox-update-plan.json")
parser.add_argument("--exe", required=True)
parser.add_argument("--version", required=True)
parser.add_argument("--commit", required=True)
args = parser.parse_args()
exe = Path(args.exe)
plan = json.loads(Path(args.plan).read_text(encoding="utf-8"))
if not exe.is_file():
    raise FileNotFoundError(exe)
checksum = hashlib.sha256(exe.read_bytes()).hexdigest()
exe.with_name(exe.name + ".sha256").write_text(f"{checksum}  {exe.name}\n", encoding="ascii")
metadata = {**plan["roblox"], "angestrapVersion": args.version, "commit": args.commit,
            "windowsBuildPassed": True, "clientLaunchTested": False,
            "checkedAtUtc": datetime.now(timezone.utc).isoformat(), "sha256": checksum}
exe.with_name("roblox-compatibility.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")
