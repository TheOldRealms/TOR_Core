# Settlement POI Test Plan

Checklist for in-game verification of the three spell-trainer halls wired through
`SettlementPOIManagerCampaignBehavior`. Tick as you go; expand with notes under a
row when something misbehaves. All POIs share the same anchor (`town_artisan_leave`,
inserted *above* the Leave option in the Artisan menu).

Legend: `[ ]` not tested · `[~]` partial · `[x]` ok · `[!]` broken

---

## VampireCryptPOI

| Item | Value |
|---|---|
| LocationId | `tor_vampire_crypt` |
| Scene | `TOR_vampire_magister_001` |
| Trainer template | `tor_spelltrainer_vc_0` (plays both ISpellTrainer + IEnchanter) |
| Race gate | `vampire` OR `necrarch` |
| Career gate | `Necrarch` OR `MinorVampire` |
| Settlements | `town_SY1`, `town_SY2`, `town_MS1` |
| Enchantment suffix | `vc_enchant` |

### Smoke test (per settlement)

- [x] town_SY1 — Dark Magister dialog works (user confirmed 2026-10-12)
- [ ] town_SY2
- [ ] town_MS1

### Edge cases (any settlement is fine)

- [ ] Enter POI as a non-vampire → "The dead within turn you away."
- [ ] Enter as vampire but wrong career (e.g. BloodKnight) → "Only the undying may enter the crypt."
- [ ] Enter during siege → "The crypt is sealed during the siege."
- [ ] POI menu entry only shows once per town (no duplicate drift)
- [ ] Save + reload inside the POI scene → dialog still works

---

## SpellsingersGrovePOI

| Item | Value |
|---|---|
| LocationId | `tor_spellsingers_grove` |
| Scene | `TOR_POI_spellsinger_cave_001_atmo_w_night` |
| Trainer template | `tor_spelltrainer_woodelves_0` |
| Race gate | `elf` OR `orion` |
| Career gate | `Spellsinger` OR `Waywatcher` |
| Settlements | `town_AL1`, `town_AL2`, `town_AL3`, `town_AL4`, `town_AL5`, `town_AL6` |
| Enchantment suffixes | `we_enchant`, `asrai_enchant` |

### Smoke test (per settlement)

- [ ] town_AL1
- [ ] town_AL2
- [ ] town_AL3
- [ ] town_AL4
- [ ] town_AL5
- [ ] town_AL6

### Edge cases

- [ ] Non-elf hero rejected with "The grove does not open for your kind."
- [ ] Elf without Spellsinger/Waywatcher → "Only the children of Ariel may enter the grove."
- [ ] Siege lockout
- [ ] Spellsinger dialog hub reachable (Ariel pathways, spellweaver choice, etc.)
- [ ] Save + reload inside the grove

---

## ShamanHutPOI

| Item | Value |
|---|---|
| LocationId | `tor_shaman_hut` |
| Scene | `TOR_greenskin_shaman_lair_001` |
| Trainer template | `tor_spelltrainer_greenskins_0` |
| Race gate | `orc` OR `goblin` |
| Career gate | `OrcShaman` |
| Settlements | `town_MC1`, `town_MC2`, `town_BX1`, `town_BX2`, `town_BZ1`, `town_BZ2`, `town_SM1`, `town_SM2`, `town_SM3`, `town_DG1`, `town_DG2`, `town_NS1`, `town_CE1`, `town_CE2`, `town_BS1`, `town_BS2`, `town_RE1`, `town_RE2`, `town_IT1`, `town_IT2`, `town_IT3` |
| Enchantment suffix | `gs_enchant` |

### Smoke test (sample — pick 2–3 towns across different clans)

- [ ] town_MC1
- [ ] town_BX1
- [ ] town_IT1

### Edge cases

- [ ] Non-greenskin rejected with "Da shaman don't see yer kind."
- [ ] Orc without OrcShaman career → "Only shaman-boyz may enter."
- [ ] Siege: "The shaman da hut iz closed. Bosses fightin'!"
- [ ] Save + reload inside hut

---

## Cross-cutting checks (run once, apply to all three)

- [ ] **Legacy spawner is OFF**: no spell trainer appears in `house_1` of any VC/Asrai/Greenskin town after `OnNewGameCreated` (confirms `IsPoiManagedSpellTrainerCulture` short-circuit)
- [ ] **Legacy enchanter is OFF**: no enchanter in `house_1` either (confirms `IsPoiManagedEnchanterCulture`)
- [ ] **Discovery works**: visit a town once as the player → the "?" placeholder on the settlement nameplate flips to the POI icon
- [ ] **Dynamic Location persists**: save after entering a town for the first time, reload → the POI location still exists in that town's complex (ReapplyDynamicScenes on OnSessionLaunched)
- [ ] **Hero StringIds stable across saves**: Dark Magister / Spellsinger Elder / Shaman keep the same StringId after a save-reload cycle (no duplication)
- [ ] **No hero drift**: trainer's `HomeSettlement` matches the POI's settlement at all times (manager has drift-log; check `rgl_log` for warnings)

---

## Console helpers (debugging aids)

- `tor.establish_poi <SettlementId> <LocationId> <SceneName>` — attach a POI at runtime
- `tor.discover_all_pois` — flip discovery for every applicable POI (for icon testing)
- `tor.undiscover_all_pois` — reset discovery (confirm "?" placeholder)
- `tor.add_career <CareerId>` — switch the player's career for gate testing (e.g. `Necrarch`, `Spellsinger`, `OrcShaman`)

---

## Not covered yet (deferred)

- **RunehallPOI** — on hold per request
- **GrailChapelPOI** — rescoped: the existing site is a forest shrine, so this becomes
  a side-quest ("add a spell-trainer hall to a specific shrine") rather than a town POI.
  Open thread; not blocking this test plan.
