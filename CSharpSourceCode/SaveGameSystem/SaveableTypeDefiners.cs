using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.CustomArenaModes;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.RaiseDead;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Quests;

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
        }

        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(EngineerQuestStates), 23);
            AddEnumDefinition(typeof(SpellCastingLevel), 24);
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
            ConstructContainerDefinition(typeof(Dictionary<string, List<string>>));
        }
    }
}
