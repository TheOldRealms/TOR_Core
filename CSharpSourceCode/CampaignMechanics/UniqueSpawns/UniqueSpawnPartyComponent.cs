using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.UniqueSpawns
{
    public class UniqueSpawnPartyComponent : WarPartyComponent
    {
        [SaveableField(1)]
        private Hero _partyOwner;

        [SaveableField(2)]
        private Settlement _spawnSettlement;

        [SaveableField(3)]
        private string _partyName;

        [SaveableField(4)]
        private PartyTemplateObject _spawnTemplate;

        [SaveableField(5)]
        private int _targetPartySize;

        [SaveableField(6)]
        private string _uniqueSpawnId;

        [SaveableField(7)]
        private bool _consumesFood; // old saves

        [SaveableField(8)]
        private int _startingFoodPerType;

        [SaveableField(9)]
        private int _initialRegularTroopCount;

        public override Hero Leader => _partyOwner;
        public override Hero PartyOwner => _partyOwner;
        public override Settlement HomeSettlement => _spawnSettlement;
        public override TextObject Name => new(_partyName);

        public int TargetPartySize => _targetPartySize;
        public string UniqueSpawnId => _uniqueSpawnId;
        public PartyTemplateObject SpawnTemplate => _spawnTemplate;
        public int InitialRegularTroopCount => _initialRegularTroopCount > 0
            ? _initialRegularTroopCount
            : _spawnTemplate.Stacks.Sum(stack => stack.MaxValue);

        private UniqueSpawnPartyComponent(
            string uniqueSpawnId,
            Settlement spawnSettlement,
            TextObject partyName,
            PartyTemplateObject spawnTemplate,
            Clan ownerClan,
            int targetPartySize,
            int startingFoodPerType)
        {
            _uniqueSpawnId = uniqueSpawnId;
            _spawnSettlement = spawnSettlement;
            _partyName = partyName.ToString();
            _spawnTemplate = spawnTemplate;
            _partyOwner = ownerClan.Leader;
            _targetPartySize = targetPartySize;
            _startingFoodPerType = startingFoodPerType;
        }

        protected override void OnMobilePartySetOnCreation()
        {
            BuildSpawnParty();
        }

        private void BuildSpawnParty()
        {
            MobileParty.InitializeMobilePartyAroundPosition(_spawnTemplate, _spawnSettlement.GatePosition, 10f);
            _initialRegularTroopCount = MobileParty.MemberRoster.TotalManCount;

            // party spawns managed by unique spawn behaviors won't have the owner hero added by the party template because their spawns had to be conditional. skipping native party creation
            MobileParty.AddElementToMemberRoster(_partyOwner.CharacterObject, 1, true);

            MobileParty.ActualClan = _partyOwner.Clan;
            MobileParty.Aggressiveness = 2.0f;
            MobileParty.IsVisible = true;
            MobileParty.Ai.SetDoNotMakeNewDecisions(false);

            AddStartingFoodIfNeeded();

            MobileParty.Party.SetVisualAsDirty();
        }

        private void AddStartingFoodIfNeeded()
        {
            if (_startingFoodPerType <= 0)
            {
                return;
            }

            foreach (var foodItem in MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(item => item.HasFoodComponent && !item.NotMerchandise))
            {
                MobileParty.ItemRoster.Add(new ItemRosterElement(foodItem, _startingFoodPerType));
            }
        }

        public static MobileParty CreateUniqueSpawnParty(
            string stringId,
            string uniqueSpawnId,
            Settlement spawnSettlement,
            TextObject partyName,
            PartyTemplateObject spawnTemplate,
            Clan ownerClan,
            int targetPartySize,
            int startingFoodPerType = UniqueSpawnCampaignBehavior.UniqueSpawnStartingFoodPerType)
        {
            return MobileParty.CreateParty(
                stringId,
                new UniqueSpawnPartyComponent(
                    uniqueSpawnId,
                    spawnSettlement,
                    partyName,
                    spawnTemplate,
                    ownerClan,
                    targetPartySize,
                    startingFoodPerType));
        }
    }
}