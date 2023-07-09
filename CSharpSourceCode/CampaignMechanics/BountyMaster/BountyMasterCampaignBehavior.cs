using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics.BountyMaster
{
    public class BountyMasterCampaignBehavior : CampaignBehaviorBase
    {
        private const string BountyMasterStringId = "tor_bountymaster";
        private Dictionary<string, string> _settlementToBountyMasterMap = new Dictionary<string, string>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
        }

        private void OnNewGameCreated(CampaignGameStarter obj)
        {
            foreach (var settlement in Settlement.All)
            {
                if (settlement.IsTown)
                {
                    CreateBountyMaster(settlement);
                }
            }
        }

        private void CreateBountyMaster(Settlement settlement)
        {
            CharacterObject template = null;
            var templates = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>().Where(x => x.IsTemplate);
            template = templates.FirstOrDefault(x => x.Culture.StringId == settlement.Culture.StringId && x.StringId.Contains(BountyMasterStringId) && x.StringId.Contains(settlement.Culture.StringId));

            if (template != null)
            {
                var hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 40);
                hero.SupporterOf = settlement.OwnerClan;
                hero.SetName(new TextObject(hero.FirstName.ToString() + " " + template.Name.ToString()), hero.FirstName);
                HeroHelper.SpawnHeroForTheFirstTime(hero, settlement);
                _settlementToBountyMasterMap.Add(settlement.StringId, hero.StringId);
            }
        }

        private void OnGameMenuOpened(MenuCallbackArgs obj) => SpawnBountyMasterIfNeeded();

        private void OnBeforeMissionStart() => SpawnBountyMasterIfNeeded();

        private void SpawnBountyMasterIfNeeded()
        {
            if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && !IsBountyMasterInTavern(Settlement.CurrentSettlement))
            {
                SpawnBountyMasterInTavern(Settlement.CurrentSettlement, true);
            }
        }

        private void SpawnBountyMasterInTavern(Settlement settlement, bool forceSpawn)
        {
            var master = GetBountyMasterForTown(settlement);
            var tavern = settlement.LocationComplex.GetLocationWithId("tavern");
            if (master != null && tavern != null)
            {

                if (forceSpawn)
                {
                    LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(master.CharacterObject, -1, null, default(UniqueTroopDescriptor))).Monster(FaceGen.GetBaseMonsterFromRace(master.CharacterObject.Race)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true);
                    tavern.AddCharacter(locationCharacter);
                }
                else
                {
                    var locChar = settlement.LocationComplex.GetLocationCharacterOfHero(master);
                    var currentloc = settlement.LocationComplex.GetLocationOfCharacter(master);
                    if (currentloc != tavern) settlement.LocationComplex.ChangeLocation(locChar, currentloc, tavern);
                }
            }
        }

        private bool IsBountyMasterInTavern(Settlement currentSettlement)
        {
            var settlement = Settlement.CurrentSettlement;
            var location = settlement.LocationComplex.GetLocationWithId("tavern");
            var master = GetBountyMasterForTown(settlement);
            if (master != null)
            {
                return location.GetLocationCharacter(master) != null;
            }
            else return false;
        }

        private Hero GetBountyMasterForTown(Settlement settlement)
        {
            Hero hero = null;
            var heroId = "";
            if (_settlementToBountyMasterMap.TryGetValue(settlement.StringId, out heroId))
            {
                hero = Hero.FindFirst(x => x.StringId == heroId);
            }
            return hero;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_settlementToBountyMasterMap", ref _settlementToBountyMasterMap);
        }

        public bool IsBountyMaster(Hero hero)
        {
            return _settlementToBountyMasterMap.ContainsValue(hero.StringId);
        }
    }
}
