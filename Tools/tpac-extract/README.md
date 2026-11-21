# tpac-extract

Pull UI textures out of Bannerlord `.tpac` asset packages on Linux without
reaching for Wine or the Modding Kit. Minimum useful scope: extract the vanilla
sprite atlases (`Modules/SandBox/AssetPackages/gauntlet_ui.tpac`) so we can
harvest cells as source material for TOR skill/perk icons.

## Usage

```bash
dotnet build

GAUNTLET="$HOME/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/Modules/SandBox/AssetPackages/gauntlet_ui.tpac"
BIN="bin/Debug/tpac-extract.dll"

# List textures (name, width×height, format)
dotnet "$BIN" "$GAUNTLET" --list

# Extract one texture as PNG into a folder
dotnet "$BIN" "$GAUNTLET" ui_characterdeveloper_1 /tmp/perks

# Keep the intermediate .dds alongside the .png (for inspection)
dotnet "$BIN" "$GAUNTLET" ui_characterdeveloper_1 /tmp/perks --keep-dds
```

Every extraction round-trips through DDS — TpacTool's built-in PNG exporter only
handles uncompressed and DXT1/5 pixel layouts, and the Bannerlord UI atlases are
BC7. `magick` (ImageMagick 7+) decodes BC7 natively, so we export DDS from the
tpac and immediately convert.

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

The .NET runtime dep `lz4net` is pulled from NuGet by the csproj — nothing to
pre-install.

## Explicit non-goals

- TPAC *packing* (writing new tpacs). Covered by
  `ambush-feature/tpac-packer-research.md` in the TORTasks vault — still parked.
- Non-texture assets (meshes, animations, skeletons). Not needed for icons.
- An MCP tool wrapper. Add later if extraction becomes a frequent need.

[fork]: https://github.com/hukai990124-wq/TpacTool
