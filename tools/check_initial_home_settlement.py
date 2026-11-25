#!/usr/bin/env python3
"""
Audit initial_home_settlement across tor_kingdoms.xml and tor_clans.xml.

TW 1.5.7 Kingdoms.xsd / Factions.xsd mark this attribute as required on every
<Kingdom> and non-minor <Faction>. Missing values silently fail hero
generation during campaign start.

Run from repo root:
    python3 tools/check_initial_home_settlement.py
    python3 tools/check_initial_home_settlement.py --show-all
    python3 tools/check_initial_home_settlement.py --fix       # writes defaults
"""
import argparse
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
KINGDOMS = ROOT / "ModuleData" / "tor_kingdoms.xml"
CLANS = ROOT / "ModuleData" / "tor_clans.xml"
SETTLEMENTS = ROOT / "ModuleData" / "tor_settlements.xml"

PRIO_TOWN, PRIO_CASTLE, PRIO_VILLAGE, PRIO_OTHER = 1, 2, 3, 9


def load_settlement_index():
    """Return: clan_id -> [(priority, settlement_id), ...] sorted."""
    raw = SETTLEMENTS.read_text()
    idx = defaultdict(list)

    for m in re.finditer(
        r'<Settlement\s+id="([^"]+)"[^>]*?owner="Faction\.([^"]+)"[^>]*?>\s*'
        r'<Components>\s*<Town\s+[^>]*?is_castle="(false|true)"',
        raw, re.DOTALL):
        sid, owner, is_castle = m.group(1), m.group(2), m.group(3)
        idx[owner].append((PRIO_CASTLE if is_castle == "true" else PRIO_TOWN, sid))

    for m in re.finditer(
        r'<Settlement\s+id="([^"]+)"[^>]*?owner="Faction\.([^"]+)"[^>]*?>\s*'
        r'<Components>\s*<Village\b',
        raw, re.DOTALL):
        idx[m.group(2)].append((PRIO_VILLAGE, m.group(1)))

    for m in re.finditer(
        r'<Settlement\s+id="([^"]+)"[^>]*?owner="Faction\.([^"]+)"',
        raw, re.DOTALL):
        sid, owner = m.group(1), m.group(2)
        if any(s[1] == sid for s in idx[owner]):
            continue
        idx[owner].append((PRIO_OTHER, sid))

    return {o: sorted(v) for o, v in idx.items()}


def load_kingdom_info():
    """Return: list of (id, current_ihs_or_None, owner, culture)."""
    raw = KINGDOMS.read_text()
    out = []
    for m in re.finditer(r'<Kingdom\s+([^>]*?)>', raw, re.DOTALL):
        body = m.group(1)
        kid = re.search(r'\bid="([^"]+)"', body).group(1)
        ihs = re.search(r'initial_home_settlement="Settlement\.([^"]+)"', body)
        owner = re.search(r'owner="Hero\.([^"]+)"', body)
        culture = re.search(r'culture="Culture\.([^"]+)"', body)
        out.append((kid,
                    ihs.group(1) if ihs else None,
                    owner.group(1) if owner else None,
                    culture.group(1) if culture else None))
    return out


def load_faction_info():
    """Return: list of (id, current_ihs_or_None, super_kingdom_or_None,
    culture, is_minor, is_bandit)."""
    raw = CLANS.read_text()
    out = []
    for m in re.finditer(r'<Faction\s+([^/>]*?)(?:/?>)', raw, re.DOTALL):
        body = m.group(1)
        fid = re.search(r'\bid="([^"]+)"', body).group(1)
        ihs = re.search(r'initial_home_settlement="Settlement\.([^"]+)"', body)
        sup = re.search(r'super_faction="Kingdom\.([^"]+)"', body)
        culture = re.search(r'culture="Culture\.([^"]+)"', body)
        is_minor = 'is_minor_faction="true"' in body
        is_bandit = 'is_bandit="true"' in body
        out.append((fid,
                    ihs.group(1) if ihs else None,
                    sup.group(1) if sup else None,
                    culture.group(1) if culture else None,
                    is_minor, is_bandit))
    return out


def best_for_clan(clan_id, idx, clan_to_kingdom, kingdom_to_clans):
    """Direct owned settlement first, else a peer clan in same kingdom."""
    if clan_id in idx:
        return idx[clan_id][0][1]
    k = clan_to_kingdom.get(clan_id)
    if k:
        for peer in kingdom_to_clans.get(k, []):
            if peer in idx:
                return idx[peer][0][1]
    return None


def best_for_kingdom(kingdom_id, idx, kingdom_to_clans):
    for clan in kingdom_to_clans.get(kingdom_id, []):
        if clan in idx:
            return idx[clan][0][1]
    return None


def write_kingdom_attr(kingdom_id, settlement_id):
    raw = KINGDOMS.read_text()
    def sub(m):
        head = m.group(0)
        if 'initial_home_settlement=' in head:
            return head
        return f'<Kingdom id="{kingdom_id}"\n             initial_home_settlement="Settlement.{settlement_id}"'
    new = re.sub(rf'<Kingdom id="{re.escape(kingdom_id)}"', sub, raw, count=1)
    KINGDOMS.write_text(new)


def write_faction_attr(faction_id, settlement_id):
    """Inject initial_home_settlement into a Faction regardless of attribute order."""
    raw = CLANS.read_text()
    # Match entire <Faction ...> or <Faction ...>...</Faction> header.
    # Attribute order varies: id may not be first (e.g. green_knight_clan_1).
    faction_re = re.compile(r'<Faction\b([^>]*?)(/?>)', re.DOTALL)
    def sub(m):
        attrs, end = m.group(1), m.group(2)
        id_match = re.search(r'\bid="([^"]+)"', attrs)
        if not id_match or id_match.group(1) != faction_id:
            return m.group(0)
        if 'initial_home_settlement=' in attrs:
            return m.group(0)
        injection = f'\n             initial_home_settlement="Settlement.{settlement_id}"'
        return f'<Faction{attrs}{injection}{end}'
    new = faction_re.sub(sub, raw)
    CLANS.write_text(new)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--show-all", action="store_true",
                   help="print every row, not just missing ones")
    p.add_argument("--fix", action="store_true",
                   help="write a default initial_home_settlement where missing")
    p.add_argument("--default-settlement", default="town_AV1",
                   help="fallback settlement id for factions that own none and "
                        "have no kingdom peers (default: town_AV1 / Averheim)")
    args = p.parse_args()

    idx = load_settlement_index()
    kingdoms = load_kingdom_info()
    factions = load_faction_info()

    clan_to_kingdom = {f[0]: f[2] for f in factions if f[2]}
    kingdom_to_clans = defaultdict(list)
    for f in factions:
        if f[2]:
            kingdom_to_clans[f[2]].append(f[0])

    k_missing, k_ok = [], []
    for kid, ihs, owner, culture in kingdoms:
        proposed = best_for_kingdom(kid, idx, kingdom_to_clans) or args.default_settlement
        row = (kid, ihs, proposed, owner, culture)
        (k_ok if ihs else k_missing).append(row)

    f_missing, f_ok = [], []
    for fid, ihs, sup, culture, is_minor, is_bandit in factions:
        proposed = best_for_clan(fid, idx, clan_to_kingdom, kingdom_to_clans) or args.default_settlement
        row = (fid, ihs, proposed, sup, culture, is_minor, is_bandit)
        (f_ok if ihs else f_missing).append(row)

    print(f"=== tor_kingdoms.xml ({len(kingdoms)} entries) ===")
    print(f"  with    initial_home_settlement: {len(k_ok)}")
    print(f"  missing initial_home_settlement: {len(k_missing)}")
    print(f"=== tor_clans.xml   ({len(factions)} entries) ===")
    print(f"  with    initial_home_settlement: {len(f_ok)}")
    print(f"  missing initial_home_settlement: {len(f_missing)}")
    print()

    if k_missing or args.show_all:
        print("--- kingdoms ---")
        rows = sorted((k_missing + k_ok) if args.show_all else k_missing)
        for kid, ihs, proposed, owner, culture in rows:
            tag = "OK " if ihs else "MIS"
            cur = ihs or f"<would use: {proposed}>"
            print(f"  [{tag}] {kid:25} culture={culture or '-':15} -> {cur}")

    if f_missing or args.show_all:
        print("--- factions ---")
        rows = sorted((f_missing + f_ok) if args.show_all else f_missing)
        for fid, ihs, proposed, sup, culture, is_minor, is_bandit in rows:
            tag = "OK " if ihs else "MIS"
            cur = ihs or f"<would use: {proposed}>"
            flags = []
            if is_minor: flags.append("minor")
            if is_bandit: flags.append("bandit")
            if sup: flags.append(f"->{sup}")
            flagstr = " ".join(flags) or "-"
            print(f"  [{tag}] {fid:40} {flagstr:35} -> {cur}")

    if not args.fix:
        if k_missing or f_missing:
            print()
            print("Run with --fix to write proposed values to XML.")
        return 0 if not (k_missing or f_missing) else 1

    for kid, _, proposed, _, _ in k_missing:
        write_kingdom_attr(kid, proposed)
    for fid, _, proposed, _, _, _, _ in f_missing:
        write_faction_attr(fid, proposed)

    # Validate XML still parses after edits.
    for path in (KINGDOMS, CLANS):
        try:
            ET.fromstring(path.read_text())
        except ET.ParseError as e:
            print(f"XML PARSE ERROR after fix in {path.name}: {e}", file=sys.stderr)
            return 2

    print(f"\nFixed {len(k_missing)} kingdoms and {len(f_missing)} factions.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
