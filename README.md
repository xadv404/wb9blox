# Catstrap

Catstrap is a fork of [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap) and [Fishstrap](https://github.com/fishstrap/fishstrap), an alternative bootstrapper for Roblox on Windows.

Brand color: dark rose (`#C2357A`). Logo assets are in [`Images/`](Images/) (same layout as Fishstrap).

## Download

Latest release: https://github.com/xadv404/wb9blox/releases/latest

## Auto-updates

Catstrap checks GitHub for updates when you launch Roblox (enabled by default in settings). When a newer release is published on `main`, the next launch downloads and installs it automatically.

To publish an update, bump the `<Version>` in `Bloxstrap/Bloxstrap.csproj` and push to `main`. GitHub Actions builds `Catstrap.exe` and creates a release.

## Features

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

Output: `Bloxstrap\bin\Release\net6.0-windows\win-x64\publish\Catstrap.exe`

## License

MIT — see [LICENSE](LICENSE).
