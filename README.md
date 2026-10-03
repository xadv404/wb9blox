# Angestrap

Angestrap is a fork of [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap), an alternative bootstrapper for Roblox on Windows.

## Download

Latest release: https://github.com/xadv404/wb9blox/releases/latest

## Auto-updates

Angestrap checks GitHub when Roblox starts. With automatic installation enabled, it downloads the newest release, verifies its SHA-256 digest, and applies it while preserving launch arguments. Update checking and automatic installation can be disabled independently in settings. Releases without a GitHub SHA-256 digest or an `Angestrap.exe.sha256` asset are rejected.

The **Follow Roblox deployments** GitHub Actions workflow checks production Roblox deployments approximately hourly. When the deployment changes, it runs regression checks, builds Angestrap on Windows, and publishes a new numbered release with checksums and deployment metadata. A failed check or build prevents publication. This rebuild does not automatically repair source-code incompatibilities or prove that Roblox gameplay works.

The status JSON is served from the latest release:
`https://github.com/xadv404/wb9blox/releases/latest/download/roblox-compatibility.json`
It records the Roblox deployment, Angestrap version, build commit, checksum, and whether an actual client launch was tested. GitHub's existing releases API supplies update availability:
`https://api.github.com/repos/xadv404/wb9blox/releases/latest`

These endpoints gain the new metadata after the updated release workflows are on `main` and a release succeeds. GitHub Actions must be enabled; the provided `GITHUB_TOKEN` supplies release permissions. Scheduled checks are subject to GitHub scheduling delays.

To publish a code update manually, set both `<Version>` and `<FileVersion>` in `Bloxstrap/Bloxstrap.csproj` to a version higher than the latest published release, then push to `main`. The release workflow produces the same checksum and metadata assets. See [regression checks](tests/README.md).

## Features

- Automatic cleanup of obsolete Angestrap and official Roblox versions, preserving active Player/Studio installations and running clients
- Discord Rich Presence
- Custom sky import with preview
- Custom cursor import with preview
- FastFlags editor
- Mod support (fonts, sounds, cursors, etc.)

## Build locally

Requirements: Windows 10/11, .NET 6 SDK, Visual Studio 2022 (optional)

```bash
git clone --recursive https://github.com/xadv404/wb9blox.git
cd wb9blox
dotnet publish .\Bloxstrap\Bloxstrap.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output: `Bloxstrap\bin\Release\net6.0-windows\win-x64\publish\Angestrap.exe`

## Server selection and FastFlag profiles

Open **Serveur à faible ping** from the launch menu, paste a Roblox game link or place ID, then search and join. Angestrap compares up to 300 public, non-full servers with a reported ping. It prefers the lowest ping, then higher server FPS and lower occupancy. Roblox's reported ping is not a measurement from your PC; this tool cannot guarantee a lower personal latency. If the API is unavailable or no ping is supplied, normal Roblox launch remains available.

In FastFlags settings, choose **Roblox par défaut**, **Performances**, **Équilibré**, or **Qualité visuelle**, apply the profile, save settings, and restart Roblox. Profiles adjust only MSAA and texture quality, preserving unrelated custom flags. Roblox may ignore locally configured flags outside its current allowlist. Profiles do not change network traffic or promise a ping/FPS improvement.

## License

MIT — see [LICENSE](LICENSE).
