using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.SettlementPOIs;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CompanionRecruitment
{
    /// <summary>
    /// Shared companion-recruitment plumbing. Discovers hosts implementing
    /// <see cref="ICompanionRecruitmentHost"/> on session launch and owns a
    /// rotating "current offer" slot per <see cref="CompanionRecruitmentConfig"/>.
    /// Each daily tick, slots that are empty beyond their refresh window
    /// repopulate with a random pick from the candidate list, and lingering
    /// un-recruited offers are re-rolled after the same interval. Recruiting
    /// runs the vanilla hero-spawn recipe
    /// (<see cref="HeroCreator.CreateSpecialHero"/> +
    /// <see cref="AddCompanionAction"/> +
    /// <see cref="AddHeroToPartyAction"/>) and clears the slot.
    /// </summary>
    public class CompanionRecruitmentBehavior : CampaignBehaviorBase
    {
        // Slot state per CompanionRecruitmentConfig.DialogSuffix. "Current
        // offer absent" = suffix missing from _currentOfferPerConfig. Both
        // dicts are saved; the config list is rebuilt on session launch.
        private Dictionary<string, string> _currentOfferPerConfig = new();
        private Dictionary<string, CampaignTime> _lastRefreshPerConfig = new();

        private readonly List<CompanionRecruitmentConfig> _configs = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_companion_recruit_current_offer", ref _currentOfferPerConfig);
            dataStore.SyncData("_companion_recruit_last_refresh",  ref _lastRefreshPerConfig);
            _currentOfferPerConfig ??= new Dictionary<string, string>();
            _lastRefreshPerConfig  ??= new Dictionary<string, CampaignTime>();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _configs.Clear();

            foreach (var host in SettlementPOIManagerCampaignBehavior.All.OfType<ICompanionRecruitmentHost>())
            {
                foreach (var cfg in host.GetCompanionRecruits())
                {
                    if (string.IsNullOrEmpty(cfg.DialogSuffix)) continue;
                    if (cfg.CandidateTemplateIds.Count == 0) continue;
                    if (_configs.Any(c => c.DialogSuffix == cfg.DialogSuffix)) continue;
                    _configs.Add(cfg);
                }
            }

            foreach (var cfg in _configs)
            {
                // Fresh campaigns / new configs: seed an offer so day-1 chapterhouse
                // visits show a recruit. Existing saves preserve whatever the
                // slot held.
                if (!_currentOfferPerConfig.ContainsKey(cfg.DialogSuffix)
                    && !_lastRefreshPerConfig.ContainsKey(cfg.DialogSuffix))
                {
                    _currentOfferPerConfig[cfg.DialogSuffix] = PickRandom(cfg, null);
                    _lastRefreshPerConfig[cfg.DialogSuffix]  = CampaignTime.Now;
                }

                RegisterDialogLines(starter, cfg);
            }
        }

        private void OnDailyTick()
        {
            foreach (var cfg in _configs)
            {
                if (!_lastRefreshPerConfig.TryGetValue(cfg.DialogSuffix, out var last)) continue;
                if ((CampaignTime.Now - last).ToDays < cfg.RefreshDays) continue;

                var hasOffer = _currentOfferPerConfig.TryGetValue(cfg.DialogSuffix, out var current)
                               && !string.IsNullOrEmpty(current);

                // Empty slot → fill. Non-empty lingering offer → re-roll to
                // a different template if the pool has more than one entry.
                _currentOfferPerConfig[cfg.DialogSuffix] = PickRandom(cfg, hasOffer ? current : null);
                _lastRefreshPerConfig[cfg.DialogSuffix]  = CampaignTime.Now;
            }
        }

        private static string PickRandom(CompanionRecruitmentConfig cfg, string avoid)
        {
            var pool = cfg.CandidateTemplateIds;
            if (pool.Count == 1) return pool[0];

            // Try up to 4 picks to avoid repeating. Cheap, bounded, and fine
            // for pools of any realistic size.
            for (int i = 0; i < 4; i++)
            {
                var pick = pool[MBRandom.RandomInt(0, pool.Count)];
                if (pick != avoid) return pick;
            }
            return pool[MBRandom.RandomInt(0, pool.Count)];
        }

        private void RegisterDialogLines(CampaignGameStarter starter, CompanionRecruitmentConfig cfg)
        {
            var suffix = cfg.DialogSuffix;

            starter.AddPlayerLine(
                "tor_companion_recruit_ask_" + suffix,
                cfg.HubState,
                "tor_companion_recruit_offer_" + suffix,
                TORTextHelper.GetTextForNative("tor_companion_recruit", "player_ask_" + suffix,
                    "I seek a worthy recruit willing to ride with me."),
                () => ConditionAsk(cfg), null, 200);

            starter.AddDialogLine(
                "tor_companion_recruit_available_" + suffix,
                "tor_companion_recruit_offer_" + suffix,
                cfg.ReintroState,
                TORTextHelper.GetTextForNative("tor_companion_recruit", "offer_available_" + suffix,
                    "{COMPANION_INTRO} stands ready. They will serve with you."),
                () => ConditionAvailable(cfg),
                () => ConsequenceRecruit(cfg),
                200);

            starter.AddDialogLine(
                "tor_companion_recruit_refill_" + suffix,
                "tor_companion_recruit_offer_" + suffix,
                cfg.ReintroState,
                TORTextHelper.GetTextForNative("tor_companion_recruit", "offer_refill_" + suffix,
                    "No one is ready right now. Return in {COMPANION_DAYS} days."),
                () => ConditionRefill(cfg),
                null,
                200);

            if (cfg.GoldCost > 0 || cfg.CustomResourceCost > 0)
            {
                starter.AddDialogLine(
                    "tor_companion_recruit_broke_" + suffix,
                    "tor_companion_recruit_offer_" + suffix,
                    cfg.ReintroState,
                    TORTextHelper.GetTextForNative("tor_companion_recruit", "offer_broke_" + suffix,
                        "You cannot afford what the arrangement costs."),
                    () => ConditionCannotAfford(cfg),
                    null,
                    200);
            }
        }

        // ---- Conditions

        private bool ConditionAsk(CompanionRecruitmentConfig cfg)
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner?.HeroObject?.Template == null) return false;
            if (partner.HeroObject.Template.StringId != cfg.HostTemplateId) return false;

            var main = Hero.MainHero;
            if (main == null) return false;

            foreach (var attr in cfg.GateAttributes)
                if (!main.HasAttribute(attr)) return false;

            return true;
        }

        private bool ConditionAvailable(CompanionRecruitmentConfig cfg)
        {
            if (!TryGetCurrentOffer(cfg, out var templateId)) return false;
            if (!CanAfford(cfg)) return false;

            var template = MBObjectManager.Instance.GetObject<CharacterObject>(templateId);
            MBTextManager.SetTextVariable("COMPANION_INTRO", template != null ? template.Name : new TextObject("A recruit"));
            return true;
        }

        private bool ConditionRefill(CompanionRecruitmentConfig cfg)
        {
            if (TryGetCurrentOffer(cfg, out _)) return false;
            MBTextManager.SetTextVariable("COMPANION_DAYS", DaysUntilRefresh(cfg));
            return true;
        }

        private bool ConditionCannotAfford(CompanionRecruitmentConfig cfg)
        {
            if (!TryGetCurrentOffer(cfg, out _)) return false;
            return !CanAfford(cfg);
        }

        private bool TryGetCurrentOffer(CompanionRecruitmentConfig cfg, out string templateId)
        {
            if (_currentOfferPerConfig.TryGetValue(cfg.DialogSuffix, out templateId)
                && !string.IsNullOrEmpty(templateId))
                return true;
            templateId = null;
            return false;
        }

        private int DaysUntilRefresh(CompanionRecruitmentConfig cfg)
        {
            if (!_lastRefreshPerConfig.TryGetValue(cfg.DialogSuffix, out var last)) return 0;
            var days = cfg.RefreshDays - (CampaignTime.Now - last).ToDays;
            return Math.Max(0, (int)Math.Ceiling(days));
        }

        private static bool CanAfford(CompanionRecruitmentConfig cfg)
        {
            var main = Hero.MainHero;
            if (cfg.GoldCost > 0 && main.Gold < cfg.GoldCost) return false;
            if (cfg.CustomResourceCost > 0 && main.GetCultureSpecificCustomResourceValue() < cfg.CustomResourceCost) return false;
            return true;
        }

        // ---- Consequence

        private void ConsequenceRecruit(CompanionRecruitmentConfig cfg)
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement == null) return;
            if (!TryGetCurrentOffer(cfg, out var templateId)) return;

            var template = MBObjectManager.Instance.GetObject<CharacterObject>(templateId);
            if (template == null)
            {
                TORCommon.Say("CompanionRecruitment: template '" + templateId + "' not found.");
                return;
            }

            var age = Campaign.Current.Models.AgeModel.HeroComesOfAge + 5 + MBRandom.RandomInt(12);
            var hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, age);
            if (hero == null) return;

            hero.ChangeState(Hero.CharacterStates.Active);
            hero.Clan = Clan.PlayerClan;
            AddCompanionAction.Apply(Clan.PlayerClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty, showNotification: true);

            if (cfg.GoldCost > 0)            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cfg.GoldCost);
            if (cfg.CustomResourceCost > 0)  Hero.MainHero.AddCultureSpecificCustomResource(-cfg.CustomResourceCost);

            // Slot empties; the daily tick will roll a replacement after RefreshDays.
            _currentOfferPerConfig.Remove(cfg.DialogSuffix);
            _lastRefreshPerConfig[cfg.DialogSuffix] = CampaignTime.Now;

            var msg = TORTextHelper.GetTextObject(
                "tor_companion_recruit_joined",
                "{HERO_NAME} has joined your company.");
            msg.SetTextVariable("HERO_NAME", hero.Name);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString()));
        }
    }
}
