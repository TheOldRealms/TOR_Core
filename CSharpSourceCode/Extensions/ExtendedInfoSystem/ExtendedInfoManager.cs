using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class ExtendedInfoManager : CampaignBehaviorBase
    {
        private static Dictionary<string, CharacterExtendedInfo> _characterInfos = [];
        private Dictionary<string, HeroExtendedInfo> _heroInfos = [];
        private Dictionary<string, MobilePartyExtendedInfo> _partyInfos = [];
        private static Dictionary<string, string> _bannerResources = [];
        private static ExtendedInfoManager _instance;
        private static readonly Dictionary<string, List<string>> _settlementInfos = [];
        private readonly string _torUnitPropertiesPath = TORPaths.TORCoreModuleExtendedDataPath + "tor_extendedunitproperties.xml";

        public static ExtendedInfoManager Instance => _instance;

        public ExtendedInfoManager() => _instance = this;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, CheckToAddPartyInfo);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnPartyDestroyed);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnQuarterDailyPartyTick.AddNonSerializedListener(this, QuarterDailyTick);
            CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, PlayerTroopUpgraded);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, PlayerBattleEnd);
            CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, PlayerTroopRecruitment);

            CustomResourceManager.RegisterEvents();
        }

        private void PlayerTroopRecruitment(CharacterObject troop, int count)
        {
            var partyInfo = MobileParty.MainParty.GetPartyInfo();
            if (partyInfo == null) return; //if player info is null then there's an issue elsewhere
            CheckToAddTroopToPartyInfo(partyInfo, troop);
        }

        private void PlayerBattleEnd(MapEvent obj)
        {
            var parties = obj.PartiesOnSide(obj.PlayerSide);

            foreach (var party in parties)
            {
                if (party.Party.MobileParty != MobileParty.MainParty) continue;
                if (party.Party.MobileParty == null) continue;

                ValidatePartyInfos(party.Party.MobileParty);
            }
        }

        private void PlayerTroopUpgraded(CharacterObject from, CharacterObject to, int count)
        {
            ValidatePartyInfos(MobileParty.MainParty);
        }

        private static void QuarterDailyTick(MobileParty mobileParty)
        {
            if (mobileParty != MobileParty.MainParty) return;
            PunishmentForMissingResource(mobileParty);
        }

        private static void PunishmentForMissingResource(MobileParty mobileParty)
        {
            var hero = mobileParty.LeaderHero;
            if (hero == null) return;
            if (hero.GetCultureSpecificCustomResourceValue() <= 0)
            {
                if (hero.IsNecromancer() || hero.IsVampire())
                {
                    var upkeep = hero.GetCalculatedCustomResourceUpkeep("DarkEnergy");
                    hero.AddWindsOfMagic(upkeep * 3); //takes winds
                }
            }
        }

        private void DailyTick()
        {
            //Sly : until(if) custom resources are ever implemented for AI heroes, there's no point in calculating the resource upkeep for every hero.
            //foreach (var entry in _heroInfos)
            //{
            var hero = Hero.MainHero; //Hero.FindFirst(x => x.StringId == entry.Key);
            var resource = hero.GetCultureSpecificCustomResource();
            if (resource != null && _heroInfos.TryGetValue(hero.StringId, out var heroInfo))
            {
                var id = resource.StringId;
                var resourceChange = hero.GetCultureSpecificCustomResourceChange(id);
                heroInfo.AddCustomResource(id, resourceChange);
            }
            //}
            //only the player uses the TroopAttribute dictionary which is what is validated - it's used for storing temporary troop attributes granted by careers like waywatcher, runesmith, etc... Extended units attributes are in the _characterInfos dictionary which reads from the xml.
            //foreach (var entry in _partyInfos)
            //{
            //var party = Campaign.Current.LordParties.FirstOrDefault(x => x.StringId == entry.Key);

            ValidatePartyInfos(MobileParty.MainParty);
            //}
        }

        public void ValidatePartyInfos(MobileParty party)
        {
            if (!_partyInfos.TryGetValue(party.StringId, out var partyInfo)) return;

            if (partyInfo.TroopAttributes == null)
            {
                partyInfo.TroopAttributes = [];
            }

            var roster = party.MemberRoster.GetTroopRoster();

            foreach (var troopAttribute in partyInfo.TroopAttributes.Keys.Reverse())
            {
                if (roster.All(x => x.Character.StringId != troopAttribute))
                {
                    CareerHelper.RemoveCareerRelatedTroopAttributes(party, troopAttribute, partyInfo); //Zerca mentioned this was related to an issue with powerstones not getting removed in some circumstances
                    partyInfo.TroopAttributes.Remove(troopAttribute);
                }
            }

            foreach (var element in roster)
            {
                CheckToAddTroopToPartyInfo(partyInfo, element.Character);
            }
        }

        /// <remarks>
        /// TroopAttributes stores applied temporary attributes (ones not in the extended unit info xml) such as knight seals, magister powerstones, waywatcher arrows, etc...
        /// </remarks>
        public void CheckToAddTroopToPartyInfo(MobilePartyExtendedInfo partyInfo, CharacterObject troop)
        {
            if (!partyInfo.TroopAttributes.TryGetValue(troop.StringId, out _))
            {
                partyInfo.TroopAttributes.Add(troop.StringId, []);
            }
        }

        public static CharacterExtendedInfo GetCharacterInfoFor(string characterId)
        {
            if (_characterInfos.TryGetValue(characterId, out CharacterExtendedInfo value))
            {
                return value;
            }
            return null;
        }

        public HeroExtendedInfo GetHeroInfoFor(string heroId)
        {
            if (_heroInfos.TryGetValue(heroId, out HeroExtendedInfo value))
            {
                return value;
            }
            return null;
        }

        public MobilePartyExtendedInfo GetPartyInfoFor(string partyId)
        {
            if (_partyInfos.TryGetValue(partyId, out MobilePartyExtendedInfo value))
            {
                return value;
            }
            return null;
        }

        public void ClearInfo(Hero hero)
        {
            if (_heroInfos.ContainsKey(hero.GetInfoKey()))
            {
                _heroInfos[hero.GetInfoKey()] = new HeroExtendedInfo(hero.CharacterObject);
            }
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(_torUnitPropertiesPath, out _characterInfos);
        }

        private void OnSessionStart(CampaignGameStarter obj)
        {
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(_torUnitPropertiesPath, out _characterInfos);
            var success = _characterInfos.Any(x => x.Value.ResourceCost != null);
            EnsurePartyInfos();
            HideVanillaUnitsInEncyclopedia();
        }

        private void HideVanillaUnitsInEncyclopedia()
        {
            MBObjectManager.Instance.GetObjectTypeList<CharacterObject>().ForEach(x =>
            {
                if (!x.IsTORTemplate() && x.Occupation == Occupation.Soldier)
                {
                    x.HiddenInEncyclopedia = true;
                }
            });
        }

        /// <remarks>
        /// Index is just before a similar event in ReligionCampaignBehavior to create ExtendedInfo entries for every named noble as the hero creation for them does not dispatch a OnHeroCreated event.
        /// </remarks>
        private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter campaignGameStarter, int index)
        {
            if (index == CampaignEvents.OnNewGameCreatedPartialFollowUpEventMaxIndex - 2)
            {
                InitializeHeroes();
                EnsurePartyInfos();
            }
        }

        private void HourlyTick() //I wonder if this could be done on the quarter daily tick instead and reduce the load significantly? The quarter tick is party-based, it could be performed on the main party's quarter tick, ie QuarterDailyTick above.
        {
            //fill winds of magic
            foreach (var hero in Hero.AllAliveHeroes)
            {
                if (hero.IsNotable)
                    continue;
                if (!_heroInfos.TryGetValue(hero.GetInfoKey(), out var heroInfo))
                    continue;
                if (!heroInfo.AllAttributes.Contains("SpellCaster"))
                    continue;

                var bonusRegen = 1f;
                if (hero.GetPerkValue(TORPerks.Spellcraft.Catalyst) &&
                    hero.CurrentSettlement != null &&
                    hero.CurrentSettlement.IsTown)
                {
                    bonusRegen += TORPerks.Spellcraft.Catalyst.SecondaryBonus;
                }

                heroInfo.AddCustomResource("WindsOfMagic", heroInfo.WindsOfMagicRechargeRate * bonusRegen);
            }
            //count down blessing duration
            foreach (var entry in _partyInfos)
            {
                if (!string.IsNullOrWhiteSpace(entry.Value.CurrentBlessingStringId) && entry.Value.CurrentBlessingRemainingDuration > 0)
                {
                    entry.Value.CurrentBlessingRemainingDuration--;
                }
                else
                {
                    entry.Value.CurrentBlessingRemainingDuration = -1;
                    entry.Value.CurrentBlessingStringId = null;
                }
            }
        }

        /// <remarks>
        /// Event is dispatched when a hero is created from template (HeroCreator.Create[]Hero) - heroes with a unique character object defined in xml will not dispatch this event because Hero.Deserialize has no event dispatches.
        /// On campaign creation, templated heroes are created, and therefore this is triggered for them, before OnNewGameCreatedPartialFollowUpEnd would call InitializeHeroes.
        /// </remarks>
        private void OnHeroCreated(Hero hero, bool bornNaturally)
        {
            if (hero.IsNotable) return;
            if (!_heroInfos.ContainsKey(hero.GetInfoKey()))
            {
                var info = new HeroExtendedInfo(hero.CharacterObject);
                _heroInfos.Add(hero.GetInfoKey(), info);
                TORCampaignEvents.Instance.OnHeroExtendedInfoCreated(hero);
                if (hero.Template != null) InitializeTemplatedHeroStats(hero);
                hero.AddCultureSpecificCustomResource(0);
            }
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail actionDetail, bool showNotification)
        {
            //should perform a similar check to whatever is required to be added to the dictionary in the first place - idk if notables are being checked against this
            if (victim.IsNotable) return;
            if (_heroInfos.ContainsKey(victim.GetInfoKey()))
            {
                _heroInfos.Remove(victim.GetInfoKey());
            }
        }

        public static void CreateDefaultInstanceAndLoad()
        {
            var manager = new ExtendedInfoManager();
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(manager._torUnitPropertiesPath, out _characterInfos);
        }

        /// <summary>
        /// Loads an xml containing the extended CharacterObject properties to be added or overwritten.
        /// </summary>
        /// <param name="filepath">A file path including the file type extension of the target xml.</param>
        /// <param name="infos">Argument used to verify if the loading was a success. This should just be evaluated in the method and a bool put out instead of the whole dictionary.</param>
        /// <remarks>
        /// Loading an empty entry from xml for a given id will clear the prior properties.
        /// </remarks>
        public static void TryLoadCharacters(in string filepath, out Dictionary<string, CharacterExtendedInfo> infos)
        {
            //construct character info for all CharacterObject templates loaded by the game.
            //this can be safely reconstructed at each session start without the need to save/load.
            Dictionary<string, CharacterExtendedInfo> unitlist = [];
            infos = unitlist;
            try
            {
                if (File.Exists(filepath))
                {
                    TORCommon.Log("ExtendedInfoManager : loading extended troop properties from : " + filepath, LogLevel.Warn);
                    var ser = new XmlSerializer(typeof(List<CharacterExtendedInfo>));
                    var list = ser.Deserialize(File.OpenRead(filepath)) as List<CharacterExtendedInfo>;
                    foreach (var item in list)
                    {
                        if (item == null || item.CharacterStringId == null)
                        {
                            TORCommon.Log("ExtendedInfoManager : TryLoadCharacters item or stringID null. \n" + (item == null ? ("Item is null") : item.ToString() + " is null"), LogLevel.Debug);
                        }
                        else if (infos.TryGetValue(item.CharacterStringId, out _))
                        {
                            TORCommon.Log("ExtendedInfoManager : TryLoadCharacters item already in dictionary overriding prior properties. \n" + "Character : " + item.CharacterStringId, LogLevel.Debug);
                            infos[item.CharacterStringId] = item;
                        }
                        else
                        {
                            infos.Add(item.CharacterStringId, item);
                        }
                    }
                }
                else
                {
                    TORCommon.Log("ExtendedInfoManager : No extended troop info xml found at : " + filepath, LogLevel.Warn);
                }
            }
            catch (Exception e)
            {
                TORCommon.Log(e.ToString(), LogLevel.Error);
                throw e; //TODO handle this more gracefully.
            }
        }

        private void InitializeHeroes()
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                if (hero.IsNotable) continue;
                if (!_heroInfos.TryGetValue(hero.GetInfoKey(), out _))
                {
                    var info = new HeroExtendedInfo(hero.CharacterObject);
                    _heroInfos.Add(hero.GetInfoKey(), info);
                    hero.AddCultureSpecificCustomResource(0);
                }
            }
        }

        private static void InitializeTemplatedHeroStats(Hero hero)
        {
            var template = hero.Template;
            int castingLevel = 0;
            if (template.IsTORTemplate() && template.Occupation == Occupation.Wanderer)
            {

                //these for loops are a verification - during HeroExtendedInfo initialization, attributes and abilities will already be added due to the template serving as a reference and _characterInfos having everything it needs from the unit info xml
                var info = hero.GetExtendedInfo();

                if (info == null) return;


                foreach (var attribute in template.GetAttributes())
                {
                    hero.AddAttribute(attribute);
                }
                foreach (var ability in template.GetAbilities())
                {
                    hero.AddAbility(ability);
                    var abilityobj = AbilityFactory.GetTemplate(ability);
                    if (abilityobj.IsSpell)
                    {
                        if (!info.KnownLores.Contains(LoreObject.GetLore(abilityobj.BelongsToLoreID)))
                        {
                            hero.AddKnownLore(abilityobj.BelongsToLoreID);
                        }
                        castingLevel = Math.Max(castingLevel, abilityobj.SpellTier);
                    }
                }

                //OnPerkPicked occurs before hero info is created so we verify manually after its creation
                if (hero.IsSpellCaster())
                {
                    if (!info.KnownLores.Contains(LoreObject.GetLore("MinorMagic"))) hero.AddKnownLore("MinorMagic");
                    if (hero.GetPerkValue(TORPerks.Spellcraft.MasterSpells)) castingLevel = 4;
                    else if (hero.GetPerkValue(TORPerks.Spellcraft.AdeptSpells)) castingLevel = 3;
                    else if (hero.GetPerkValue(TORPerks.Spellcraft.EntrySpells)) castingLevel = 2;
                    else if (castingLevel < 1) castingLevel = 1; //minimally Minor since they're getting the lore
                }
                if (hero.IsPriest())
                {
                    var prayerLevel = 0;
                    if (hero.GetPerkValue(TORPerks.Faith.GrandPrayers)) prayerLevel = 4;
                    else if (hero.GetPerkValue(TORPerks.Faith.AdeptPrayers)) prayerLevel = 3;
                    else if (hero.GetPerkValue(TORPerks.Faith.NovicePrayers)) prayerLevel = 2;//all (God)Devoted traits grant 25 Faith so this should always be the effective prayer level
                    var prayerList = CareerHelper.GetPriestPrayerList(hero);
                    foreach (var prayer in prayerList)
                    {
                        if (prayer.Rank <= prayerLevel)
                        {
                            hero.AddAbility(prayer.PrayerID);
                        }
                    }
                }
                hero.SetSpellCastingLevel((SpellCastingLevel)castingLevel);
            }
        }

        private void CheckToAddPartyInfo(MobileParty party)
        {
            if (party.IsLordParty && !_partyInfos.TryGetValue(party.StringId, out _))
            {
                _partyInfos.Add(party.StringId, new MobilePartyExtendedInfo());
            }
        }

        private void OnPartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            if (destroyedParty.IsLordParty && _partyInfos.TryGetValue(destroyedParty.StringId, out _)) _partyInfos.Remove(destroyedParty.StringId);
        }

        private void EnsurePartyInfos()
        {
            foreach (var party in MobileParty.AllLordParties)
            {
                CheckToAddPartyInfo(party);
            }
        }

        public void AddBlessingToParty(MobileParty party, string blessingId, int duration)
        {
            CheckToAddPartyInfo(party);
            var partyId = party.StringId;
            _partyInfos[partyId].CurrentBlessingStringId = blessingId;
            _partyInfos[partyId].CurrentBlessingRemainingDuration = duration;
        }

        public static string GetBannerImageResource(Banner banner)
        {
            var bannerCode = banner.Serialize();
            if (_bannerResources.TryGetValue(bannerCode, out var resource))
            {
                return resource;
            }
            else return null;
        }

        public static void AddBannerImageResource(string bannerCode, string imageResource)
        {
            _bannerResources.AddOrReplace(bannerCode, imageResource);
        }

        public static void AddSettlementInfo(Settlement settlement, string tag)
        {
            if (_settlementInfos.TryGetValue(settlement.StringId, out var list))
            {
                if (!list.Contains(tag)) list.Add(tag);
            }
            else
            {
                _settlementInfos.Add(settlement.StringId, [tag]);
            }
        }

        public static List<string> GetSettlementInfo(Settlement settlement)
        {
            if (_settlementInfos.TryGetValue(settlement.StringId, out var list)) return list;
            else return [];
        }

        public void AddAttributeToCharacterOfParty(string partyId, CharacterObject characterObject, string attribute)
        {
            _partyInfos[partyId].AddTroopAttribute(characterObject, attribute);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_heroInfos", ref _heroInfos);
            dataStore.SyncData("_partyInfos", ref _partyInfos);
            dataStore.SyncData("_bannerResources", ref _bannerResources);
        }
    }
}