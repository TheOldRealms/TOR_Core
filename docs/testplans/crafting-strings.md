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

- An **Empire** campaign with an existing save (Empire has an enchanter, a Sigmar priest and a
  custom resource). S9 needs any smithy.
- Console enabled. Keep the TOR log open: a missing id logs `[TEXT]Couldn't find text with id`.

## Scenarios

### S1 — No missing-text errors — **gate**

| | |
|---|---|
| Setup | Load the save; run S2–S9. |
| Expect | The TOR log has no `[TEXT]Couldn't find text` line naming a `tor_enchant*`, `tor_refine*`, `tor_gained*` or `tor_ironingot6*` id. |
| Result | ☐ pass ☐ fail |

### S2 — Enchanting screen labels — **gate**

| | |
|---|---|
| Setup | Enchanter dialog → "May I use your enchanting facilities?" |
| Expect | Panel headers read **Items** and **Enchantments**; select an item: button reads **Enchant**; bottom button reads **Done**. |
| Result | ☐ pass ☐ fail |

### S3 — Trait tooltip and trait limit

| | |
|---|---|
| Setup | `tor.add_enchantment_blueprint <TraitId>` for a trait your party lacks the skill for. |
| Action | Hover it; then select more traits than allowed. |
| Expect | Tooltip: description, blank line, "Requires …". Limit popup: "You can only select N traits". |
| Result | ☐ pass ☐ fail |

### S4 — Blueprint shop text — **gate**

| | |
|---|---|
| Setup | Enchanter → "I would like to learn new enchantments." |
| Expect | Title "Make your choice…", culture description, buttons **Accept** / **Cancel**. Hover an affordable row: description, cost line, "Applies to …" on separate lines. |
| Result | ☐ pass ☐ fail |

### S5 — Unaffordable hints, all three

| | |
|---|---|
| Setup | Drop gold and/or prestige below a blueprint's cost with negative amounts: `campaign.add_gold_to_hero -N`, `tor.add_custom_resource Prestige -N` (spend down instead if a command refuses negatives). |
| Expect | Gold only: "Not enough ⟨gold⟩." Resource only: "Not enough ⟨resource⟩." Both: "Not enough ⟨resource⟩ and ⟨gold⟩." Row disabled in all three. The **first** row of a freshly opened shop shows the resource icon too — the review caught it resolving before the icon was set. |
| Result | ☐ pass ☐ fail |

### S6 — Priest blessing shop and dialog

| | |
|---|---|
| Setup | Talk to a Sigmar priest as a follower. |
| Expect | Every dialog option shows text (8 orphan priest ids were deleted). Blessing shop title is "Make your choice…". |
| Result | ☐ pass ☐ fail |

### S7 — Donate items

| | |
|---|---|
| Setup | `campaign.add_item_to_player_party tor_greenskin_mask_savage_001 1` |
| Action | Enchanter → "I have magical items I no longer need." → donate it. |
| Expect | Prompt "Donate items" with the resource icon; then "Gained N⟨resource⟩". |
| Result | ☐ pass ☐ fail |

### S8 — Refine All

| | |
|---|---|
| Setup | Artisan district → weaponsmith → Refinement, with materials for 2+ refinements. |
| Expect | Button "Refine All (N)"; with 0–1 possible, "Refine All". Hover hint unchanged. |
| Result | ☐ pass ☐ fail |

### S9 — Gromril name

| | |
|---|---|
| Setup | Smithy materials bar, or `campaign.add_item_to_player_party ironIngot6 1` then inventory. |
| Expect | Named **Gromril**, not "Thamaskene Steel" or an id. |
| Result | ☐ pass ☐ fail |

## Commands added by this epic

None. Native `campaign.add_gold_to_hero` / `campaign.add_item_to_player_party` and
`tor.add_custom_resource` / `tor.add_enchantment_blueprint` reach every scenario.
