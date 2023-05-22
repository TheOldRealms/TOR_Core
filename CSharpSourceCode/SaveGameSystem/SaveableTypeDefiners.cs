﻿using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem.Spells;
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
        }

        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(EngineerQuestStates), 14);
            AddEnumDefinition(typeof(SpellCastingLevel), 15);
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
            ConstructContainerDefinition(typeof(Dictionary<Settlement, Dictionary<CharacterObject, int>>));
        }
    }
}