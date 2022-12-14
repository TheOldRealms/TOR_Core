using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class ExtendedInfoManager : CampaignBehaviorBase
    {
        private static Dictionary<string, CharacterExtendedInfo> _characterInfos = new Dictionary<string, CharacterExtendedInfo>();
        private Dictionary<string, HeroExtendedInfo> _heroInfos = new Dictionary<string, HeroExtendedInfo>();
        private static ExtendedInfoManager _instance;

        public static ExtendedInfoManager Instance => _instance;

        public ExtendedInfoManager() => _instance = this;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, FillWindsOfMagic);
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        }

        public static CharacterExtendedInfo GetCharacterInfoFor(string id)
        {
            return _characterInfos.ContainsKey(id) ? _characterInfos[id] : null;
        }

        public HeroExtendedInfo GetHeroInfoFor(string id)
        {
            return _heroInfos.ContainsKey(id) ? _heroInfos[id] : null;
        }

        public void ClearInfo(Hero hero)
        {
            if (_heroInfos.ContainsKey(hero.GetInfoKey()))
            {
                _heroInfos[hero.GetInfoKey()] = new HeroExtendedInfo(hero.CharacterObject);
            }
        }

        private void OnSessionStart(CampaignGameStarter obj)
        {
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(out _characterInfos);
        }

        private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter campaignGameStarter)
        {
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(out _characterInfos);
            InitializeHeroes();
        }

        private void FillWindsOfMagic()
        {
            foreach (var entry in _heroInfos)
            {
                if (entry.Value.AllAttributes.Contains("SpellCaster"))
                {
                    var hero = Hero.FindFirst(x => x.StringId == entry.Key);
                    float bonusRegen = 1f;
                    if(hero != null && hero.GetPerkValue(TORPerks.SpellCraft.Catalyst) && hero.CurrentSettlement != null && hero.CurrentSettlement.IsTown)
                    {
                        bonusRegen += TORPerks.SpellCraft.Catalyst.SecondaryBonus * 0.01f;
                    }
                    entry.Value.CurrentWindsOfMagic += entry.Value.WindsOfMagicRechargeRate * bonusRegen;
                    entry.Value.CurrentWindsOfMagic = Math.Min(entry.Value.CurrentWindsOfMagic, entry.Value.MaxWindsOfMagic);
                }
            }
        }

        private void OnHeroCreated(Hero hero, bool arg2)
        {
            if (!_heroInfos.ContainsKey(hero.GetInfoKey()))
            {
                var info = new HeroExtendedInfo(hero.CharacterObject);
                _heroInfos.Add(hero.GetInfoKey(), info);
                if (hero.Template != null) InitializeTemplatedHeroStats(hero);
            }
        }

        private void OnHeroKilled(Hero arg1, Hero arg2, KillCharacterAction.KillCharacterActionDetail arg3, bool arg4)
        {
            if (_heroInfos.ContainsKey(arg1.GetInfoKey()))
            {
                _heroInfos.Remove(arg1.GetInfoKey());
            }
        }

        public static void CreateDefaultInstanceAndLoad()
        {
            _ = new ExtendedInfoManager();
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(out _characterInfos);
        }

        private static void TryLoadCharacters(out Dictionary<string, CharacterExtendedInfo> infos)
        {
            //construct character info for all CharacterObject templates loaded by the game.
            //this can be safely reconstructed at each session start without the need to save/load.
            Dictionary<string, CharacterExtendedInfo> unitlist = new Dictionary<string, CharacterExtendedInfo>();
            infos = unitlist;
            try
            {
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_extendedunitproperties.xml";
                if (File.Exists(path))
                {
                    var ser = new XmlSerializer(typeof(List<CharacterExtendedInfo>));
                    var list = ser.Deserialize(File.OpenRead(path)) as List<CharacterExtendedInfo>;
                    foreach (var item in list)
                    {
                        if (!infos.ContainsKey(item.CharacterStringId))
                        {
                            infos.Add(item.CharacterStringId, item);
                        }
                    }
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
                if (!_heroInfos.ContainsKey(hero.GetInfoKey()))
                {
                    var info = new HeroExtendedInfo(hero.CharacterObject);
                    _heroInfos.Add(hero.GetInfoKey(), info);
                }
            }
        }

        private static void InitializeTemplatedHeroStats(Hero hero)
        {
            var template = hero.Template;
            int castingLevel = 0;
            if (template.IsTORTemplate() && template.Occupation == Occupation.Wanderer)
            {
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
                            castingLevel = Math.Max(castingLevel, abilityobj.SpellTier);
                        }
                    }
                }
                hero.SetSpellCastingLevel((SpellCastingLevel)castingLevel);
                if (hero.IsSpellCaster() && !info.KnownLores.Contains(LoreObject.GetLore("MinorMagic"))) hero.AddKnownLore("MinorMagic");
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_heroInfos", ref _heroInfos);
        }

    }
}
