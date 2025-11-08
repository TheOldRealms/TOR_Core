using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.CustomArenaModes;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items.InventoryUseScripts;
using TOR_Core.Quests;
using TOR_Core.Quests.Careers;

namespace TOR_Core.SaveGameSystem
{
    public class TORSaveableTypeDefiner : SaveableTypeDefiner
    {
        public TORSaveableTypeDefiner() : base(771000) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(TORBaseSettlementComponent), 1);
            AddClassDefinition(typeof(BaseRaiderSpawnerComponent), 2);
            AddClassDefinition(typeof(CursedSiteComponent), 3);
            AddClassDefinition(typeof(ShrineComponent), 4);
            AddClassDefinition(typeof(HerdStoneComponent), 5);
            AddClassDefinition(typeof(ChaosPortalComponent), 6);
            AddClassDefinition(typeof(EngineerQuest), 7);
            AddClassDefinition(typeof(GraveyardNightWatchPartyComponent), 8);
            AddClassDefinition(typeof(HeroExtendedInfo), 9);
            AddClassDefinition(typeof(MobilePartyExtendedInfo), 10);
            AddClassDefinition(typeof(QuestPartyComponent), 11);
            AddClassDefinition(typeof(RaidingPartyComponent), 12);
            AddClassDefinition(typeof(SpecializeLoreQuest), 13);
            AddClassDefinition(typeof(JoustTournamentGame), 14);
            AddClassDefinition(typeof(HuntCultistsQuestCampaignBehavior.HuntCultistsIssue), 15);
            AddClassDefinition(typeof(HuntCultistsQuestCampaignBehavior.HuntCultistsQuest), 16);
            AddClassDefinition(typeof(PlaguedVillageQuestCampaignBehavior.PlaguedVillageIssue), 17);
            AddClassDefinition(typeof(PlaguedVillageQuestCampaignBehavior.PlaguedVillageQuest), 18);
            AddClassDefinition(typeof(SlaverCampComponent), 19);
            AddClassDefinition(typeof(OakOfAgesComponent), 20);
            AddClassDefinition(typeof(WorldRootsComponent), 21);
            AddClassDefinition(typeof(ArcheryContestTournamentGame), 22);
            AddClassDefinition(typeof(TorItemDuplicationData), 23);
            AddInterfaceDefinition(typeof(IInventoryUseScript), 24);
            AddClassDefinition(typeof(BaseInventoryUseScript), 25);
            AddClassDefinition(typeof(SkillBookScript), 26);
            AddClassDefinition(typeof(HeroTrainingData),27);
            AddClassDefinition(typeof(RunesmithQuest), 30);    //TODO change to 28, and so on
            AddClassDefinition(typeof(RunelordQuest), 31);    //TODO change to 28, and so on
            AddClassDefinition(typeof(OrcBossQuest1), 32); //Sly : changing the id# will cause any save made with the prior id to crash the game when the game is launched and it scans through the Game Saves folder for every available save; this can only be done when people do not need access to prior saves, or they must clear their save folder of any prior ones
            AddClassDefinition(typeof(OrcBossQuest2), 33);
        }

        protected override void DefineEnumTypes() //watch out for the save ids used for class definitions, conflicts will cause crashes on load
        {
            AddEnumDefinition(typeof(EngineerQuestStates), 28);
            AddEnumDefinition(typeof(SpellCastingLevel), 29);
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
        }
    }
}
