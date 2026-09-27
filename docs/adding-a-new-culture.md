# Adding a new custom culture to TOR_Core

Reusable checklist for adding a new playable culture + kingdom to the mod. Written
during the Beastmen buildout, based on auditing every touchpoint of the existing
Mousillon culture (the first fully custom culture added to the mod). Follow the
sections in order — each stage assumes ids from earlier stages already exist.

The **`TOR_Tools` MCP host** covers most XML edits with schema-validated `query_entries` /
`duplicate_entry` / `update_entry` / `find_references` / `strings_add`. Prefer it over
grep + hand-edit; the section-by-section commands below assume it's wired.

Table of contents:

- [Overview: what a "culture" actually is](#overview)
- [Stage 0 — Decide identity](#stage-0)
- [Stage 1 — XML data (must-have, in order)](#stage-1)
- [Stage 2 — C# integration (must-have)](#stage-2)
- [Stage 3 — Character creation](#stage-3)
- [Stage 4 — Cross-culture rules](#stage-4)
- [Stage 5 — Optional flavor](#stage-5)
- [Stage 6 — Verify in-game](#stage-6)
- [Appendix: known gotchas](#appendix)

<a id="overview"></a>
## Overview: what a "culture" actually is

A "culture" in TOR is the union of:

- **1 `Culture` XML entry** — visual/economic backbone (colors, banner, encounter mesh,
  troop trees, role NPCs, name pools).
- **≥1 `Kingdom` XML entry** — political entity that owns clans/settlements. A culture
  can back several kingdoms (e.g., Empire electors) or exactly one (Mousillon).
- **≥1 `Clan` / faction XML entry** — sub-factions inside each kingdom. Each hero
  belongs to a clan.
- **≥1 `Hero` XML entry** — named lords. The kingdom's `owner=` attribute must point
  to a hero that exists.
- **≥1 troop tree** in `tor_troopdefinitions.xml` — recruit, militia, elite, unique.
- **≥1 `MBPartyTemplate` set** in `tor_partytemplates.xml` — lord/villager/caravan/
  militia/rebel/patrol party compositions.
- **≥3 `Settlement` entries** in `tor_settlements.xml` — town, castle, villages.
- **1 townspeople file** `tor_townspeople_<culture>.xml` — merchants, notables, etc.
- **A pile of `<string>` entries** in `tor_strings.xml` — every visible name.
- **Optional**: banner icon groups in `banner_icons.xml`, character-creation options,
  custom resources, careers, dialog tags.

If any of the **must-have** items is missing, the game either fails to load the campaign
or silently falls back to a wrong culture. The **should-have** items make the culture
feel first-class instead of bolted on. **Nice-to-have** items are career/quest tie-ins.

<a id="stage-0"></a>
## Stage 0 — Decide identity

Before writing anything, lock these decisions with the user / lore lead. They gate
every subsequent step and are painful to revise.

- [ ] **Culture id** (lowercase snake, no spaces): e.g., `beastmen`.
- [ ] **Culture display name** (localized): e.g., "Beastmen".
- [ ] **Kingdom id** (usually = culture id or `<culture>_kingdom`): e.g., `beastmen`.
- [ ] **Kingdom title / ruler title**: e.g., "Beastmen Herd" / "Beastlord".
- [ ] **Faction feel**: playable kingdom, or hidden/bandit-like? (Affects
      `is_main_culture`, whether the culture appears in the encyclopedia, character
      creation, etc.)
- [ ] **Culture flavor tag(s)** driving C# behavior:
  - Is this an "evil" culture (opposed to Empire/Bretonnia)? → `CustomResourceManager` evilCultures list.
  - Is this an "undead" culture? → `IsUndead`/`IsVampireKingdom` checks in extension methods.
  - Does it use an existing custom resource (DarkEnergy, Chivalry, Prestige...) or a new one?
- [ ] **Reskinned vanilla culture slot**: many mod cultures reuse a vanilla `Culture.*`
      id (empire, vlandia, khuzait, etc.) for save-compatibility. New custom cultures
      use a unique id (`mousillon`, `beastmen`, `chaos`, ...). Confirm the choice
      before authoring anything.
- [ ] **Campaign-map starting region** (rough polygon of the world map): needed so
      Stage-1 settlement coordinates land in the right terrain. Character creation
      spawn coords in Stage 3 must also land inside this region.
- [ ] **Reference culture to clone**: usually the closest thematic match. For the
      Beastmen buildout, Mousillon was the reference because it was the first fully
      custom culture (no vanilla-slot reuse).

<a id="stage-1"></a>
## Stage 1 — XML data (must-have, in order)

Order matters: later entries reference earlier ones by id. If you skip ahead, MCP
`validate` complains about dangling cross-references.

### 1a. Body property (crashes without it if troops reference it)

- [ ] `tor_bodyproperties.xml` → add one `BodyProperty id="fighter_<culture>_peasant"`
      entry with age/weight/build ranges and a facial-key template.
- MCP: `duplicate_entry(file="tor_bodyproperties.xml", id="fighter_mousillon_peasant")`
  then `update_entry` to change the id and tune values.

### 1b. Culture entry

- [ ] `tor_cultures.xml` → add one `Culture id="<culture>"` entry.
- Key attributes: `name` (with `{=str_...}Fallback`), `is_main_culture`, `color`,
  `color2`, `banner_key` (points at a group from `banner_icons.xml`),
  `encounter_background_mesh` (art asset), `basic_troop`, `elite_basic_troop`,
  `melee_militia_troop`, `ranged_militia_troop`, `default_party_template`,
  `villager_party_template`, `militia_party_template`.
- Fifty-odd role NPCs (townswoman, villager, caravan_master, blacksmith, etc.) can
  clone Vlandia or the reference culture's values wholesale — very few need
  culture-specific overrides.
- MCP: `duplicate_entry(file="tor_cultures.xml", id="mousillon")` then `update_entry`
  to swap the id, name, colors, banner, mesh. `find_references(id="Culture.<oldculture>")`
  after to catch anything still pointing at the reference.

### 1c. Banner icon groups (crashes on character creation banner picker without them)

- [ ] `banner_icons.xml` → add 1–2 `<BannerIconGroup id="<N>">` entries with
      16 `<Icon>` children each.
- Group ids: pick the next free integer (currently max ≈ 136).
- Icon ids: pick the next free 16-id range (current max ≈ 17610).
- `material_name` references an engine art asset that must exist
  (`<culture>_banners_1`, `<culture>_banners_2`) — coordinate with the art side or
  reuse an existing pack for playtest.
- MCP now supports this file end-to-end (`<base>`-root traversal fixed
  during the Beastmen buildout).

### 1d. Troops

- [ ] `tor_troopdefinitions.xml` → add ≥10 troops (peasant levy, archer, militia,
      men-at-arms, yeoman, elite, unique cultural units).
- Every troop needs `culture="Culture.<culture>"` and
  `face_key_template="BodyProperty.fighter_<culture>_peasant"`.
- MCP: `query_entries(file="tor_troopdefinitions.xml", filters=[{"field":"culture","op":"eq","value":"Culture.mousillon"}])`
  to enumerate the reference; `duplicate_entry` for each; `update_entry` to change
  ids/name/level.

### 1e. Party templates

- [ ] `tor_partytemplates.xml` → clone the reference culture's set:
      `<culture>_lordparty_template`, `_caravan_template`, `_elite_caravan_template`,
      `_villager_template`, `_militia_template`, `_rebelparty_template`, patrol
      templates (level 1/2/3).
- Each template's `<PartyTemplateStack>` entries reference `NPCCharacter.<troop_id>`
  from step 1d.
- MCP: `duplicate_entry` on each Mousillon template; `update_entry` on the new id
  and the `troop` attributes inside nested stacks.

### 1f. Kingdom + clan(s) + hero(es)

Circular reference alert: a Kingdom's `owner=` points at a Hero, which points at a
Clan, whose `faction=` points at the Kingdom. Author heroes → clans → kingdom in
that order (game load handles the cycle, but MCP `validate` reads best in order).

- [ ] `tor_heroes.xml` → add ≥1 hero (`tor_<culture>_lord_factionleader` is the
      canonical id for the kingdom leader). Set `culture`, and a `faction` that will
      exist in step 1g.
- [ ] `tor_clans.xml` → add ≥1 `Faction id="<culture>_clan_1"` entry. `culture`,
      `banner_key`, `text` description.
- [ ] `tor_kingdoms.xml` → add one `Kingdom id="<culture>"` entry with
      `owner="Hero.tor_<culture>_lord_factionleader"` and `culture="Culture.<culture>"`.
- MCP: `duplicate_entry` for each Mousillon equivalent; `update_entry` to rewire ids;
  finally `validate` on all three files to catch dangling references.

### 1g. Settlements

- [ ] `tor_settlements.xml` → add ≥3 settlements: 1 castle-town, 0–1 castles, ≥2
      villages per castle. All `culture="Culture.<culture>"` and
      `owner="Faction.<culture>_clan_1"` (or a specific clan for that region).
- Map coordinates: `posX`/`posY` in the roughly 713–1884 × 668–1387 range. Villages
  cluster near their castle; check the existing Mousillon coordinates (`castle_MS1`
  etc.) as a reference.
- Each settlement's nested `<Components>` polymorphically holds a `<Town>`/`<Village>`/
  `<Castle>` (or `<Hideout>` for lair-style locations) — cloning the reference
  preserves this correctly via MCP `duplicate_entry`.
- Scene references (`scene_name="TOR_<culture>_castle_001"` etc.) must resolve; for
  playtest fall back to a vanilla or reference-culture scene, then swap when art
  lands.

### 1h. Townspeople file

- [ ] Create `ModuleData/tor_townspeople_<culture>.xml`. Root
      `<NPCCharacters>` → 15–25 `<NPCCharacter is_template="true">` entries covering
      merchants (5), notables, gang leaders, headmen, preachers, rural notables.
- Simplest path: copy `tor_townspeople_mousillon.xml`, then MCP
  `query_entries(file="tor_townspeople_<culture>.xml", filters=[{"field":"culture","op":"eq","value":"Culture.mousillon"}])`
  → for each, `update_entry` to swap the id / name / culture. Equipment set refs
  usually inherit the reference culture's templates.
- Register the new file for loading — see the module's XML manifest (usually
  `SubModule.xml` or the `Modules` XSL manifest).

### 1i. Localization strings

- [ ] `tor_strings.xml` → add every visible string the entries above reference.
      Culture name, kingdom name, ruler title, troop names, settlement names, hero
      names, description text, encyclopedia flavor.
- MCP: `strings_add(id="str_tor_...", text="...", category="Cultures")` — pick an
  existing category from `strings_list_categories` so the new strings group with
  peers when the file rewrites.
- Sanity check: `strings_search(query="<culture>")` after this stage should return a
  chunky list; if it doesn't, some Fallback text is inline in the XML instead of
  routed through a string id.

<a id="stage-2"></a>
## Stage 2 — C# integration (must-have)

XML alone won't load — several C# constants and helper lists gate the culture.

- [ ] `Utilities/TORConstants.cs`
  - Add `public const string <CULTURE> = "<culture>";` to the `Cultures` struct.
  - If this culture has its own kingdom, add the same constant to the `Factions`
    struct.
  - Add to `Cultures.All` if it should be a "main playable" culture.
- [ ] `CampaignMechanics/CustomResources/CustomResourceManager.cs`
  - If the culture uses **DarkEnergy** (vampire/undead flavor): add to the DarkEnergy
    cultures list.
  - If the culture is **evil-aligned** (hostile-by-default to good factions): add to
    `evilCultures`.
  - If the culture is **undead**: add to `undeadCultures`.
  - Otherwise: add nothing here — the culture picks up default behavior.

<a id="stage-3"></a>
## Stage 3 — Character creation

- [ ] `tor_custom_xmls/tor_cc_options.xml` → add stage 1/2/3 options for the culture
      (`option_1_<culture>_<archetype>`, etc.). Each ties to an `EquipmentSetId` in
      `tor_equipment_sets.xml`.
- [ ] `CampaignMechanics/CharacterCreation/TORCharacterCreationContentHandler.cs`
  - Add hardcoded spawn coordinates (must land inside the culture's settlement
    region — cross-check against Stage 1g coords).
  - Add case branches for each new `option_3_<culture>_<career>` id if it triggers a
    career-choice or race change.
  - Add banner-icon key assignment for the culture's default banner.
- [ ] Localization: add the option titles/descriptions to `tor_strings.xml` (they
      appear in the CC UI, so they must be localized).

<a id="stage-4"></a>
## Stage 4 — Cross-culture rules (should-have)

Without these, the culture behaves oddly at hiring / reinforcement / smithing time.

- [ ] `Models/TORHiringCompatibilityModel.cs`
  - Which cultures can this culture recruit from?
  - Which cultures can recruit from this culture?
  - Castle-village exception rules (Mousillon's castle villages can recruit from
    Bretonnia/Sylvania — the model has explicit cases; add or omit for the new
    culture).
- [ ] `Models/TORReinforcementRestrictionModel.cs` — which units can join which
      parties as reinforcements.
- [ ] `Models/TORSmithingModel.cs` — which weapon categories are craftable by this
      culture's smiths (Mousillon restricted to Empire + Bretonnia gear).
- [ ] `CampaignMechanics/Crafting/EnchanterTownBehavior.cs` — if the culture has an
      enchanter NPC; specify which cultures can cross-visit.
- [ ] `CampaignMechanics/TORFactionDiscontinuationCampaignBehavior.cs` — if the
      culture should be marked for removal when a paired culture is eliminated
      (Mousillon dies when Sylvania dies, e.g.).
- [ ] `Extensions/KingdomExtension.cs` — add culture to `IsVampireKingdom` /
      `IsUndead` / other predicates if the flavor matches.

<a id="stage-5"></a>
## Stage 5 — Optional flavor (nice-to-have)

Skip on first pass; add as content lands.

- [ ] `CampaignMechanics/CustomDialogs/ConversationTags/<Culture>Tag.cs` — new
      `ConversationTag` subclass for culture-specific dialogue filtering.
- [ ] `CharacterDevelopment/CareerSystem/Choices/*` — culture-specific career trees
      (Grail Knight, Necrarch, Beastlord, etc.). Each is a new file cloned from an
      existing career.
- [ ] `CampaignMechanics/TORCustomSettlement/*` — special settlement types (shrine,
      cursed site, herdstone, chaos portal) tied to the culture.
- [ ] `CampaignMechanics/Assimilation/AssimilationCampaignBehavior.cs` — assimilation
      rules if the culture interbreeds/converts with others.
- [ ] `Quests/` — career storyline quests.

<a id="stage-6"></a>
## Stage 6 — Verify in-game

- [ ] `dotnet build` succeeds (or the mod's build script).
- [ ] Bannerlord launches with the culture's module loaded.
- [ ] Start a fresh campaign, character creation shows the new culture on stage 1.
- [ ] Pick the culture, complete CC — spawn lands in the culture's region.
- [ ] Encyclopedia → Kingdoms shows the new kingdom.
- [ ] Encyclopedia → Cultures shows the new culture with correct color/banner.
- [ ] Culture's kingdom has ≥1 clan, ≥1 hero, ≥1 settlement in-map.
- [ ] Recruit a peasant levy in the culture's town — party joins successfully.
- [ ] Enter a culture town/castle scene — no missing-scene warnings.
- [ ] Banner editor: culture's icon group appears in the banner picker without
      crashing.
- [ ] Save + reload a campaign — no save-corruption errors.

<a id="appendix"></a>
## Appendix: known gotchas

- **Culture id != Culture.id**: XML entries use bare `<culture>` in some fields and
  `Culture.<culture>` in others (owner, ownerFaction, etc.). MCP schemas handle the
  `prefixToStrip`/`prefixToAdd` when cross-navigating; hand-edits must match the
  reference.
- **Hero → Clan → Kingdom cycle**: game load resolves the cycle, MCP `validate` may
  complain during authoring. Ignore validate warnings until all three files are
  written; then re-validate.
- **Save-game compatibility**: custom types persisted by campaign behaviors must be
  registered in `SaveGameSystem/TORSaveableTypeDefiner.cs` with a stable id.
  New cultures alone don't trigger this — but new custom settlements, career
  choices, or custom resources do.
- **Banner materials**: the `material_name` on banner icons references an engine
  art asset. Missing materials render as pink checkerboards in-game but don't
  crash — safe to placeholder during script work, but flag for the art side.
- **Reference culture bleed-through**: after cloning Mousillon (or another
  reference), always run `find_references(id="Culture.<oldculture>")` and
  `find_references(id="Faction.<oldculture>_clan_1")` on the new entries; missed
  references silently route new-culture parties to the old culture's behavior.
- **String category grouping**: `strings_add` respects the `category` argument and
  places new strings under a `<!-- Category -->` comment on save. Use an existing
  category from `strings_list_categories` so the file rewrite is a small diff.
- **CRLF line-endings in TOR_Tools**: the sibling repo's Linux checkout tends to
  flip line endings — commits in TOR_Tools will show every file as "modified" if
  git core.autocrlf is off. Doesn't affect TOR_Core; is a TOR_Tools workflow
  gotcha only.

---

## Beastmen-specific decisions taken during this buildout

_Fill in as the build progresses so future cultures can look up "what did they do?"_

**Session 1 (2026-09-27) — playable-shell buildout, verbatim-clone approach:**

- Culture id: `beastmen`
- Culture display name: "Beastmen" (str_tor_culture_beastmen)
- Kingdom id: `beastmen`; display name "Beastmen Herds"; ruler title "Beastlord"
- Reference culture cloned: **Mousillon** (Culture, Kingdom, Clan, Hero, BodyProperty)
- Vanilla slot: **new** (not reusing a vanilla `Culture.*` slot — see caveat below on the bandit alias)
- Custom resource: **deferred** — flavor tags in `CustomResourceManager` not yet set
- Faction feel: **playable kingdom** (is_main_culture=true retained from Mousillon)
- Culture colors: `0xff4A2820` (dark brown-red) / `0xff8B7355` (bone/tan)
- Clan: `beastmen_clan_1` "Great Herd"
- Hero: `tor_beastmen_lord_factionleader` — the Beastlord of the Great Herd (placeholder text)
- BodyProperty: `fighter_beastmen_peasant` — verbatim clone of `fighter_mousillon_peasant` (needs Beastmen-flavored hair/beard/tattoo pool later)
- Starting settlements: **re-owned** Carroburg (town_ML3) + its 4 bound villages (Anseldorf ML3_1, Barenfähre ML3_2, Dunkelbild ML3_3, Weidemarkt ML3_4). Middenland loses those. `owner=Faction.beastmen_clan_1`, `culture=Culture.beastmen` on all five. No new settlement ids created. Positional / scene / component ids unchanged (crucial — settlement ids are referenced from save games, quests, and other data files).
- CC spawn coordinates: **not set** — spawn logic in `TORCharacterCreationContentHandler.cs` still needs Beastmen-specific coordinates. The player can't yet start as a Beastmen character.

**Caveat — the existing `BEASTMEN` alias:**

`Utilities/TORConstants.cs` line 44 already has `public const string BEASTMEN = "steppe_bandits";`. That is a **legacy bandit-tier alias** — pre-existing, unrelated to this buildout. The new kingdom-tier culture we authored uses the literal id `"beastmen"` and does **not** yet have a C# constant of its own. Any C# code that currently does `if (culture.StringId == TORConstants.Cultures.BEASTMEN)` will match the *bandit* culture (steppe_bandits), not the new kingdom culture. Same pattern as `GREENSKIN` (kingdom) / `GREENSKIN_BANDIT` (bandit) or `CHAOS` (kingdom) / `CHAOS_CULTIST` (bandit) — but the split hasn't been formalized for Beastmen yet. **Next session** should decide whether to rename `BEASTMEN` → `BEASTMEN_BANDIT` and add a new `BEASTMEN = "beastmen"` for the kingdom, or use a distinct name like `BEASTMEN_KINGDOM`.

**Still-to-do for a first-pass playable Beastmen:**

- Character-creation flow (Stage 3): `option_1/2/3_beastmen_*` in `tor_cc_options.xml`, spawn coords + banner-icon assignment in `TORCharacterCreationContentHandler.cs`, related strings.
- Cross-culture rules (Stage 4): hiring compatibility, reinforcement, faction discontinuation flag.
- Content polish (deferred): Beastmen-flavored name pools, notable templates, banner-icon groups, troop tree, encounter mesh, townspeople file.
