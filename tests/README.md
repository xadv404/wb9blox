# Compatibility and version cleanup checks

Run from the repository root with the pinned .NET 6.0.428 SDK:

```sh
dotnet run --project tests/PackageManifestTests/PackageManifestTests.csproj
dotnet run --project tests/VersionCleanupTests/VersionCleanupTests.csproj
dotnet run --project tests/FeatureTests/FeatureTests.csproj
dotnet run --project tests/FastFlagWorkflowTests/FastFlagWorkflowTests.csproj
python3 -m unittest discover -s tests/update_monitor
```

These dependency-free executable checks compile the application's actual manifest
parser and cleanup helper. They fail with a nonzero exit status on a regression.
Cleanup checks use temporary folders and verify retained installations, obsolete
folders, locked executables, unrecognized folders, symbolic links and failure handling.
Symbolic-link checks require permission to create links (Linux, or Windows with
Developer Mode / suitable privileges).

Windows integration still needs validation: registry detection of the installed
Player and Studio, preservation of running Roblox processes, and cleanup after an
actual update. Closed obsolete versions are retried at the next Angestrap launch.
If registration or process inspection is unavailable, cleanup conservatively keeps
potentially active versions. Functional Roblox compatibility requires access to
Roblox's deployment endpoints and a Windows client launch.

Feature checks exercise server ranking, pagination, rate limits, cancellation, game URL validation, profile merging/reset, checksum parsing, and tampering rejection. Monitor tests cover version increments, unchanged deployments, unavailable metadata, and generated checksums.

An optional read-only live check is:

```sh
dotnet run --project tests/FeatureTests/FeatureTests.csproj -- --live-place 2753915549
python3 tools/roblox_update_monitor.py --repo xadv404/wb9blox --output /tmp/roblox-update-plan.json
```

The monitor prepares a plan only; it never publishes releases locally. Verify actual server joining and updater relaunch on Windows. GitHub Actions publication and scheduled execution are separate from local tests and have not been exercised merely by running these commands.

FastFlag workflow checks compile the actual profile view model, flag manager, and JSON persistence classes. They verify apply/save/reload, preservation of custom flags, restoration after reset, selection when reopening the page, and failed-save reporting. Desktop-only notifications are stubbed; saving and loading use real temporary files. On Windows, also verify that the preset toggle starts enabled in the flag editor and that applying a profile saves from the main settings window.
