using Helpers;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
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
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        }

        private void OnNewGameCreated(CampaignGameStarter obj)
        {
            foreach (var town in Town.AllTowns)
            {
                if (town.Settlement.IsTown)
                {
                    CreateBountyMaster(town.Settlement);
                }
            }
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (!settlement.IsTown)
            {
                return;
            }

            var bountyMaster = GetBountyMasterForTown(settlement);
            var newOwnerCultureId = newOwner.MapFaction.Culture.StringId;

            if (newOwnerCultureId == "empire")
            {
                if (bountyMaster == null)
                {
                    CreateBountyMaster(settlement);
                }

                return;
            }
            if (bountyMaster != null)
            {
                DisableHeroAction.Apply(bountyMaster);
                _settlementToBountyMasterMap.Remove(settlement.StringId);
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
                hero.CharacterObject.HiddenInEncyclopedia = true;
                var nameObject = template.GetName();
                nameObject.SetTextVariable("FIRSTNAME", hero.FirstName);
                hero.SetName(nameObject, hero.FirstName);
                HeroHelper.SpawnHeroForTheFirstTime(hero, settlement);
                _settlementToBountyMasterMap.Add(settlement.StringId, hero.StringId);
            }
        }

        private void OnGameMenuOpened(MenuCallbackArgs obj) => PlaceBountyMasterIfNeeded();

        private void OnBeforeMissionStart() => PlaceBountyMasterIfNeeded();

        private void PlaceBountyMasterIfNeeded()
        {
            var s = Settlement.CurrentSettlement;
            if (s != null && s.IsTown && !IsBountyMasterPlaced(s))
                PlaceBountyMaster(s);
        }

        /// <summary>
        /// Target location for the town's bounty master. Prefers the Witch Hunter Lodge
        /// where it exists (the Order of Sigmar canonically runs bounty work from the
        /// chapterhouse); falls back to the tavern — vanilla's historic placement — for
        /// Empire towns without a lodge.
        /// </summary>
        private static Location GetTargetLocation(Settlement settlement)
        {
            var complex = settlement?.LocationComplex;
            if (complex == null) return null;
            var lodge = complex.GetLocationWithId("tor_witchhunter_lodge");
            if (lodge != null && lodge.GetSceneCount() > 0) return lodge;
            return complex.GetLocationWithId("tavern");
        }

        private bool IsBountyMasterPlaced(Settlement settlement)
        {
            var master = GetBountyMasterForTown(settlement);
            var target = GetTargetLocation(settlement);
            if (master == null || target == null) return false;
            return target.GetLocationCharacter(master) != null;
        }

        private void PlaceBountyMaster(Settlement settlement)
        {
            var master = GetBountyMasterForTown(settlement);
            var target = GetTargetLocation(settlement);
            if (master == null || target == null) return;

            // Migrate from an older placement (e.g. tavern on a save predating the lodge).
            var current = settlement.LocationComplex.GetLocationOfCharacter(master);
            if (current != null && current != target && current.GetLocationCharacter(master) != null)
                current.RemoveCharacter(master);

            if (target.GetLocationCharacter(master) != null) return;

            var locationCharacter = new LocationCharacter(
                new AgentData(new SimpleAgentOrigin(master.CharacterObject, -1, null, default(UniqueTroopDescriptor)))
                    .Monster(FaceGen.GetBaseMonsterFromRace(master.CharacterObject.Race)),
                new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors),
                "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true);
            target.AddCharacter(locationCharacter);
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