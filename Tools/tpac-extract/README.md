# tpac-extract

Read Bannerlord `.tpac` asset packages on Linux without reaching for Wine or the
Modding Kit. Covers two asset families:

- **UI textures** — the original scope. Extract DXT/BC7 sprites out to PNG for
  harvesting as source material for TOR skill/perk icons.
- **Skeletal animations** — enumerate + JSON-dump bone-channel data, or export
  to a Blender-loadable `.glb` armature from vanilla + TOR animation packs.
  Unblocks reverse-engineering work for the Linux-native dual-wield mirror
  tool (see
  `TORTasks/linux-debug-ops/task-10-tpac-animation-mirror-tool-linux-native`).

## Usage

```bash
dotnet build

GAUNTLET="$HOME/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/Modules/SandBox/AssetPackages/gauntlet_ui.tpac"
ANIMS="$HOME/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/Modules/Native/AssetPackages/animations.tpac"
BIN="bin/Debug/tpac-extract.dll"

# --- textures ---
# List textures (name, width×height, format)
dotnet "$BIN" "$GAUNTLET" --list

# Extract one texture as PNG into a folder
dotnet "$BIN" "$GAUNTLET" ui_characterdeveloper_1 /tmp/perks

# Keep the intermediate .dds alongside the .png (for inspection)
dotnet "$BIN" "$GAUNTLET" ui_characterdeveloper_1 /tmp/perks --keep-dds

# --- animations ---
# List every skeletal animation (name, bone count, duration, skeleton guid)
dotnet "$BIN" "$ANIMS" --list-anims

# List every animation-clip action (metadata only — sound codes, priority, flags, etc.)
dotnet "$BIN" "$ANIMS" --list-clips

# List every asset-item in a package, grouped by type (quick orientation)
dotnet "$BIN" "$ANIMS" --list-all

# Dump one skeletal animation (root position + per-bone position/rotation keyframes) to JSON
dotnet "$BIN" "$ANIMS" --dump-anim anim_1h_with_shield_stand_idle_1 /tmp/anims

# Export one animation as a Blender-loadable .glb armature
dotnet "$BIN" "$ANIMS" --export-gltf anim_quick_slashleft_1h_balance /tmp/anims/slashleft.glb

# Override fps (default 30) or point at a non-default skeleton package
dotnet "$BIN" "$ANIMS" --export-gltf anim_quick_slashleft_1h_balance /tmp/anims/slashleft.glb --fps 24 --skeletons "$HOME/.../skeletons.tpac"
```

Texture extraction round-trips through DDS — TpacTool's built-in PNG exporter
only handles uncompressed and DXT1/5 pixel layouts, and the Bannerlord UI
atlases are BC7. `magick` (ImageMagick 7+) decodes BC7 natively, so we export
DDS from the tpac and immediately convert.

Animation dumps are plain JSON — one file per animation, carrying the per-bone
position/rotation keyframes plus root-position/scale frames. Shape mirrors the
`AnimationDefinitionData` struct in `TpacTool.Lib`.

glTF export produces a skeleton-only armature (no mesh) with one animation
channel per bone plus a root-displacement channel. `SkeletalAnimation.Skeleton`
is a canonical engine ID that doesn't resolve to a Skeleton asset directly;
instead the exporter picks a skeleton from `skeletons.tpac` by bone-count match
(preferring canonical biped skeletons named after `bip01_pelvis`). Override
with `--skeleton-name <name>` if you need a specific one. Open the `.glb` in
Blender (File → Import → glTF 2.0) to inspect the animation.

## Requirements

- .NET SDK 8 or newer (any modern distro's `dotnet-sdk` package)
- ImageMagick 7+ (`dnf install ImageMagick`) — the `magick` binary must be on
  PATH

## Slicing an atlas into per-perk cells

The vanilla perk atlas `ui_characterdeveloper_1.png` is a 4096×2048 grid of
(roughly) 128×128 cells. The exact cell layout is stored in
`Modules/SandBox/GUI/SandBoxSpriteData.xml` — each `<Sprite>` entry gives the
pixel-space rectangle for a named icon. Pull the cells you want:

```bash
# Example: crop one 128×128 cell at (0, 0)
magick /tmp/perks/ui_characterdeveloper_1.png -crop 128x128+0+0 cell_00.png
```

For a batch slice driven by the game's own sprite data, read the XML and feed
each `<Sprite Name="..." Rect="x y w h" />` into `-crop`.

## Vendored dependencies (`lib/`)

- `TpacTool.Lib.dll`, `TpacTool.IO.dll` — [hukai990124-wq/TpacTool][fork] fork
  at commit `d0eb9d3`, Release/net6.0.
- `TpacTool.Lib.dll` carries a local patch (`asset-metadata-size-skip.patch`,
  reproducible per `lib/REBUILD.txt`). Without it the loader hits an
  `EndOfStreamException` partway through `gauntlet_ui.tpac` on current
  Bannerlord builds, because the per-asset-metadata block has grown trailing
  fields the upstream parser doesn't recognise.
- `LICENSE.TpacTool` — upstream MIT.

.NET runtime deps are pulled from NuGet by the csproj — nothing to
pre-install:

- `lz4net` — TpacTool's compression codec.
- `SharpGLTF.Toolkit` (MIT) — glTF writer. Only used by `--export-gltf`.

## Explicit non-goals

- TPAC *packing* (writing new tpacs). Covered by
  `ambush-feature/tpac-packer-research.md` + `linux-debug-ops/task-6-c-tpac-writer-in-tortoolscore`
  in the TORTasks vault.
- Mesh asset-items beyond the metadata list `--list-all` shows. TpacTool
  parses them but we have no immediate use. Note the current glTF export
  is skeleton-only; no skinned geometry.
- The actual bone-mirror pass. Still open under
  `linux-debug-ops/task-10-tpac-animation-mirror-tool-linux-native` — this tool
  provides the read side so that work can be driven from Linux.
- An MCP tool wrapper. Add later if extraction becomes a frequent need.

[fork]: https://github.com/hukai990124-wq/TpacTool
