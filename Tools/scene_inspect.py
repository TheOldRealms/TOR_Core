#!/usr/bin/env python3
"""
Introspect a Bannerlord scene.xscene file.

Usage:
    scene_inspect.py <scene_name>              # auto-resolves by name across all Modules
    scene_inspect.py <path/to/scene.xscene>    # direct path
    scene_inspect.py --list-ambush             # all scenes with an AmbushRetreatZone script
    scene_inspect.py --list-scripts <name>     # all scenes referencing <script name>
    scene_inspect.py --compare <A> <B>         # diff entity/script counts between two scenes
    scene_inspect.py <scene> --entities        # list all entities with positions
    scene_inspect.py <scene> --filter prefab:sp_battle_set        # entities matching a filter
    scene_inspect.py <scene> --filter script:AmbushRetreatZone
    scene_inspect.py <scene> --filter name:Choke
    scene_inspect.py <scene> --bbox                               # min/max coords for the scene
    scene_inspect.py <scene> --distance prefab:sp_battle_set script:AmbushRetreatZone
    scene_inspect.py <scene> --var-filter Radius=10               # script var-value match
    scene_inspect.py <scene> --var-filter ShowDebugMarkerInGame   # var-presence (any value)
    scene_inspect.py <scene> --scripts-by-prefab                  # script density per owning prefab
    scene_inspect.py <scene> --near 595,342,0 --near-radius 50    # restrict to a radius

Filter syntax: "<key>:<value>" where key is one of prefab/name/tag/script and
value is a substring (case-insensitive). Multiple --filter flags AND together.
--var-filter accepts "<Name>" (presence) or "<Name>=<substring>" (value match);
also AND-combined and only meaningful alongside a script filter / --scripts mode.

Scenes live at <ModuleRoot>/SceneObj/<scene_name>/scene.xscene. The tool
walks <bannerlord>/Modules/*/SceneObj/*/scene.xscene by default; override
via --bannerlord-root.
"""

from __future__ import annotations

import argparse
import math
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path
from typing import Iterable, Optional

DEFAULT_BANNERLORD_ROOT = Path.home() / ".local/share/Steam/steamapps/common/Mount & Blade II Bannerlord"


def iter_scene_files(bannerlord_root: Path) -> Iterable[Path]:
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
    direct = Path(name_or_path)
    if direct.is_file() and direct.suffix == ".xscene":
        return direct
    for scene_file in iter_scene_files(bannerlord_root):
        if scene_file.parent.name == name_or_path:
            return scene_file
    return None


def _parse_vec3(s: str) -> Optional[tuple[float, float, float]]:
    if not s:
        return None
    try:
        parts = [float(p.strip()) for p in s.split(",")]
        if len(parts) >= 3:
            return (parts[0], parts[1], parts[2])
    except ValueError:
        pass
    return None


def _collect_entities(root: ET.Element) -> list[dict]:
    """Flatten every game_entity in the tree (including nested children)."""
    out: list[dict] = []
    for ge in root.iter("game_entity"):
        prefab = ge.get("prefab", "")
        name = ge.get("name", "")
        tag = ge.get("tag", "")
        tr = ge.find("transform")
        position = _parse_vec3(tr.get("position", "")) if tr is not None else None
        rotation = tr.get("rotation_euler", "") if tr is not None else ""
        scripts = []
        for script in ge.findall("scripts/script"):
            variables = {v.get("name"): v.get("value") for v in script.findall("variables/variable")}
            scripts.append({"name": script.get("name"), "variables": variables})
        out.append({
            "prefab": prefab,
            "name": name,
            "tag": tag,
            "position": position,
            "rotation": rotation,
            "scripts": scripts,
        })
    return out


def parse_scene(scene_file: Path) -> dict:
    tree = ET.parse(scene_file)
    root = tree.getroot()
    entities = _collect_entities(root)
    return {
        "path": scene_file,
        "name": root.get("name", ""),
        "unique_token": root.get("unique_token", ""),
        "version": root.get("version", ""),
        "revision": root.get("revision", ""),
        "entities": entities,
    }


def _parse_filter(spec: str) -> tuple[str, str]:
    if ":" not in spec:
        raise SystemExit(f"ERROR: filter '{spec}' needs the form <key>:<value>")
    key, _, value = spec.partition(":")
    return key.strip(), value.strip()


def _parse_var_filter(spec: str) -> tuple[str, Optional[str]]:
    """'Radius=10' → ('Radius', '10'); 'Radius' → ('Radius', None)."""
    if "=" in spec:
        name, _, value = spec.partition("=")
        return name.strip(), value.strip()
    return spec.strip(), None


def _entity_in_radius(e: dict, center: tuple[float, float, float], radius: float) -> bool:
    if e["position"] is None:
        return False
    return _distance(e["position"], center) <= radius


def _filter_entities(
    entities: list[dict],
    filters: list[tuple[str, str]],
    var_filters: Optional[list[tuple[str, Optional[str]]]] = None,
    near: Optional[tuple[tuple[float, float, float], float]] = None,
) -> list[dict]:
    var_filters = var_filters or []

    def match_vars(scripts: list[dict]) -> bool:
        # Each var_filter must be satisfied by SOME script on the entity.
        for var_name, value_substr in var_filters:
            vn_l = var_name.lower()
            ok = False
            for s in scripts:
                for k, v in s["variables"].items():
                    if k.lower() != vn_l:
                        continue
                    if value_substr is None or value_substr.lower() in (v or "").lower():
                        ok = True
                        break
                if ok:
                    break
            if not ok:
                return False
        return True

    def match(e: dict) -> bool:
        for key, needle in filters:
            needle_l = needle.lower()
            if key == "prefab":
                if needle_l not in e["prefab"].lower(): return False
            elif key == "name":
                if needle_l not in e["name"].lower(): return False
            elif key == "tag":
                if needle_l not in e["tag"].lower(): return False
            elif key == "script":
                if not any(needle_l in s["name"].lower() for s in e["scripts"]): return False
            else:
                raise SystemExit(f"ERROR: unknown filter key '{key}' (want prefab/name/tag/script)")
        if var_filters and not match_vars(e["scripts"]):
            return False
        if near is not None and not _entity_in_radius(e, near[0], near[1]):
            return False
        return True

    return [e for e in entities if match(e)]


def _entity_label(e: dict) -> str:
    return e["prefab"] or e["name"] or e["tag"] or "<untagged>"


def _print_entity_row(e: dict, show_scripts: bool = True) -> None:
    label = _entity_label(e)
    pos = e["position"]
    pos_str = f"({pos[0]:.1f}, {pos[1]:.1f}, {pos[2]:.1f})" if pos else "(no pos)"
    extra = ""
    if show_scripts and e["scripts"]:
        extra = "  scripts=[" + ", ".join(s["name"] for s in e["scripts"]) + "]"
    print(f"  {label:32}  pos={pos_str}{extra}")


def _distance(a: tuple[float, float, float], b: tuple[float, float, float]) -> float:
    return math.sqrt((a[0]-b[0])**2 + (a[1]-b[1])**2 + (a[2]-b[2])**2)


def _bbox(entities: list[dict]) -> Optional[dict]:
    pts = [e["position"] for e in entities if e["position"]]
    if not pts:
        return None
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    zs = [p[2] for p in pts]
    return {
        "min": (min(xs), min(ys), min(zs)),
        "max": (max(xs), max(ys), max(zs)),
        "size": (max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs)),
        "count": len(pts),
    }


def report_scene(scene: dict, verbose: bool = False) -> None:
    print(f"Scene: {scene['name']}")
    print(f"  path:         {scene['path']}")
    print(f"  version:      {scene['version']}  revision: {scene['revision']}")
    print(f"  unique_token: {scene['unique_token']}")
    print(f"  entities:     {len(scene['entities'])}")

    prefab_counts = Counter(e["prefab"] for e in scene["entities"] if e["prefab"])
    print(f"\n  Prefab histogram ({len(prefab_counts)} distinct, all):")
    for prefab, n in prefab_counts.most_common():
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
            prefab = _entity_label(entity)
            pos = entity["position"]
            pos_str = f"({pos[0]:.1f}, {pos[1]:.1f}, {pos[2]:.1f})" if pos else "(no pos)"
            vars_str = ", ".join(f"{k}={v}" for k, v in script["variables"].items()) if script["variables"] else "<no vars>"
            print(f"    {script['name']:<24}  on {prefab:<32}  at {pos_str}  [{vars_str}]")

    print("\n  Ambush diagnostics:")
    ambush_zones = [(e, s) for e, s in all_scripts if s["name"] == "AmbushRetreatZone"]
    battle_sets = [e for e in scene["entities"] if e["prefab"] == "sp_battle_set"]
    flee_attackers = [e for e in scene["entities"] if e["prefab"] == "flee_line_attacker"]
    flee_defenders = [e for e in scene["entities"] if e["prefab"] == "flee_line_defender"]

    if battle_sets:
        for bs in battle_sets:
            print(f"    - sp_battle_set at {bs['position']}")
    else:
        print("    - MISSING sp_battle_set — field-battle spawn hierarchy absent.")

    for fa in flee_attackers:
        print(f"    - flee_line_attacker at {fa['position']}")
    for fd in flee_defenders:
        print(f"    - flee_line_defender at {fd['position']}")

    if not ambush_zones:
        print("    - No AmbushRetreatZone scripts. Scene relies on the procedural fallback.")
    else:
        for entity, script in ambush_zones:
            radius = script["variables"].get("Radius", "?")
            debug = script["variables"].get("ShowDebugMarkerInGame", "?")
            print(f"    - AmbushRetreatZone at {entity['position']}  Radius={radius}  ShowDebugMarkerInGame={debug}")
            if debug == "true":
                print(f"        WARNING: debug marker enabled — strip before shipping.")
            if battle_sets and entity["position"] and battle_sets[0]["position"]:
                d = _distance(entity["position"], battle_sets[0]["position"])
                print(f"        distance to sp_battle_set: {d:.1f}m")
                if d > 150:
                    print(f"        WARNING: zone is >150m from the battle set — likely off the playable terrain.")

    tactical_regions = [s for _, s in all_scripts if s["name"] == "TacticalRegion"]
    for tr in tactical_regions:
        region_type = tr["variables"].get("tacticalRegionType", "?")
        print(f"    - TacticalRegion: {region_type}")


def report_scripts_by_prefab(entities: list[dict]) -> None:
    """Group script-instance counts by their OWNING entity's prefab.

    For a siege scene with 50+ scripts attached to a handful of tower / gate
    prefabs, this surfaces "which prefabs carry the script load" at a glance,
    which is where debugging usually starts ("this one tower is misbehaving").
    """
    rows: dict[str, dict] = {}
    for e in entities:
        if not e["scripts"]:
            continue
        key = e["prefab"] or e["name"] or e["tag"] or "<untagged>"
        row = rows.setdefault(key, {"instances": 0, "scripts": Counter(), "sample_pos": e["position"]})
        row["instances"] += 1
        for s in e["scripts"]:
            row["scripts"][s["name"]] += 1

    print(f"Scripts by owning prefab ({len(rows)} distinct prefabs with scripts):")
    for key, row in sorted(rows.items(), key=lambda kv: (-sum(kv[1]["scripts"].values()), kv[0])):
        total = sum(row["scripts"].values())
        pos = row["sample_pos"]
        pos_str = f"({pos[0]:.1f}, {pos[1]:.1f}, {pos[2]:.1f})" if pos else "(no pos)"
        scripts_str = ", ".join(f"{n}x {name}" if n > 1 else name
                                for name, n in row["scripts"].most_common())
        print(f"  {key:32} entities={row['instances']:3d}  scripts={total:3d}  sample={pos_str}  [{scripts_str}]")


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
    ap.add_argument("--entities", action="store_true", help="list every entity with position")
    ap.add_argument("--filter", action="append", default=[], metavar="KEY:VALUE",
                    help="narrow entity listing (prefab:/name:/tag:/script:). Repeatable; AND-combined.")
    ap.add_argument("--var-filter", action="append", default=[], dest="var_filter", metavar="NAME[=VALUE]",
                    help="match script variables; 'Radius=10' or 'ShowDebugMarkerInGame'. Repeatable; AND-combined.")
    ap.add_argument("--near", metavar="X,Y,Z", help="restrict to entities within --near-radius of this point")
    ap.add_argument("--near-radius", type=float, default=50.0, help="radius for --near (default 50m)")
    ap.add_argument("--scripts-by-prefab", action="store_true",
                    help="tabulate script-instance counts grouped by owning prefab (siege-debug lens)")
    ap.add_argument("--bbox", action="store_true", help="report bounding box of (filtered) entities")
    ap.add_argument("--distance", nargs=2, metavar=("A", "B"),
                    help="print distance between first entities matching two filter specs")
    ap.add_argument("-v", "--verbose", action="store_true", help="dump every script-instance detail in the summary")
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

        filters = [_parse_filter(f) for f in args.filter]
        var_filters = [_parse_var_filter(v) for v in args.var_filter]
        near = None
        if args.near:
            try:
                xyz = tuple(float(p.strip()) for p in args.near.split(","))
                if len(xyz) != 3:
                    raise ValueError
                near = (xyz, args.near_radius)
            except ValueError:
                raise SystemExit(f"ERROR: --near needs 'X,Y,Z' floats, got '{args.near}'")

        if args.distance:
            fa = _parse_filter(args.distance[0])
            fb = _parse_filter(args.distance[1])
            matches_a = _filter_entities(scene["entities"], [fa])
            matches_b = _filter_entities(scene["entities"], [fb])
            if not matches_a or not matches_b:
                print(f"ERROR: distance needs at least one match per side (got {len(matches_a)} / {len(matches_b)})", file=sys.stderr)
                continue
            a = matches_a[0]
            b = matches_b[0]
            if a["position"] and b["position"]:
                print(f"{_entity_label(a)}  {a['position']}")
                print(f"{_entity_label(b)}  {b['position']}")
                print(f"distance: {_distance(a['position'], b['position']):.1f}m")
            else:
                print("ERROR: one or both matched entities have no position")
            continue

        if args.entities or filters or var_filters or near:
            matches = _filter_entities(scene["entities"], filters, var_filters, near)
            print(f"{len(matches)} entit{'y' if len(matches)==1 else 'ies'}:")
            for e in matches:
                _print_entity_row(e)
            print()

        if args.scripts_by_prefab:
            source = _filter_entities(scene["entities"], filters, var_filters, near)
            report_scripts_by_prefab(source)
            print()

        if args.bbox:
            matches = _filter_entities(scene["entities"], filters, var_filters, near)
            box = _bbox(matches)
            if box:
                print(f"bbox ({box['count']} positioned entities):")
                print(f"  min:  {box['min']}")
                print(f"  max:  {box['max']}")
                print(f"  size: {box['size']}")
            else:
                print("bbox: no positioned entities in the filter")
            print()

        if not (args.entities or filters or var_filters or near or args.bbox or args.distance or args.scripts_by_prefab):
            report_scene(scene, verbose=args.verbose)
            print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
