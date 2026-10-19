using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.CustomArenaModes;
using TOR_Core.BattleMechanics.Reinforcements;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.MapNotifications;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.CampaignMechanics.UniqueSpawns;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items.InventoryUseScripts;
using TOR_Core.Quests;
using TOR_Core.Quests.Careers;

namespace TOR_Core.SaveGameSystem
{
    public class TORSaveableTypeDefiner : SaveableTypeDefiner
    {
        public TORSaveableTypeDefiner() : base(771000) { }

        /// <remarks>
        /// Changing the id# will cause any save made with the prior id to crash the game when the game is launched and it scans through the Game Saves folder for every available save; this can only be done when people do not need access to prior saves, or they must clear their save folder of any prior ones.
        /// </remarks>
        protected override void DefineClassTypes()
        {
            //General definitions between 0-99
            //Sly : I thought about more organization within the first hundred between settlements, info, etc... but then I decided not to.
            AddClassDefinition(typeof(HeroExtendedInfo), 0);
            AddClassDefinition(typeof(MobilePartyExtendedInfo), 1);
            AddClassDefinition(typeof(TORBaseSettlementComponent), 2);
            AddClassDefinition(typeof(BaseRaiderSpawnerComponent), 3);
            AddClassDefinition(typeof(CursedSiteComponent), 4);
            AddClassDefinition(typeof(ShrineComponent), 5);
            AddClassDefinition(typeof(HerdStoneComponent), 6);
            AddClassDefinition(typeof(ChaosPortalComponent), 7);
            AddClassDefinition(typeof(GraveyardNightWatchPartyComponent), 8);
            AddClassDefinition(typeof(QuestPartyComponent), 9);
            AddClassDefinition(typeof(RaidingPartyComponent), 10);
            AddClassDefinition(typeof(JoustTournamentGame), 11);
            AddClassDefinition(typeof(SlaverCampComponent), 12);
            AddClassDefinition(typeof(OakOfAgesComponent), 13);
            AddClassDefinition(typeof(WorldRootsComponent), 14);
            AddClassDefinition(typeof(ArcheryContestTournamentGame), 15);
            AddClassDefinition(typeof(TorItemDuplicationData), 16);
            AddInterfaceDefinition(typeof(IInventoryUseScript), 17);
            AddClassDefinition(typeof(BaseInventoryUseScript), 18);
            AddClassDefinition(typeof(SkillBookScript), 19);
            AddClassDefinition(typeof(HeroTrainingData), 20);
            AddClassDefinition(typeof(TrollCaveComponent), 21);
            AddClassDefinition(typeof(TrollCaveDefenderPartyComponent), 22);
            AddClassDefinition(typeof(ScriptUseData), 23);
            AddClassDefinition(typeof(UniqueSpawnPartyComponent), 24);
            AddClassDefinition(typeof(TORMapNotification), 25);
            AddClassDefinition(typeof(GreenKnightBehavior.GreenKnightBattleState), 26);
            AddClassDefinition(typeof(ReinforcementBehavior.ReinforcementBattleState), 27);
            AddClassDefinition(typeof(ReinforcementBehavior.ReinforcementCandidateState), 28);


            //Quests and issues begin at 200 - they generally can't be expanded to cover new use cases
            AddClassDefinition(typeof(HuntCultistsQuestCampaignBehavior.HuntCultistsIssue), 200);
            AddClassDefinition(typeof(HuntCultistsQuestCampaignBehavior.HuntCultistsQuest), 201);
            AddClassDefinition(typeof(PlaguedVillageQuestCampaignBehavior.PlaguedVillageIssue), 202);
            AddClassDefinition(typeof(PlaguedVillageQuestCampaignBehavior.PlaguedVillageQuest), 203);
            // 204 was EngineerQuest — retired with the quest; keep the slot gap so other ids stay stable.
            AddClassDefinition(typeof(SpecializeLoreQuest), 205);
            AddClassDefinition(typeof(RunesmithQuest), 206);
            AddClassDefinition(typeof(RunelordQuest), 207);
            AddClassDefinition(typeof(OrcBossQuest1), 208);
            AddClassDefinition(typeof(OrcBossQuest2), 209);
            AddClassDefinition(typeof(OrcShamanQuest1), 210);
            AddClassDefinition(typeof(OrcShamanQuest2), 211);

            // Witch Hunter Lodge bounties — Phase B shipped these without
            // registration, which meant active bounty state got silently dropped
            // across save/load. Register at the next available slot.
            AddClassDefinition(typeof(CampaignMechanics.WitchHunter.Bounties.MinorBountyIssue), 212);
            // Id 213 originally held MinorBountyQuest; the Phase C refactor split it
            // into MinorBountyQuestBase (abstract — not instantiated) + per-objective
            // subclasses. 213 now points at the KillTroops subclass, which preserves
            // the field layout saved under the old class name.
            AddClassDefinition(typeof(CampaignMechanics.WitchHunter.Bounties.MinorBountyKillQuest), 213);
            AddClassDefinition(typeof(CampaignMechanics.WitchHunter.Bounties.MinorBountyCaptureQuest), 214);
            AddClassDefinition(typeof(CampaignMechanics.WitchHunter.Bounties.MinorBountyDefeatPartyQuest), 215);
        }

        protected override void DefineEnumTypes() //watch out for the save ids used for class definitions, conflicts will cause crashes on load
        {
            //Enums between 100-199
            // 100 was EngineerQuestStates — retired with the quest; slot preserved.
            AddEnumDefinition(typeof(SpellCastingLevel), 101);
            AddEnumDefinition(typeof(UniqueSpawnState), 102);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<Settlement, Dictionary<CharacterObject, int>>));
            ConstructContainerDefinition(typeof(Dictionary<Settlement, CultureObject>));
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(Dictionary<string, bool>));
            ConstructContainerDefinition(typeof(Dictionary<string, HeroExtendedInfo>));
            ConstructContainerDefinition(typeof(Dictionary<string, MobilePartyExtendedInfo>));
            ConstructContainerDefinition(typeof(List<RaidingPartyComponent>));
            ConstructContainerDefinition(typeof(List<KingdomDecision>));
            ConstructContainerDefinition(typeof(Dictionary<Settlement, Dictionary<CharacterObject, int>>));
            ConstructContainerDefinition(typeof(Dictionary<string, double>));
            ConstructContainerDefinition(typeof(Dictionary<string, float>));
            ConstructContainerDefinition(typeof(Dictionary<string, string>));
            ConstructContainerDefinition(typeof(Dictionary<ItemObject, TorItemDuplicationData>));
            ConstructContainerDefinition(typeof(Dictionary<string, List<string>>));
            ConstructContainerDefinition(typeof(List<BaseInventoryUseScript>));
            ConstructContainerDefinition(typeof(Dictionary<string, List<BaseInventoryUseScript>>));
            ConstructContainerDefinition(typeof(Dictionary<string, HeroTrainingData>));
            ConstructContainerDefinition(typeof(Dictionary<MapEvent, GreenKnightBehavior.GreenKnightBattleState>));
            ConstructContainerDefinition(typeof(Dictionary<MapEvent, ReinforcementBehavior.ReinforcementBattleState>));
            ConstructContainerDefinition(typeof(List<ReinforcementBehavior.ReinforcementCandidateState>));
            ConstructContainerDefinition(typeof(List<ScriptUseData>));
            ConstructContainerDefinition(typeof(List<string>));
        }
    }
}