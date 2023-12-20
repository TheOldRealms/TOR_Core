using SandBox;
using SandBox.GauntletUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.ScreenSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomResources
{
    public class CustomResourceManager
    {
        private static CustomResourceManager _instance;
        private Dictionary<string, CustomResource> _resources = new Dictionary<string, CustomResource>();
        private ScreenBase _currentPartyScreen;
        private PartyVM _currentPartyVM;
        private List<Tuple<string, int>> _resourceChanges = new List<Tuple<string, int>>();

        private CustomResourceManager() { }

        public static void Initialize() 
        {
            _instance = new CustomResourceManager();
            _instance._resources.Clear();
            _instance._resources.Add("DarkEnergy", 
                new CustomResource("DarkEnergy", "Dark Energy", "Dark Energy is used by practitioners of necromancy to raise and upkeep their undead minions.", "darkenergy_icon_45", "khuzait"));
            _instance._resources.Add("WindsOfMagic",
                new CustomResource("WindsOfMagic", "Winds of Magic", "Winds of Magic is used by spellcasters to cast spells.", "winds_icon_45"));
        }

        public static CustomResource GetResourceObject(string id)
        {
            if (_instance._resources.TryGetValue(id, out CustomResource resource)) { return resource; }
            else return null;
        }

        public static CustomResource GetResourceObject(Func<List<CustomResource>, CustomResource> query)
        {
            return query(_instance._resources.Values.ToList());
        }

        public static bool DoesResourceObjectExist(string id)
        {
            return _instance._resources.TryGetValue(id, out _);
        }

        public static void RegisterEvents()
        {
            ScreenManager.OnPushScreen += ScreenManager_OnPushScreen;
            ScreenManager.OnPopScreen += ScreenManager_OnPopScreen;
        }

        private static void ScreenManager_OnPopScreen(ScreenBase poppedScreen)
        {
            if (poppedScreen == _instance._currentPartyScreen) _instance._currentPartyScreen = null;
        }

        private static void ScreenManager_OnPushScreen(ScreenBase pushedScreen)
        {
            if(pushedScreen is GauntletPartyScreen)
            {
                _instance._currentPartyScreen = pushedScreen;
                _instance._resourceChanges.Clear();
                PartyScreenManager.PartyScreenLogic.PartyScreenClosedEvent += PartyScreenLogic_PartyScreenClosedEvent;
                PartyScreenManager.PartyScreenLogic.AfterReset += PartyScreenLogic_AfterReset;
            }
        }

        private static void PartyScreenLogic_AfterReset(PartyScreenLogic partyScreenLogic, bool fromCancel)
        {
            _instance._resourceChanges.Clear();
            if (_instance._currentPartyVM != null) _instance._currentPartyVM.GetExtension().RefreshValues();
        }

        private static void PartyScreenLogic_PartyScreenClosedEvent(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
        {
            PartyScreenManager.PartyScreenLogic.PartyScreenClosedEvent -= PartyScreenLogic_PartyScreenClosedEvent;
            PartyScreenManager.PartyScreenLogic.AfterReset -= PartyScreenLogic_AfterReset;
            if (!fromCancel)
            {
                foreach(var tuple in _instance._resourceChanges)
                {
                    Hero.MainHero.AddCustomResource(tuple.Item1, -tuple.Item2);
                }
            }
            _instance._resourceChanges.Clear();
            if ((Hero.MainHero.IsVampire() || Hero.MainHero.IsNecromancer()) && PartyScreenManager.Instance.CurrentMode == PartyScreenMode.Loot)
            {
                if (leftMemberRoster != null && leftMemberRoster.TotalManCount > 0)
                {
                    Hero.MainHero.AddCultureSpecificCustomResource(leftMemberRoster.TotalManCount / 10);
                }
                if (leftPrisonRoster != null && leftPrisonRoster.TotalManCount > 0)
                {
                    Hero.MainHero.AddCultureSpecificCustomResource(leftPrisonRoster.TotalManCount / 10);
                }
            }
        }

        public static void OnPartyScreenTroopUpgrade(PartyVM partyVM, PartyScreenLogic.PartyCommand command)
        {
            if(_instance._currentPartyVM != partyVM) _instance._currentPartyVM = partyVM;
            if(command.Code == PartyScreenLogic.PartyCommandCode.UpgradeTroop)
            {
                CharacterObject troopToUpgrade = command.Character;
                CharacterObject upgradeTarget = troopToUpgrade.UpgradeTargets[command.UpgradeTarget];
                var requirement = upgradeTarget.GetCustomResourceRequiredForUpgrade();

                if(requirement != null)
                {
                    _instance._resourceChanges.Add(new Tuple<string, int>(requirement.Item1.StringId, requirement.Item2 * command.TotalNumber));
                    partyVM.GetExtension().RefreshValues();
                }
            }
        }

        public static Dictionary<CustomResource, int> GetPendingResources()
        {
            var dictionary = new Dictionary<CustomResource, int>();
            foreach(var item in _instance._resourceChanges)
            {
                var resource = GetResourceObject(item.Item1);
                if (dictionary.ContainsKey(resource))
                {
                    dictionary[resource] += item.Item2;
                }
                else dictionary.Add(resource, item.Item2);
            }
            return dictionary;
        }
    }
}
