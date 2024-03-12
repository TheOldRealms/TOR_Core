﻿using SandBox;
using SandBox.GauntletUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.ScreenSystem;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomResources
{
    public class CustomResourceManager
    {
        private Dictionary<string, CustomResource> _resources = new Dictionary<string, CustomResource>();
        private ScreenBase _currentPartyScreen;
        private PartyVM _currentPartyVM;
        private List<Tuple<string, int>> _resourceChanges = new List<Tuple<string, int>>();
        public static CustomResourceManager Instance { get; private set; }

        private CustomResourceManager() { }

        public static void Initialize() 
        {
            Instance = new CustomResourceManager();
            Instance._resources.Clear();
            Instance._resources.Add("Prestige", 
                new CustomResource("Prestige", "Prestige", "Is used for upgrading special units of the Empire and special actions.", "prestige_icon_45", "empire"));
            Instance._resources.Add("Chivalry", 
                new CustomResource("Chivalry", "Chivalry", "Is used for upgrading special units of Bretonnia and special actions.", "winds_icon_45", "vlandia"));
            Instance._resources.Add("DarkEnergy", 
                new CustomResource("DarkEnergy", "Dark Energy", "Dark Energy is used by practitioners of necromancy to raise and upkeep their undead minions.", "darkenergy_icon_45",new []{"khuzait", "mousillon"}));
            Instance._resources.Add("WindsOfMagic",
                new CustomResource("WindsOfMagic", "Winds of Magic", "Winds of Magic is used by spellcasters to cast spells.", "winds_icon_45"));
            
        }

        public static CustomResource GetResourceObject(string id)
        {
            if (Instance._resources.TryGetValue(id, out CustomResource resource)) { return resource; }
            else return null;
        }

        public static CustomResource GetResourceObject(Func<List<CustomResource>, CustomResource> query)
        {
            return query(Instance._resources.Values.ToList());
        }

        public static bool DoesResourceObjectExist(string id)
        {
            return Instance._resources.TryGetValue(id, out _);
        }

        public static void RegisterEvents()
        {
            ScreenManager.OnPushScreen += ScreenManager_OnPushScreen;
            ScreenManager.OnPopScreen += ScreenManager_OnPopScreen;
            Instance.RegisterCampaignEvents();
        }

        private void RegisterCampaignEvents()
        {
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, CalculateCustomResourceGainFromBattles);
        }

        private void CalculateCustomResourceGainFromBattles(MapEvent mapEvent)
        {
            float renownChange;

            mapEvent.GetBattleRewards(MobileParty.MainParty.Party, out renownChange, out _, out _, out _, out _);

            if (MobileParty.MainParty.LeaderHero.GetCultureSpecificCustomResource() == GetResourceObject("Prestige"))
            {
                MobileParty.MainParty.LeaderHero.AddCultureSpecificCustomResource((int)(1 + renownChange));
            }
        }

        private static void ScreenManager_OnPopScreen(ScreenBase poppedScreen)
        {
            if (poppedScreen == Instance._currentPartyScreen) Instance._currentPartyScreen = null;
        }

        private static void ScreenManager_OnPushScreen(ScreenBase pushedScreen)
        {
            if(pushedScreen is GauntletPartyScreen)
            {
                Instance._currentPartyScreen = pushedScreen;
                Instance._resourceChanges.Clear();
                PartyScreenManager.PartyScreenLogic.PartyScreenClosedEvent += PartyScreenLogic_PartyScreenClosedEvent;
                PartyScreenManager.PartyScreenLogic.AfterReset += PartyScreenLogic_AfterReset;
            }
        }

        private static void PartyScreenLogic_AfterReset(PartyScreenLogic partyScreenLogic, bool fromCancel)
        {
            Instance._resourceChanges.Clear();
            if (Instance._currentPartyVM != null) Instance._currentPartyVM.GetExtension().RefreshValues();
        }

        private static void PartyScreenLogic_PartyScreenClosedEvent(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
        {
            PartyScreenManager.PartyScreenLogic.PartyScreenClosedEvent -= PartyScreenLogic_PartyScreenClosedEvent;
            PartyScreenManager.PartyScreenLogic.AfterReset -= PartyScreenLogic_AfterReset;
            if (!fromCancel)
            {
                foreach(var tuple in Instance._resourceChanges)
                {
                    Hero.MainHero.AddCustomResource(tuple.Item1, -tuple.Item2);
                }
            }
            Instance._resourceChanges.Clear();
            if ((Hero.MainHero.IsVampire() || Hero.MainHero.CanRaiseDead()) && PartyScreenManager.Instance.CurrentMode == PartyScreenMode.Loot)
            {
                var prisoners = PlayerEncounter.Current.RosterToReceiveLootPrisoners.TotalManCount;
                var totalCausalties = Hero.MainHero.PartyBelongedTo.MapEvent.GetMapEventSide(BattleSideEnum.Defender).Casualties;
               
                
                var result = 0f;

                result += totalCausalties - prisoners;
                if (leftMemberRoster != null && leftMemberRoster.Count > 0)
                {
                    result += AdjustBattleSpoilsForDarkEnergy(leftMemberRoster);
                }

                if (leftPrisonRoster != null && leftPrisonRoster.Count > 0)
                {
                    result += AdjustBattleSpoilsForDarkEnergy(leftPrisonRoster, true);
                }

                Hero.MainHero.AddCultureSpecificCustomResource(result);
            }
        }


        private static float AdjustBattleSpoilsForDarkEnergy(TroopRoster leftUnits, bool isPrisoner=false)
        {
            var explainedNumber = new ExplainedNumber();
            float reduction = 5;
                
            foreach (var troop in leftUnits.GetTroopRoster().ToList())
            {
                if(troop.Character.IsHero) continue;
                
                var level = troop.Character.Level;
                
                explainedNumber.Add(level*troop.Number);
            }

            return explainedNumber.ResultNumber/reduction;
        }

        public static void OnPartyScreenTroopUpgrade(PartyVM partyVM, PartyScreenLogic.PartyCommand command)
        {
            if(Instance._currentPartyVM != partyVM) Instance._currentPartyVM = partyVM;
            if(command.Code == PartyScreenLogic.PartyCommandCode.UpgradeTroop)
            {
                CharacterObject troopToUpgrade = command.Character;
                CharacterObject upgradeTarget = troopToUpgrade.UpgradeTargets[command.UpgradeTarget];
                var requirement = upgradeTarget.GetCustomResourceRequiredForUpgrade(true);

                if(requirement != null)
                {
                    Instance._resourceChanges.Add(new Tuple<string, int>(requirement.Item1.StringId, requirement.Item2 * command.TotalNumber));
                    partyVM.GetExtension().RefreshValues();
                }
            }
        }

        public static Dictionary<CustomResource, int> GetPendingResources()
        {
            var dictionary = new Dictionary<CustomResource, int>();
            foreach(var item in Instance._resourceChanges)
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
