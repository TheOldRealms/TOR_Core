#!/usr/bin/env python3
"""
Introspect a Bannerlord scene.xscene file.

Usage:
    scene_inspect.py <scene_name>              # auto-resolves by name across all Modules
    scene_inspect.py <path/to/scene.xscene>    # direct path
    scene_inspect.py --list-ambush             # all scenes with an AmbushRetreatZone script
    scene_inspect.py --list-scripts <name>     # all scenes referencing <script name>
    scene_inspect.py --compare <A> <B>         # diff entity/script counts between two scenes

Scenes live at <ModuleRoot>/SceneObj/<scene_name>/scene.xscene. The tool
walks /home/<user>/.local/share/Steam/steamapps/common/Mount & Blade II
Bannerlord/Modules/*/SceneObj/*/scene.xscene by default; override via
--bannerlord-root.

Output is grouped: metadata, prefab histogram (top 10), all scripts
with their variables, tags, and ambush-specific flags (retreat-zone
presence, zone radius, scene-level warnings).
"""

from __future__ import annotations

import argparse
import os
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path
from typing import Iterable, Optional

DEFAULT_BANNERLORD_ROOT = Path.home() / ".local/share/Steam/steamapps/common/Mount & Blade II Bannerlord"


def iter_scene_files(bannerlord_root: Path) -> Iterable[Path]:
    """Yield every scene.xscene under <bannerlord>/Modules/*/SceneObj/*/."""
    modules_dir = bannerlord_root / "Modules"
    if not modules_dir.is_dir():
        return
    for module_dir in sorted(modules_dir.iterdir()):
        scene_obj = module_dir / "SceneObj"
        if not scene_obj.is_dir():
            continue
        for scene_dir in sorted(scene_obj.iterdir()):
            scene_file = scene_dir / "scene.xscene"
            if scene_file.is_file():
                yield scene_file


def resolve_scene(name_or_path: str, bannerlord_root: Path) -> Optional[Path]:
    """Accept a direct scene.xscene path or a bare scene name; return the file path."""
    direct = Path(name_or_path)
    if direct.is_file() and direct.suffix == ".xscene":
        return direct
    for scene_file in iter_scene_files(bannerlord_root):
        if scene_file.parent.name == name_or_path:
            return scene_file
    return None


def parse_scene(scene_file: Path) -> dict:
    """Return a structured dict describing the scene's contents."""
    tree = ET.parse(scene_file)
    root = tree.getroot()
    entities = []
    for ge in root.iter("game_entity"):
        prefab = ge.get("prefab", "")
        name = ge.get("name", "")
        tag = ge.get("tag", "")
        scripts = []
        for script in ge.iter("script"):
            variables = {v.get("name"): v.get("value") for v in script.findall("variables/variable")}
            scripts.append({"name": script.get("name"), "variables": variables})
        entities.append({"prefab": prefab, "name": name, "tag": tag, "scripts": scripts})
    return {
        "path": scene_file,
        "name": root.get("name", ""),
        "unique_token": root.get("unique_token", ""),
        "version": root.get("version", ""),
        "revision": root.get("revision", ""),
        "entities": entities,
    }


def report_scene(scene: dict, verbose: bool = False) -> None:
    print(f"Scene: {scene['name']}")
    print(f"  path:         {scene['path']}")
    print(f"  version:      {scene['version']}  revision: {scene['revision']}")
    print(f"  unique_token: {scene['unique_token']}")
    print(f"  entities:     {len(scene['entities'])}")

    prefab_counts = Counter(e["prefab"] for e in scene["entities"] if e["prefab"])
    print(f"\n  Top prefabs ({len(prefab_counts)} distinct):")
    for prefab, n in prefab_counts.most_common(10):
        print(f"    {n:3d}  {prefab}")

    tags = sorted({e["tag"] for e in scene["entities"] if e["tag"]})
    if tags:
        print(f"\n  Tags: {', '.join(tags)}")

    all_scripts = [(e, s) for e in scene["entities"] for s in e["scripts"]]
    script_counts = Counter(s["name"] for _, s in all_scripts)
    print(f"\n  Scripts ({len(all_scripts)} instance(s), {len(script_counts)} distinct):")
    for script_name, n in script_counts.most_common():
        print(f"    {n:3d}  {script_name}")

    if verbose or any(s["name"] == "AmbushRetreatZone" for _, s in all_scripts):
        print("\n  Script instance detail:")
        for entity, script in all_scripts:
            prefab = entity["prefab"] or "<no prefab>"
            vars_str = ", ".join(f"{k}={v}" for k, v in script["variables"].items()) if script["variables"] else "<no vars>"
            print(f"    {script['name']:<28}  on {prefab:<32}  [{vars_str}]")

    ambush_zones = [s for _, s in all_scripts if s["name"] == "AmbushRetreatZone"]
    print("\n  Ambush diagnostics:")
    if not ambush_zones:
        print("    - No AmbushRetreatZone scripts. Scene relies on the procedural fallback.")
    else:
        print(f"    - {len(ambush_zones)} AmbushRetreatZone script(s) found.")
        for z in ambush_zones:
            radius = z["variables"].get("Radius", "?")
            debug = z["variables"].get("ShowDebugMarkerInGame", "?")
            print(f"      - Radius={radius}  ShowDebugMarkerInGame={debug}")
            if debug == "true":
                print(f"        WARNING: debug marker enabled — strip before shipping.")

    tactical_regions = [s for _, s in all_scripts if s["name"] == "TacticalRegion"]
    if tactical_regions:
        for tr in tactical_regions:
            region_type = tr["variables"].get("tacticalRegionType", "?")
            print(f"    - TacticalRegion: {region_type}")


def list_with_script(script_name: str, bannerlord_root: Path) -> None:
    print(f"Scenes containing <script name=\"{script_name}\">:\n")
    for scene_file in iter_scene_files(bannerlord_root):
        try:
            root = ET.parse(scene_file).getroot()
        except ET.ParseError:
            continue
        found = any(s.get("name") == script_name for s in root.iter("script"))
        if found:
            print(f"  {scene_file.parent.name:<40}  ({scene_file})")


def compare(scene_a: dict, scene_b: dict) -> None:
    print(f"Comparing {scene_a['name']}  vs  {scene_b['name']}\n")
    counts_a = Counter(e["prefab"] for e in scene_a["entities"] if e["prefab"])
    counts_b = Counter(e["prefab"] for e in scene_b["entities"] if e["prefab"])
    diff = (counts_b - counts_a) + (counts_a - counts_b)
    print("Entity-count delta (prefabs appearing a different number of times):")
    for prefab in sorted(diff):
        print(f"  {prefab:<40}  A={counts_a.get(prefab, 0):4d}  B={counts_b.get(prefab, 0):4d}")

    scripts_a = Counter(s["name"] for e in scene_a["entities"] for s in e["scripts"])
    scripts_b = Counter(s["name"] for e in scene_b["entities"] for s in e["scripts"])
    sdiff = (scripts_b - scripts_a) + (scripts_a - scripts_b)
    if sdiff:
        print("\nScript-count delta:")
        for name in sorted(sdiff):
            print(f"  {name:<40}  A={scripts_a.get(name, 0):4d}  B={scripts_b.get(name, 0):4d}")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("args", nargs="*", help="scene name or path(s)")
    ap.add_argument("--bannerlord-root", type=Path, default=DEFAULT_BANNERLORD_ROOT)
    ap.add_argument("--list-ambush", action="store_true", help="list every scene with an AmbushRetreatZone")
    ap.add_argument("--list-scripts", metavar="NAME", help="list every scene referencing a script by name")
    ap.add_argument("--compare", nargs=2, metavar=("A", "B"), help="diff two scenes")
    ap.add_argument("-v", "--verbose", action="store_true", help="dump every script-instance detail")
    args = ap.parse_args()

    if args.list_ambush:
        list_with_script("AmbushRetreatZone", args.bannerlord_root)
        return 0
    if args.list_scripts:
        list_with_script(args.list_scripts, args.bannerlord_root)
        return 0
    if args.compare:
        a = resolve_scene(args.compare[0], args.bannerlord_root)
        b = resolve_scene(args.compare[1], args.bannerlord_root)
        if not a or not b:
            print("ERROR: couldn't resolve one or both scenes.", file=sys.stderr)
            return 1
        compare(parse_scene(a), parse_scene(b))
        return 0

    if not args.args:
        ap.print_help()
        return 1

    for target in args.args:
        resolved = resolve_scene(target, args.bannerlord_root)
        if resolved is None:
            print(f"ERROR: scene not found: {target}", file=sys.stderr)
            continue
        try:
            scene = parse_scene(resolved)
        except ET.ParseError as e:
            print(f"ERROR parsing {resolved}: {e}", file=sys.stderr)
            continue
        report_scene(scene, verbose=args.verbose)
        print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
