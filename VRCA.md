# VRCA build of AssetRipper

This branch is a modified version of [AssetRipper](https://github.com/AssetRipper/AssetRipper), built for VRCA avatar recovery.

## Base

Branch `vrca` starts at tag [`vrcw-3`](https://github.com/kirin-3/AssetRipper/tree/vrcw-3), commit `9dd51b0663c53bae76a967bfa1b30da24f6c356f`. It retains the upstream base `925e09a9`, cherry-picked hex fix `12dae7db7`, recovery object map and compiled-shader export, and case-insensitive unique-name fix described in [VRCW.md](VRCW.md). It reports version 1.3.14. VRCW remains on its own branch and release.

## Changes

Both fixes are in `Source/AssetRipper.Processing/AnimationClips/CustomCurveResolver.cs` and preserve a separate exported curve for each binding:

- Constraint arrays take their index from `attribute >> 4`, above the low four field bits, for all six constraint types. This covers source transforms and weights, and ParentConstraint translation and rotation offsets. Unity 2022.3 sample ParentConstraint weights have attributes 15, 31, 47, 63 and 79: field 15 and indexes 0 through 4. The old `>> 8` mapped all five to index 0 and merged their keys. Rebuilding with the four-bit index reproduced the original binding identities in VRCA's round-trip checks.
- Material and blend-shape bindings on unresolved paths use their own attribute in the existing reversible `path_0x<attribute>_<suffix>` name. Previously, every binding on that path used the path name, combining different material channels or blend shapes in one curve. The full material attribute is retained, including its scalar/component flags. This fixes mixed-section curves that VRCA cannot safely split after export.

`AssetRipper.Tests/CustomCurveResolverTests.cs` exercises the five observed constraint attributes, every array field in all six constraint cases (including indexes above 15), and distinct, reversible material and blend-shape names on the same unresolved path. The recovery object map, compiled-shader output, asset names and GUID rules are unchanged.

## Build

Release `vrca-1` is built with .NET SDK **10.0.302** from a fresh clone of its source tag:

```bash
git clone --branch vrca-1 https://github.com/kirin-3/AssetRipper.git AssetRipper-vrca-1
cd AssetRipper-vrca-1
dotnet test Source/AssetRipper.Tests/AssetRipper.Tests.csproj
dotnet build AssetRipper.slnx -c Release
dotnet publish Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -r linux-x64 --self-contained -p:PublishAot=false -o out/linux-x64
dotnet publish Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -r win-x64 --self-contained -p:PublishAot=false -o out/win-x64
```

Each output directory also contains `LICENSE.md` and `SOURCE.txt`, whose contents are `kirin-3/AssetRipper vrca-1 <source commit>`. Its contents are zipped at the archive root as `AssetRipper-vrca-1-linux-x64.zip` or `AssetRipper-vrca-1-win-x64.zip`. These are self-contained portable-runtime builds with AOT disabled. The Windows executable is `AssetRipper.GUI.Free.exe`; the Linux executable is `AssetRipper.GUI.Free`.

## License

GPL-3.0, like upstream. See [LICENSE.md](LICENSE.md). Source for release `vrca-1` is tag [`vrca-1`](https://github.com/kirin-3/AssetRipper/tree/vrca-1).
