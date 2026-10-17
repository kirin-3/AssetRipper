# VRCW build of AssetRipper

This branch is a modified version of [AssetRipper](https://github.com/AssetRipper/AssetRipper), built for the VRCW world recovery tools.

## Base

Upstream commit [`925e09a91892dc046159162616bb8947a4afab16`](https://github.com/AssetRipper/AssetRipper/commit/925e09a91892dc046159162616bb8947a4afab16) ("Add Buy Me a Coffee and Ko-fi links to index page"). It is unreleased, between 1.3.14 and 2.0.0, and reports version 1.3.14.

## Changes

The object-map change (`7d5c63153`) touches only `Source/AssetRipper.Export.UnityProjects/ProjectExporter.cs`. After a project export it also writes:

- `recovery-object-map.json` at the project root: for every object of a serialized file, its source file and path ID, its class, whether its collection exported, and the GUID, file ID and asset type the exporter chose for it.
- `RecoveryCompiledShaders/`: the bundle's compiled shaders as YAML shader assets, with an `index.json`. Shaders built by a Unity version outside the 2022.3 line are only listed.

The object-map change leaves the export under `Assets/` unchanged.

Upstream's [hex-encoding fix `12dae7db7`](https://github.com/AssetRipper/AssetRipper/commit/12dae7db7) is cherry-picked onto this base. It writes negative integers in hex-encoded YAML fields as their two's-complement bytes, including light-probe occlusion indices and Avatar bone IDs. It changes `ReverseHexString.cs` and adds regression tests in `YamlScalarNodeTests.cs`. No other upstream changes are included.

The case-insensitive unique-name change makes `FileSystem.GetUniqueName` treat a name as taken when the folder already holds an entry equal to it under `OrdinalIgnoreCase`, through a per-folder entry cache that also records every name the method returns. On a case-sensitive file system the export then picks the same names as on Windows, `_<n>` suffixes included, so no two exported entries differ only in case. Windows output is unchanged. It changes `Source/AssetRipper.IO.Files/FileSystem.cs` and adds a regression test in `GetUniqueNameTests.cs`.

## Build

Release `vrcw-3` is built from a fresh clone of tag `vrcw-3`. On Windows, with the .NET SDK 10.0.302:

```
git clone -c core.longpaths=true --branch vrcw-3 https://github.com/kirin-3/AssetRipper.git
cd AssetRipper
dotnet publish Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -r win-x64 --self-contained -p:PublishAot=false -o out
copy LICENSE.md out\
```

`out\` then gets a `SOURCE.txt` naming the source tag, and its contents are zipped (files at the zip root) as `AssetRipper-vrcw-3-win-x64.zip`.

The Linux archive is built on Ubuntu 22.04 by `tools/build_linux_forks.sh` in the VRCW repository, which publishes the same project with `-r linux-x64` and zips it as `AssetRipper-vrcw-3-linux-x64.zip`.

## License

GPL-3.0, like upstream. See `LICENSE.md`. The source of release `vrcw-3` is tag [`vrcw-3`](https://github.com/kirin-3/AssetRipper/tree/vrcw-3).
