#!/usr/bin/env python3
"""
Attach an AmbushSpawnPointScript to every `attacker_ambush_spawnpoint`
entity in a Bannerlord scene.xscene file.

The script component is what the ambush UI reads at runtime to cap how
many formations may stack on the same spot (MaxFormations) and how wide
the per-spot troop layout may spread (MaxWidth). Zero means unlimited for
both fields. Pass different defaults with the flags below.

Idempotent: if an entity already has an AmbushSpawnPointScript, the script
is left alone (values not overwritten) so repeated runs after hand-edits
in the in-game Scene Editor don't clobber scener tuning. Formatting is
preserved by walking the file as text and only inserting the new <scripts>
block in-place; nothing else is reserialised.

Usage:
    python3 ambush_attach_spawnpoint_script.py <path/to/scene.xscene>
    python3 ambush_attach_spawnpoint_script.py <path> --max-formations 2
    python3 ambush_attach_spawnpoint_script.py <path> --marker <tag>
"""
import argparse
import re
import sys
from pathlib import Path
from lxml import etree

SCRIPT_CLASS_NAME = "AmbushSpawnPointScript"
DEFAULT_MARKER_TAG = "attacker_ambush_spawnpoint"


def detect_indent(text, position):
    """
    Walk back from position to the start of its line to return the leading
    whitespace. Used to match sibling indentation when we inject <scripts>.
    """
    line_start = text.rfind("\n", 0, position)
    if line_start == -1:
        return ""
    ws_end = line_start + 1
    while ws_end < position and text[ws_end] in " \t":
        ws_end += 1
    return text[line_start + 1:ws_end]


def build_scripts_block(indent, max_formations, max_width):
    """Render the <scripts> snippet with indents matching sibling depth."""
    child = indent + "\t"
    grand = child + "\t"
    great = grand + "\t"
    lines = [
        f"{indent}<scripts>",
        f'{child}<script name="{SCRIPT_CLASS_NAME}">',
        f"{grand}<variables>",
        f'{great}<variable name="MaxFormations" value="{int(max_formations)}"/>',
        f'{great}<variable name="MaxWidth" value="{float(max_width):.3f}"/>',
        f"{grand}</variables>",
        f"{child}</script>",
        f"{indent}</scripts>",
    ]
    return "\n".join(lines)


def process(text, marker, max_formations, max_width):
    """
    Returns (new_text, attached, skipped, scanned). For each occurrence
    of the marker <tag> we walk back to the immediately-enclosing
    <game_entity> (so nested children can't double-count the parent),
    then insert the <scripts> block right after that entity's own
    <components>...</components> sibling. Edits are applied back-to-
    front so earlier offsets stay valid as the text grows.
    """
    tag_pattern = re.compile(
        r'<tag\s+name="' + re.escape(marker) + r'"\s*/>')
    hits = list(tag_pattern.finditer(text))
    # Back-to-front so inserting earlier edits does not shift later ones.
    hits.sort(key=lambda m: m.start(), reverse=True)

    scanned = 0
    attached = 0
    skipped = 0
    for h in hits:
        # Walk back to the nearest '<game_entity' opening tag whose
        # end lies before our marker tag; that's the direct parent.
        open_start = text.rfind("<game_entity", 0, h.start())
        if open_start < 0:
            continue
        open_end = text.find(">", open_start)
        if open_end < 0 or open_end >= h.start():
            continue
        # Entity body ends at the matching </game_entity>. Compute by
        # walking forward with depth-tracking since game_entity nests.
        body_start = open_end + 1
        body_end = find_matching_entity_close(text, body_start)
        if body_end is None:
            continue
        scanned += 1

        # Check for an already-attached AmbushSpawnPointScript at this
        # entity's depth. The body-slice may still contain nested
        # entities, so strip them before the substring check.
        own_body = strip_nested_entities(text[body_start:body_end])
        if re.search(r'<script\s+name="' + re.escape(SCRIPT_CLASS_NAME) + r'"',
                     own_body):
            skipped += 1
            continue

        components_close = find_direct_child_close(text, body_start, body_end,
                                                   "components")
        if components_close is None:
            continue

        indent = detect_indent(text, components_close - len("</components>"))
        snippet = "\n" + build_scripts_block(indent, max_formations, max_width)
        text = text[:components_close] + snippet + text[components_close:]
        attached += 1
    return text, attached, skipped, scanned


def find_matching_entity_close(text, body_start):
    """Walk forward from body_start counting game_entity opens/closes to
    find the one that closes the entity whose body starts at body_start."""
    open_re = re.compile(r"<game_entity\b")
    close_re = re.compile(r"</game_entity\s*>")
    depth = 0
    pos = body_start
    while True:
        o = open_re.search(text, pos)
        c = close_re.search(text, pos)
        if c is None:
            return None
        if o is not None and o.start() < c.start():
            depth += 1
            pos = o.end()
        else:
            if depth == 0:
                return c.start()
            depth -= 1
            pos = c.end()


def strip_nested_entities(body):
    """Return body with any <game_entity>...</game_entity> block removed,
    so sibling-level substring checks don't trigger on children."""
    out = []
    depth = 0
    i = 0
    open_tok = "<game_entity"
    close_tok = "</game_entity"
    while i < len(body):
        if body.startswith(open_tok, i):
            depth += 1
            end = body.find(">", i)
            i = end + 1 if end >= 0 else i + len(open_tok)
        elif body.startswith(close_tok, i):
            end = body.find(">", i)
            i = end + 1 if end >= 0 else i + len(close_tok)
            depth = max(0, depth - 1)
        else:
            if depth == 0:
                out.append(body[i])
            i += 1
    return "".join(out)


def find_direct_child_close(text, body_start, body_end, tag_name):
    """
    Find the end position (just past '>') of the entity's own
    </tag_name> closing. Skips any nested <game_entity> blocks so we
    don't accidentally pick a close belonging to a child entity.
    """
    open_re = re.compile(r"<" + tag_name + r"\b")
    close_re = re.compile(r"</" + tag_name + r"\s*>")
    nested_open = re.compile(r"<game_entity\b")
    nested_close = re.compile(r"</game_entity\s*>")
    pos = body_start
    depth = 0
    while pos < body_end:
        no = nested_open.search(text, pos, body_end)
        nc = nested_close.search(text, pos, body_end)
        o = open_re.search(text, pos, body_end)
        c = close_re.search(text, pos, body_end)
        candidates = [t for t in [no, nc, o, c] if t is not None]
        if not candidates:
            return None
        nxt = min(candidates, key=lambda m: m.start())
        if nxt is no:
            depth += 1
            pos = nxt.end()
        elif nxt is nc:
            depth -= 1
            pos = nxt.end()
        elif nxt is o and depth == 0:
            # Skip the opening of the target tag at depth 0;
            # move past '>' to continue scanning for its close.
            end_of_tag = text.find(">", nxt.start())
            pos = end_of_tag + 1 if end_of_tag >= 0 else nxt.end()
        elif nxt is c and depth == 0:
            return nxt.end()
        else:
            pos = nxt.end()
    return None


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("scene", type=Path, help="Path to scene.xscene")
    parser.add_argument("--marker", default=DEFAULT_MARKER_TAG,
                        help=f"Marker tag to match (default: {DEFAULT_MARKER_TAG})")
    parser.add_argument("--max-formations", type=int, default=2,
                        help="Default MaxFormations on attach (default: 2)")
    parser.add_argument("--max-width", type=float, default=0.0,
                        help="Default MaxWidth on attach (default: 0 = unlimited)")
    parser.add_argument("--dry-run", action="store_true",
                        help="Print what would change but do not save")
    args = parser.parse_args()

    if not args.scene.exists():
        print(f"error: scene not found: {args.scene}", file=sys.stderr)
        return 2

    original = args.scene.read_text(encoding="utf-8")
    new_text, attached, skipped, scanned = process(
        original, args.marker, args.max_formations, args.max_width)

    print(f"scanned {scanned} entities with tag '{args.marker}': "
          f"{attached} newly scripted, {skipped} already scripted")

    # Sanity-check: re-parse the modified scene to make sure we did not
    # break the XML. Cheap insurance against regex-driven file rewrites.
    try:
        etree.fromstring(new_text.encode("utf-8"))
    except etree.XMLSyntaxError as e:
        print(f"error: post-edit XML is invalid, refusing to save: {e}",
              file=sys.stderr)
        return 3

    if args.dry_run or attached == 0:
        if args.dry_run:
            print("(dry run, not saving)")
        return 0

    args.scene.write_text(new_text, encoding="utf-8")
    print(f"wrote {args.scene}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
