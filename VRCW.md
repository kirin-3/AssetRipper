# VRCW build of AssetRipper

This branch is a modified version of [AssetRipper](https://github.com/AssetRipper/AssetRipper), built for the VRCW world recovery tools.

## Base

Upstream commit [`925e09a91892dc046159162616bb8947a4afab16`](https://github.com/AssetRipper/AssetRipper/commit/925e09a91892dc046159162616bb8947a4afab16) ("Add Buy Me a Coffee and Ko-fi links to index page"). It is unreleased, between 1.3.14 and 2.0.0, and reports version 1.3.14.

## Change

One commit on top of the base, touching only `Source/AssetRipper.Export.UnityProjects/ProjectExporter.cs`. After a project export it also writes:

- `recovery-object-map.json` at the project root: for every object of a serialized file, its source file and path ID, its class, whether its collection exported, and the GUID, file ID and asset type the exporter chose for it.
- `RecoveryCompiledShaders/`: the bundle's compiled shaders as YAML shader assets, with an `index.json`. Shaders built by a Unity version outside the 2022.3 line are only listed.

The export under `Assets/` is unchanged.

## Build

Release `vrcw-1` was built on Windows with the .NET SDK 10.0.302, from a fresh clone of tag `vrcw-1`:

```
git clone -c core.longpaths=true --branch vrcw-1 https://github.com/kirin-3/AssetRipper.git
cd AssetRipper
dotnet publish Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -r win-x64 --self-contained -p:PublishAot=false -o out
copy LICENSE.md out\
```

`out\` then gets a `SOURCE.txt` naming the source tag, and its contents are zipped (files at the zip root) as `AssetRipper-vrcw-1-win-x64.zip`.

## License

GPL-3.0, like upstream. See `LICENSE.md`. The source of release `vrcw-1` is tag [`vrcw-1`](https://github.com/kirin-3/AssetRipper/tree/vrcw-1).
