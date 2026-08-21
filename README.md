# Angestrap

Angestrap is a fork of [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap), an alternative bootstrapper for Roblox on Windows with extra features and customization options.

## About this fork

This project is based on Bloxstrap v2.11.4 and has been rebranded as **Angestrap**. It installs to its own folder (`%LocalAppData%\Angestrap`) and can run alongside Bloxstrap without conflict.

## Features

Inherited from Bloxstrap:

- Discord Rich Presence
- Mod support (cursors, sounds, fonts, etc.)
- FastFlags editor
- Server region information
- Custom bootstrapper themes and appearance options

## Requirements

- Windows 10/11 (64-bit)
- [.NET 6 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/6.0)

## Building

1. Clone with submodules: `git clone --recursive <your-repo-url>`
2. Open `Bloxstrap.sln` in Visual Studio 2022
3. Build in Release configuration

The output executable will be named `Angestrap.exe`.

## License

MIT — see [LICENSE](LICENSE). This fork retains Bloxstrap's original license and third-party attributions.
