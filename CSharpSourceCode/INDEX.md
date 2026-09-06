# TOR_Core — Index

**The Old Realms** is a Mount & Blade II: Bannerlord total-conversion mod bringing Games
Workshop's *Warhammer Fantasy Battles* setting to Bannerlord. This is `TOR_Core`, the main
C# gameplay-logic module (there are sibling modules — `TOR_Armory` for art/data assets,
`TOR_Environment`, and Bannerlord's own `Native`/`SandBox`/`StoryMode`/`CustomBattle` —
referenced by the launch args in `TOR_Core.csproj`, but not present in this source tree).

Every folder in this tree (down to leaf subfolders) has its own `CLAUDE.md` with details;
this file is the map of how they fit together. Start here, then drill into the folder that
owns the system you're touching.

## Orientation

- **Entry point**: `SubModule.cs` (`TOR_Core.SubModule : MBSubModuleBase`). Read this file
  first when you need to know "where does X get registered/initialized" — it is the single
  place that lists every `CampaignBehaviorBase`, `GameModel`, and mission behavior the mod
  adds, plus Harmony setup and startup ordering. If a system isn't wired up, it starts here.
- **Project file**: `TOR_Core.csproj` — old-style (non-SDK) .NET Framework 4.8 project;
  every source file must be listed in a `<Compile Include=.../>` element or it won't build.
  References are DLLs from the installed game (`../../../bin/Win64_Shipping_Client/`) and
  sibling modules (`Native`, `SandBox`, `StoryMode`, `CustomBattle`) — this is a Bannerlord
  mod, not a standalone app; you cannot build/run it without a Bannerlord installation.
- **`lib/`** — two vendored DLLs (`ink-engine-runtime.dll`, `ink_compiler.dll`) for the
  `Ink/` narrative-scripting integration. **`obj/`**/**`bin/`** — build output, ignore.
- **`Properties/`** — just `AssemblyInfo.cs`.

## Architectural patterns you'll see everywhere

- **`CampaignBehaviorBase` per mechanic** — almost every gameplay system in
  `CampaignMechanics/` is one behavior class registered in `SubModule.InitializeGameStarter`.
  To find how a mechanic starts, grep its behavior class name in `SubModule.cs`.
- **`GameModel` overrides** — `Models/` replaces vanilla formulas one at a time
  (`TORXyzModel : DefaultXyzModel`), registered in `SubModule.OnGameStart`. To change a
  formula, find the matching model here before writing a Harmony patch.
- **Harmony patches** (`HarmonyPatches/`) are the fallback for anything vanilla doesn't
  expose a model/behavior/virtual method for. Most patch at `OnSubModuleLoad`; a few need
  `[HarmonyPatchCategory("LatePatches")]` to run after `Game.Current`'s text manager exists.
- **XML-defined template data + a static loader/factory**: `AbilityTemplate`
  (`AbilitySystem/AbilityFactory`), `TriggeredEffectTemplate`
  (`BattleMechanics/TriggeredEffect/TriggeredEffectManager`), `StatusEffectTemplate`
  (`BattleMechanics/StatusEffect/StatusEffectManager`), `ItemTrait`
  (`Items/ItemTraitManager`), `ReligionObject`/`CareerObject`/`CareerChoiceObject`
  (native `MBObjectManager` + XML). All loaded once in `SubModule.OnSubModuleLoad`/
  `BeginGameStart`. If you need to add a new spell/effect/trait/career-choice, you're
  almost always adding a data entry + maybe one script class, not new infrastructure.
- **Utility-AI** (`BattleMechanics/AI`) — behaviors implement `IAgentBehavior` and score
  themselves via `Axis`/`ScoringFunctions`; `DecisionManager` picks the best score. This
  pattern is specific to spellcaster AI (`CastingAI/`) but the primitives
  (`CommonAIFunctions/`) are reusable.
- **Extension methods over subclassing** — `Extensions/` adds behavior to vanilla types
  (`Agent`, `Hero`, `CharacterObject`, ...) as static extension methods rather than wrapper
  classes, since most vanilla types aren't designed to be subclassed.
- **Two ways to attach "extra data" to a vanilla object**:
  - Runtime/campaign side data → `Extensions/ExtendedInfoSystem` (side dictionaries keyed
    by string id, e.g. `hero.GetExtendedInfo()`).
  - Extra bindable UI properties on a vanilla `ViewModel` → `Extensions/UI`'s
    `BaseViewModelExtension`/`ViewModelExtensionManager` (reflection-based property/command
    forwarding, registered via `[ViewModelExtension]`).
- **Save compatibility**: every persisted custom type must be registered in a
  `SaveableTypeDefiner` (mainly `SaveGameSystem/TORSaveableTypeDefiner`, but a few
  behaviors define their own inline) with a **stable, never-reused** numeric id — see that
  file's own warning before adding or renumbering one.

## The Warhammer domain model (so the folder docs make sense)

- **Cultures** (`Utilities/TORConstants.Cultures`) map onto (and often reuse the game-object
  slot of) vanilla Bannerlord cultures: Empire (`empire`), Bretonnia (`vlandia`), Sylvania
  (`khuzait`), Mousillon (`mousillon`), Asrai/Wood Elves (`battania`), Eonir/Wood Elves
  (`eonir`), Dawi/Dwarfs (`sturgia`), Greenskin (`aserai`) — plus Druchii, Beastmen, Chaos,
  and several bandit-culture reskins. `Cultures.All` lists the 8 main playable ones.
- **Magic**: Winds-of-Magic **Spells** (Lores: Fire/Light/Heavens/Life/Metal/Beasts/Death,
  High Magic, Dark Magic, Necromancy, Big Waaagh) and Dwarf **Rune Magic** are one system
  (`AbilitySystem`); priestly **Prayers** are a parallel system tied to
  `CampaignMechanics/Religion`; each **Career** has its own unique signature
  `CareerAbility`. All three share the same `Ability`/`AbilityTemplate` runtime.
- **Careers** (`CharacterDevelopment/CareerSystem`) are Warhammer-flavored "prestige
  classes" — Grail Knight, Black Grail Knight, Grail Damsel, Knight of the Old World,
  Witch Hunter, Warrior Priest (+ of Ulric), Blood Knight, Vampire Count, Necromancer,
  Necrarch, Imperial Magister, Waywatcher, Spellsinger, Warden, Grey Lord, Mercenary,
  Ironbreaker, Runelord, Slayer, Orc Boss, Orc Shaman — each with a perk tree
  (`CareerSystem/Choices`), a signature ability, and sometimes a special roster-screen
  button (`CareerSystem/CareerButton`).
- **Per-culture "second currency"** (`CampaignMechanics/CustomResources`): Prestige
  (Empire), Chivalry (Bretonnia), DarkEnergy (Sylvania/Mousillon), ForestHarmony (Asrai),
  CouncilFavor (Eonir), OathGold (Dawi), Teef/Waaagh (Greenskin).
- **Bespoke settlement types** (`CampaignMechanics/TORCustomSettlement`): Chaos Portal,
  Herdstone, Slaver Camp, Troll Cave (all raider-spawning lairs), Cursed Site, Oak of Ages,
  Shrine, World Roots.
- **Damage/effects pipeline**: an `Ability`/weapon-hit fires a
  `BattleMechanics/TriggeredEffect` → resolves target set → applies damage via
  `BattleMechanics/DamageSystem/TORDamageHelper` and/or a
  `BattleMechanics/StatusEffect` → both get scaled by `Models/TORAbilityModel`
  (skill/perk effectiveness) and `CharacterDevelopment/CareerSystem/CareerHelper`
  (career passives) along the way.

## Top-level folder map

| Folder | What it owns |
|---|---|
| `AbilitySystem/` | Spells, prayers, career abilities: the shared casting/effect runtime. |
| `Audio/` | Standalone file-based sound playback (ambient sounds). |
| `BattleMechanics/` | In-mission mechanics: AI, status effects, triggered effects, artillery, banners, firearms, dismemberment, voice, arena modes. |
| `CampaignMechanics/` | Every campaign-map mechanic (largest folder): factions, careers, religion, custom resources, diplomacy, crafting, custom settlements, quests-adjacent behaviors. |
| `CharacterDevelopment/` | Skills, perks, traits, attributes, and the Career data model. |
| `Extensions/` | Extension methods on vanilla types; the ExtendedInfo side-data system; the ViewModel-extension UI injection system. |
| `GameManagers/` | Early campaign bootstrapping, hotkeys, shader-related game managers. |
| `HarmonyPatches/` | Every Harmony patch, grouped by what vanilla system they touch. |
| `Ink/` | Branching-narrative integration (Inkle's Ink language). |
| `Items/` | Item traits/enchantments, weapon on-hit scripts, inventory-use scripts. |
| `Missions/` | Mission-open factory methods + scripted one-off fight controllers. |
| `Models/` | `GameModel` overrides (vanilla formula replacements) + Custom Battle variants. |
| `Quests/` | `QuestBase` quest classes, including Career storyline quests. |
| `SaveGameSystem/` | Save-type registration (`SaveableTypeDefiner`). |
| `Utilities/` | Cross-cutting static helpers (config, paths, constants, math, logging). |

## Where to look for a given task

- **"Add/tune a spell or prayer"** → `AbilitySystem/` (template + maybe a new
  `AbilityScript`), `AbilitySystem/Spells/LoreObject` if it's a new Lore.
- **"Add a new status effect / DOT / buff"** → `BattleMechanics/StatusEffect`.
- **"A weapon should do something special on hit"** → `Items/WeaponHitScripts` +
  `Items/ItemTrait`.
- **"Add/tune a Career perk or its passive"** → `CharacterDevelopment/CareerSystem/Choices`.
- **"Change how damage/resistance math works"** → `BattleMechanics/DamageSystem` +
  `Models/TORAgentApplyDamageModel`/`TORAbilityModel`.
- **"Add a new campaign mechanic/town service"** → a new `CampaignBehaviorBase` under
  `CampaignMechanics/`, registered in `SubModule.InitializeGameStarter`.
- **"Vanilla formula needs to behave differently"** → check `Models/` first; only reach for
  `HarmonyPatches/` if there's no model hook for it.
- **"AI isn't casting/behaving right"** → `BattleMechanics/AI/CastingAI` (spellcasters) or
  `BattleMechanics/AI/TeamAI` (formation/team AI).
- **"Add UI to an existing vanilla screen"** → `Extensions/UI` (`BaseViewModelExtension`)
  rather than a Harmony patch on the screen class, if at all possible.
