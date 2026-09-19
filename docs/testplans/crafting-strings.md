# `Crafting` Strings — Test Plan

Manual test plan for [`../superpowers/plans/crafting-strings.md`](../superpowers/plans/crafting-strings.md).

**What this epic changed:** every player-visible string in `CampaignMechanics/Crafting/` and its
enchanting prefab now resolves through an id in `tor_strings.xml`; 18 ids added, 13 orphans
deleted, 2 renamed. Four of those ids were being requested by code and never existed.
**What this plan must prove:** every Crafting text shows real words, never an id, `ERROR:` or
blank. The only intended visual change is the blueprint-shop tooltip spacing (D8).

## Decisions taken

| # | Decision | Alternatives considered | Why this one |
|---|---|---|---|
| D1 | `tor_strings.xml` edited by hand; `tortools` used for reads only. | Write through `strings_add` as the spec says. | Re-tested on this branch: one add stripped all 326 category comments (719-line diff), then reverted. The server's category model knows 2 categories, so the comments are the only grouping. Fix belongs in TOR_Tools. |
| D2 | Player-visible = anything drawn on screen: dialog, menu, inquiry title/body/buttons, tooltip, notification, prefab label. Exceptions and logs stay literal. | Also localize `TORUseScriptArgumentException` messages. | Those are modder-facing XML errors; translating them hides the real item id from the person fixing it. |
| D3 | New ids go into the existing feature comment blocks (*Enchantment Shop*, *Enchanting UI Messages*), not a per-module block. | A `<!-- Crafting -->` block. | Neighbours by feature is how the file and its translators already work; the ledger, not the XML, tracks modules. |
| D4 | Orphans deleted: 5 `tor_enchantmentshop_requirement_*`, 8 priest variants (two literally say "PLEASE REPORT WHERE YOU SEE THIS"). `tor_enchantmentshop_insufficient_*` revived instead of re-minted. | Keep orphans in case they return. | No translation files exist yet, so deleting now costs nothing; each later epic would carry them forever. |
| D5 | The unreachable `DonationMode(false)` path is localized (4 ids), not deleted. | Delete it; or skip its strings. | Deleting code is Epic 4. Skipping would leave the only audit exceptions in the module. Epic 4 deletes the branch *and* `tor_enchant_prompt_disintegrate.*`, `tor_gained_items_notification_text`, `tor_gained_item_amount_text`. |
| D6 | `Done` reuses native `str_done`; buttons reuse `tor_inquiry_accept_text` / `_cancel_text`. | Mint `tor_enchanting_done_button`. | Native strings are already translated by TaleWorlds. |
| D7 | `tor_crafting.refine_all` → `tor_refine_all_text`, `ironingot6_name` → `tor_ironingot6_name`. | Change the code to match the old ids. | Code asked for `tor_refine_all_text` and the XML key already said so; the id was the typo. Renames are free until a translation exists. |
| D8 | Shop tooltip: `desc\n {gold} , {cr},\n {restriction}` → `desc{newline}{gold}, {cr}{newline}{restriction}`. | Reproduce the stray spaces exactly. | The old form prefixed a `{=id}` description onto the template, so any translation of the description would have silently dropped the cost line. |
| D9 | No automatic check for new literals in done modules; `docs/superpowers/tools/strings-audit.ps1` is run by hand per Strings epic. | CI or analyzer. | There is no CI. The script answers the spec's open question well enough for 25 more modules. |

## Run record

| | |
|---|---|
| Date | |
| Branch / commit | `feature/StringsCrafting` |
| Save used | |
| Tester | |
| Result | |

## Preconditions

**Build and launch** (PowerShell; the build overwrites the tracked `bin/.../TOR_Core.dll` —
don't commit it):

```powershell
cd "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\TOR_Core\CSharpSourceCode"
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" TOR_Core.csproj /p:Configuration=Release /v:m
cd ..\..\..\bin\Win64_Shipping_Client
.\Bannerlord.exe /singleplayer _MODULES_*Native*SandBoxCore*BirthAndDeath*CustomBattle*Sandbox*TOR_Armory*TOR_Environment*TOR_Core*_MODULES_
```

Steam must be running. Launcher equivalent: Singleplayer → Mods, tick exactly those eight
(`TOR_Environment` lives in the `TOR_Environment_new` folder), untick StoryMode → Play.

**Console:** set `cheat_mode = 1` in
`Documents\Mount and Blade II Bannerlord\Configs\engine_config.txt` (it is `0` on the dev
machine), then open it in-game with **Alt + ~**. A reply of `Cheat mode is disabled!` means this
step was missed. Native `campaign.*` commands separate arguments with ` | `; `tor.*` commands
use spaces.

**Campaign:** Sandbox → New Campaign → **Empire**, any background. On the map, run once:

```
tor.add_spells_to_player AmberSpear
```

Knowing a Lore of Beasts spell gives the hero that lore, which is what makes the Empire
enchanter stock Ghur scrolls — without it S4/S5 get "I'm afraid you have no one capable of
learning these arts." Save here as the base for every scenario.

**Where things are:** the Empire enchanter stands in every Empire town (use **Altdorf**); the
Sigmar priest only in Altdorf. Both are in the town scene — *Take a walk around the town*, hold
**Alt** to highlight them.

**Log:** `Modules\TOR_Core\Logs\<yyyy>\<Month>\<dd>\TOR_log<date>.txt`. A missing id logs
`[TEXT]Couldn't find text with id`.

## Scenarios

### S1 — No missing-text errors — **gate**

| | |
|---|---|
| Setup | Load the save; run S2–S9. |
| Expect | The TOR log has no `[TEXT]Couldn't find text` line naming a `tor_enchant*`, `tor_refine*`, `tor_gained*` or `tor_ironingot6*` id. |
| Result | X pass ☐ fail |

### S2 — Enchanting screen labels — **gate**

| | |
|---|---|
| Setup | Enchanter dialog → "May I use your enchanting facilities?" |
| Expect | Panel headers read **Items** and **Enchantments**; select an item: button reads **Enchant**; bottom button reads **Done**. |
| Result | X pass ☐ fail |

### S3 — Trait tooltip and trait limit

| | |
|---|---|
| Setup | `tor.add_enchantment_blueprint emp_enchant_ghur_crows` (Spellcraft 200) and `tor.add_enchantment_blueprint emp_enchant_ghur_whisper` (Spellcraft 25). Both apply to **weapons**; an armour selection shows an empty list. |
| Action | **Tooltip:** open the enchanting screen, select a weapon, hover *Crows of Ghur* (greyed — a new hero has no Spellcraft). **Limit:** greyed rows can't be selected, so close the screen, run `campaign.set_skills_of_hero <Your Hero Name> \| 200` (full name as shown in-game; sets every skill, Spellcraft included — check with **C**), reopen, select a weapon, click *Whisper of Ghur* then *Crows of Ghur*. |
| Expect | Tooltip: description, blank line, "Requires Spellcraft 200.". Limit popup on the second click: "You can only select 1 traits" (1 is the cap without the True Transmutation perk). |
| Result | ☐ pass X fail | The the spellcraft 200 requirement works, the not allowing more than 1 works. Unfortunately we see "You cannot select more than one traits" - it's a plural. Can we make sure that this is accounted for?

### S4 — Blueprint shop text — **gate**

| | |
|---|---|
| Setup | Enchanter → "I would like to learn new enchantments." |
| Expect | Title "Make your choice…", culture description, buttons **Accept** / **Cancel**. Hover an affordable row: description, cost line, "Applies to …" on separate lines. |
| Result | X pass ☐ fail |

### S5 — Unaffordable hints, all three

| | |
|---|---|
| Setup | The cheapest scroll (*Whisper of Ghur*) costs 3000 gold. Close and reopen the shop after each step — hints are built when it opens. Prestige never goes below 0, so an oversized negative zeroes it. |
| Action | **Both:** `tor.add_custom_resource Prestige -100000` (gold must be under 3000; a fresh hero is). **Gold only:** `tor.add_custom_resource Prestige 100000`. **Resource only:** `campaign.add_gold_to_hero 100000` then `tor.add_custom_resource Prestige -100000`. |
| Expect | Both: "Not enough ⟨resource⟩ and ⟨gold⟩." Gold only: "Not enough ⟨gold⟩." Resource only: "Not enough ⟨resource⟩." Row disabled in all three. The **first** row of a freshly opened shop shows the resource icon too — the review caught it resolving before the icon was set. |
| Result | X pass ☐ fail |

### S6 — Priest blessing shop and dialog

| | |
|---|---|
| Setup | `tor.add_devotion Sigmar 100`, then talk to the Sigmar priest in Altdorf. |
| Expect | Every dialog option shows text (8 orphan priest ids were deleted). Blessing shop title is "Make your choice…". |
| Result | X pass ☐ fail |

### S7 — Donate items

| | |
|---|---|
| Setup | `campaign.add_item_to_player_party tor_greenskin_mask_savage_001` (one item; it carries a trait, which is what the enchanter accepts) |
| Action | Enchanter → "I have magical items I no longer need." → donate it. |
| Expect | Prompt "Donate items" with the resource icon; then "Gained N⟨resource⟩". |
| Result | X pass ☐ fail |

### S8 — Refine All

| | |
|---|---|
| Setup | Run `campaign.add_item_to_player_party hardwood` four times (2 hardwood → 1 charcoal, so two refinements), then Artisan district → weaponsmith → Refinement. |
| Expect | Button "Refine All (N)"; with 0–1 possible, "Refine All". Hover hint unchanged. |
| Result | X pass ☐ fail |

### S9 — Gromril name

| | |
|---|---|
| Setup | `campaign.add_item_to_player_party ironIngot6`, then open the inventory (**I**) or any smithy's materials bar. |
| Expect | Named **Gromril**, not "Thamaskene Steel" or an id. |
| Result | X pass ☐ fail |

## Fixes

Found during this run. Retest each with a rebuilt DLL: close the game first — a running
Bannerlord locks `TOR_Core.dll` and the build fails at the copy step (`MSB3027`).

### F1 — Trait-limit popup plural (from S3)

**Found:** with a limit of 1 the popup read "You can only select 1 traits".
**Fix:** `tor_enchant_hint_max_traits_selectable` now uses
`{MAX_TRAITS} {?IS_PLURAL}traits{?}trait{\?}`; `EnchantingVM` sets `IS_PLURAL` when the limit is
above 1. Both cases need checking: the limit is 2 only with the True Transmutation perk
(Spellcraft 300), and no console command grants perks, so it is picked by hand.

| | |
|---|---|
| Setup | From the base save: `tor.add_enchantment_blueprint emp_enchant_ghur_whisper`, `tor.add_enchantment_blueprint emp_enchant_ghur_savagery`, `tor.add_enchantment_blueprint emp_enchant_ghur_crows` (Spellcraft 25 / 125 / 200, all **weapon**), then `campaign.set_skills_of_hero <Your Hero Name> \| 200`. |
| Action 1 | Enchanting screen → select a weapon → click *Whisper of Ghur*, then *Savagery of Ghur*. |
| Expect 1 | "You can only select 1 **trait**". |
| Action 2 | Close the screen. `campaign.set_skills_of_hero <Your Hero Name> \| 300`, open the character screen (**C**) → Spellcraft → pick **True Transmutation**. Reopen the enchanting screen → select a weapon → click *Whisper*, *Savagery*, then *Crows of Ghur*. |
| Expect 2 | The first two select; the third gives "You can only select 2 **traits**". |
| Result | ☐ pass ☐ fail |

## Commands added by this epic

None. Existing commands reach every scenario:

| Command | Syntax | Example |
|---|---|---|
| `tor.add_spells_to_player` | `<SpellId> …` | `tor.add_spells_to_player AmberSpear` |
| `tor.add_enchantment_blueprint` | `<TraitId>` (campaign-wide, no hero) | `tor.add_enchantment_blueprint emp_enchant_ghur_crows` |
| `tor.add_custom_resource` | `<ResourceId> <amount>`, negatives allowed, floors at 0 | `tor.add_custom_resource Prestige -100000` |
| `tor.add_devotion` | `<Deity> <amount>` | `tor.add_devotion Sigmar 100` |
| `campaign.set_skills_of_hero` | `<HeroName> \| <level>`; sets all skills | `campaign.set_skills_of_hero Karl Franz \| 200` |
| `campaign.add_gold_to_hero` | `[HeroName] \| <amount>`; hero omitted = player | `campaign.add_gold_to_hero 100000` |
| `campaign.add_item_to_player_party` | `<ItemId> [\| Modifier] [\| Amount]`; omitted = 1 | `campaign.add_item_to_player_party ironIngot6` |
