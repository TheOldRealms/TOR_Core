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

        [SaveableField(40)]
        private bool _initialized;

        [SaveableField(50)]
        private PartyTemplateObject _partyTemplate;

        public override Hero Leader => _owner;
        public override Hero PartyOwner => _owner;
        public override TextObject Name => _name;
        public override Settlement HomeSettlement => _homeSettlement;

        public bool IsReady => _initialized;

        public QuestPartyComponent(Hero owner, TextObject name, Settlement homeSettlement, PartyTemplateObject partyTemplate)
        {
            _owner = owner;
            _name = name;
            _homeSettlement = homeSettlement;
            _partyTemplate = partyTemplate;
        }

        protected override void OnMobilePartySetOnCreation()
        {
            InitializeQuestPartyProperties();
        }

        public static MobileParty CreateParty(Settlement settlement, Hero leader, Clan clan, string partyTemplateOverride = null)
        {
            var name= new TextObject ("{TOR_QUEST_PARTYLEADER_NAME}'s party");
            name.SetTextVariable("TOR_QUEST_PARTYLEADER_NAME", leader.FirstName);
            
            PartyTemplateObject partyTemplate = null;
            if (partyTemplateOverride != null)
            {
                partyTemplate = MBObjectManager.Instance.GetObject<PartyTemplateObject>(partyTemplateOverride);
            }
            
            //MapMobilePartyTrackerVMPatches uses "torquestparty" to detect TOR's parties to permit map trackers
            return MobileParty.CreateParty(leader.StringId + "_torquestparty_1", new QuestPartyComponent(leader, name, settlement, partyTemplate));
        }
        
        private void InitializeQuestPartyProperties()
        {
            var component = MobileParty.PartyComponent as QuestPartyComponent;
            MobileParty.ActualClan = PartyOwner.Clan;
            MobileParty.Aggressiveness = 0.5f;
            MobileParty.AddElementToMemberRoster(Leader.CharacterObject, 1, true);
            if (_partyTemplate == null)
                _partyTemplate = Clan.DefaultPartyTemplate;
            MobileParty.InitializeMobilePartyAroundPosition(_partyTemplate, HomeSettlement.Position, 30);
            MobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Grain, 50));
            SetPartyAiAction.GetActionForPatrollingAroundSettlement(MobileParty, HomeSettlement, MobileParty.NavigationType.Default, false, false);
            MobileParty.Ai.SetDoNotMakeNewDecisions(true);
            MobileParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
            _initialized = true;
            // mobileParty.Party.Visuals.SetMapIconAsDirty();
        }

        public TextObject GetName()
        {
            return new TextObject(MobileParty.Name.ToString());
        }

        public Vec3 GetPosition()
        { 
            return MobileParty.GetPositionAsVec3();
        }

        public bool CheckTracked(BasicCharacterObject basicCharacter)
        {
            return MobileParty.IsCurrentlyUsedByAQuest;
        }

        public Banner GetBanner()
        {
            return _owner.Clan?.Banner;
        }
    }
}