#!/usr/bin/env python3
"""Read Roblox deployment metadata and plan a tested Angestrap rebuild.

No repository mutation or release publication happens in this script.
"""
import argparse
import json
import os
from pathlib import Path
import re
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET


def get_json(url, token=None):
    headers = {"User-Agent": "Angestrap-Roblox-Update-Monitor", "Accept": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    request = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def version_tuple(value):
    if not isinstance(value, str) or not re.fullmatch(r"v?\d+\.\d+\.\d+", value):
        raise ValueError("Invalid Angestrap version")
    return tuple(map(int, value.lstrip("v").split(".")))


def create_plan(client, latest, base_version, previous):
    guid = client.get("clientVersionUpload")
    version = client.get("version")
    if not isinstance(guid, str) or not re.fullmatch(r"version-[a-fA-F0-9]{16}", guid):
        raise ValueError("Invalid Roblox deployment identifier")
    if not isinstance(version, str) or not re.fullmatch(r"\d+(?:\.\d+){2,3}", version):
        raise ValueError("Invalid Roblox client version")
    base = version_tuple(base_version)
    latest_version = version_tuple(latest["tag_name"]) if latest else None
    next_version = base
    if latest_version and latest_version >= base:
        next_version = (latest_version[0], latest_version[1], latest_version[2] + 1)
    needed = previous is None or previous.get("clientVersionUpload") != guid
    return {
        "release_needed": needed,
        "version": ".".join(map(str, next_version)),
        "roblox": {"version": version, "clientVersionUpload": guid, "channel": "production"},
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True)
    parser.add_argument("--output", default="roblox-update-plan.json")
    args = parser.parse_args()
    if not re.fullmatch(r"[\w.-]+/[\w.-]+", args.repo):
        raise ValueError("Invalid repository name")
    token = os.environ.get("GH_TOKEN")
    latest = None
    try:
        latest = get_json(f"https://api.github.com/repos/{args.repo}/releases/latest", token)
    except urllib.error.HTTPError as error:
        if error.code != 404:
            raise
    # A metadata fetch failure must not manufacture a new deployment or publish a release.
    try:
        client = get_json("https://clientsettingscdn.roblox.com/v2/client-version/WindowsPlayer")
    except (urllib.error.URLError, TimeoutError):
        client = get_json("https://clientsettings.roblox.com/v2/client-version/WindowsPlayer")
    previous = None
    if latest:
        assets = [asset for asset in latest.get("assets", []) if asset.get("name") == "roblox-compatibility.json"]
        if assets:
            url = assets[0]["browser_download_url"]
            expected_prefix = f"https://github.com/{args.repo}/releases/download/"
            if not url.startswith(expected_prefix):
                raise ValueError("Unexpected release metadata URL")
            previous = get_json(url)
            if not isinstance(previous, dict) or not isinstance(previous.get("clientVersionUpload"), str):
                raise ValueError("Invalid previous deployment metadata")
    project = ET.parse("Bloxstrap/Bloxstrap.csproj").getroot()
    base_version = project.findtext("PropertyGroup/Version")
    plan = create_plan(client, latest, base_version, previous)
    Path(args.output).write_text(json.dumps(plan, indent=2) + "\n", encoding="utf-8")
    print(f"Roblox {plan['roblox']['version']}: rebuild {'needed' if plan['release_needed'] else 'not needed'}")
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
            output.write(f"needed={str(plan['release_needed']).lower()}\nversion={plan['version']}\n")


if __name__ == "__main__":
    main()
