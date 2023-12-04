using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.Quests
{
    public class QuestPartyComponent : WarPartyComponent, ITrackableCampaignObject
    {
        [SaveableField(10)]
        private TextObject _name;

        [SaveableField(20)]
        private Settlement _homeSettlement;

        [SaveableField(30)]
        private Hero _owner;
        
        public override Hero Leader => _owner;
        public override Hero PartyOwner => _owner;
        public override TextObject Name => _name;
        public override Settlement HomeSettlement => _homeSettlement;
        
        public static MobileParty CreateParty(Settlement settlement, Hero leader, Clan clan, string partyTemplateOverride=null)
        {
            var name= new TextObject ("{TOR_QUEST_PARTYLEADER_NAME}'s party");
            name.SetTextVariable("TOR_QUEST_PARTYLEADER_NAME", leader.FirstName);
            
            PartyTemplateObject partyTemplate = null;
            if(partyTemplateOverride!=null)
            {
                partyTemplate = MBObjectManager.Instance.GetObject<PartyTemplateObject>(partyTemplateOverride);
            }
            
            return MobileParty.CreateParty(leader.StringId + "_questparty_1", new QuestPartyComponent(), delegate (MobileParty mobileParty)
            {
                (mobileParty.PartyComponent as QuestPartyComponent).InitializeQuestPartyProperties(mobileParty, settlement, leader, clan, name, partyTemplate);
            });
        }
        
        private void InitializeQuestPartyProperties(MobileParty mobileParty, Settlement settlement, Hero leader, Clan clan, TextObject name=null, PartyTemplateObject partyTemplate=null)
        {
            var component = mobileParty.PartyComponent as QuestPartyComponent;
            component._owner = leader;
            component._homeSettlement = settlement;
            if(name!=null)
                component._name = name;
            mobileParty.ActualClan = clan;
            mobileParty.Aggressiveness = 0.5f;
            mobileParty.AddElementToMemberRoster(leader.CharacterObject, 1, true);
            if (partyTemplate == null) 
                partyTemplate = clan.DefaultPartyTemplate;
            mobileParty.InitializeMobilePartyAroundPosition(partyTemplate, settlement.Position2D, 30, 15, 30);
            mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Grain, 50));
            SetPartyAiAction.GetActionForPatrollingAroundSettlement(mobileParty, settlement);
            mobileParty.Ai.SetDoNotMakeNewDecisions(true);
            mobileParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
          // mobileParty.Party.Visuals.SetMapIconAsDirty();
        }

        public TextObject GetName()
        {
            return new TextObject(MobileParty.Name.ToString());
        }

        public Vec3 GetPosition()
        { 
            return MobileParty.GetPosition();
        }

        public float GetTrackDistanceToMainAgent()
        {
            return MobileParty.GetTrackDistanceToMainAgent();
        }

        public bool CheckTracked(BasicCharacterObject basicCharacter)
        {
            return MobileParty.IsCurrentlyUsedByAQuest;
        }
    }
}