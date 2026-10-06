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

## Build

Release `vrcw-2` is built on Windows with the .NET SDK 10.0.302, from a fresh clone of tag `vrcw-2`:

```
git clone -c core.longpaths=true --branch vrcw-2 https://github.com/kirin-3/AssetRipper.git
cd AssetRipper
dotnet publish Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -r win-x64 --self-contained -p:PublishAot=false -o out
copy LICENSE.md out\
```

`out\` then gets a `SOURCE.txt` naming the source tag, and its contents are zipped (files at the zip root) as `AssetRipper-vrcw-2-win-x64.zip`.

## License

GPL-3.0, like upstream. See `LICENSE.md`. The source of release `vrcw-2` is tag [`vrcw-2`](https://github.com/kirin-3/AssetRipper/tree/vrcw-2).
