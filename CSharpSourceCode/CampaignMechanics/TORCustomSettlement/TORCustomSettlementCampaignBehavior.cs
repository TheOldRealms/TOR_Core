using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public class TORCustomSettlementCampaignBehavior : CampaignBehaviorBase
    {
        private CampaignTime _startWaitTime = CampaignTime.Now;
        private int _numberOfTroops = 0;
        [SaveableField(0)] private Dictionary<string, bool> _customSettlementActiveStates = new Dictionary<string, bool>();
        [SaveableField(1)] private Dictionary<string, int> _cursedSiteWardDurationLeft = new Dictionary<string, int>();
        [SaveableField(2)] private Dictionary<string, int> _lastGhostRecruitmentTime = new Dictionary<string, int>();
        private TORFaithModel _model;

        public static MBReadOnlyList<Settlement> AllCustomSettlements = new MBReadOnlyList<Settlement>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, OnSettlementHourlyTick);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
            CampaignEvents.TickPartialHourlyAiEvent.AddNonSerializedListener(this, OnAiTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameStart);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, CollectSettlementData);
        }

        private void CollectSettlementData()
        {
            var customSettlements = Settlement.FindAll(x => x.SettlementComponent is TORBaseSettlementComponent);
            AllCustomSettlements = new MBReadOnlyList<Settlement>(customSettlements);
            foreach (var settlement in customSettlements)
            {
                var comp = settlement.SettlementComponent as TORBaseSettlementComponent;
                if (!_customSettlementActiveStates.ContainsKey(settlement.StringId))
                {
                    _customSettlementActiveStates.Add(settlement.StringId, comp.IsActive);
                }
                else
                {
                    _customSettlementActiveStates[settlement.StringId] = comp.IsActive;
                }

                if(comp is CursedSiteComponent)
                {
                    if (!_cursedSiteWardDurationLeft.ContainsKey(settlement.StringId))
                    {
                        _cursedSiteWardDurationLeft.Add(settlement.StringId, ((CursedSiteComponent)comp).WardHours);
                    }
                    else
                    {
                        _cursedSiteWardDurationLeft[settlement.StringId] = ((CursedSiteComponent)comp).WardHours;
                    }
                }
            }
        }

        private void OnNewGameStart(CampaignGameStarter starter)
        {
            var customSettlements = Settlement.FindAll(x => x.SettlementComponent is TORBaseSettlementComponent);
            foreach (var settlement in customSettlements)
            {
                var comp = settlement.SettlementComponent as TORBaseSettlementComponent;
                comp.IsActive = true;
            }
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party == MobileParty.MainParty) return;
            if(settlement.SettlementComponent is ShrineComponent)
            {
                var shrine = settlement.SettlementComponent as ShrineComponent;
                party.AddBlessingToParty(shrine.Religion.StringId, _model.CalculateBlessingDurationForParty(party));
                party.LeaderHero.AddSkillXp(TORSkills.Faith, _model.CalculateSkillXpForPraying(party.LeaderHero));
                party.LeaderHero.AddReligiousInfluence(shrine.Religion, _model.CalculateDevotionIncreaseForPraying(party.LeaderHero));
                LeaveSettlementAction.ApplyForParty(party);
                party.Ai.SetMoveModeHold();
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.Ai.RethinkAtNextHourlyTick = true;
            }
            else if(settlement.SettlementComponent is CursedSiteComponent)
            {
                var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_spirit_host");
                var freeSlots = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
                int raisePower = Math.Max(1, (int)party.LeaderHero.GetExtendedInfo().SpellCastingLevel);
                var count = MBRandom.RandomInt(0, 8);
                count *= raisePower;
                if (freeSlots > 0)
                {
                    if (freeSlots < count) count = freeSlots;
                    party.MemberRoster.AddToCounts(troop, count);
                    CampaignEventDispatcher.Instance.OnTroopRecruited(party.LeaderHero, settlement, null, troop, count);
                }
                LeaveSettlementAction.ApplyForParty(party);
                if (_lastGhostRecruitmentTime.ContainsKey(party.LeaderHero.StringId))
                {
                    _lastGhostRecruitmentTime[party.LeaderHero.StringId] = (int)CampaignTime.Now.ToDays;
                }
                else
                {
                    _lastGhostRecruitmentTime.Add(party.LeaderHero.StringId, (int)CampaignTime.Now.ToDays);
                }
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.Ai.RethinkAtNextHourlyTick = true;
            }
        }

        private void OnAiTick(MobileParty party)
        {
            if (party == MobileParty.MainParty) return;
            if (CanPartyGoToShrine(party))
            {
                var settlements = TORCommon.FindSettlementsAroundPosition(party.Position2D, 20, x => x.SettlementComponent is ShrineComponent);
                if(settlements.Count > 0)
                {
                    var shrine = settlements.First().SettlementComponent as ShrineComponent;
                    if(party.LeaderHero.GetDominantReligion() == shrine.Religion)
                    {
                        party.Ai.SetMoveGoToSettlement(settlements.First());
                        party.Ai.SetDoNotMakeNewDecisions(true);
                    }
                }
            }
            if (CanPartyRecruitGhosts(party))
            {
                var settlements = TORCommon.FindSettlementsAroundPosition(party.Position2D, 20, x => x.SettlementComponent is CursedSiteComponent);
                if (settlements.Count > 0)
                {
                    party.Ai.SetMoveGoToSettlement(settlements.First());
                    party.Ai.SetDoNotMakeNewDecisions(true);
                }
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _model = Campaign.Current.Models.GetFaithModel();
            AddChaosPortalMenus(starter);
            AddShrineMenus(starter);
            AddCursedSiteMenus(starter);
            foreach(var entry in _customSettlementActiveStates)
            {
                var settlement = Settlement.Find(entry.Key);
                if(settlement != null && settlement.SettlementComponent is TORBaseSettlementComponent)
                {
                    var comp = settlement.SettlementComponent as TORBaseSettlementComponent;
                    comp.IsActive = entry.Value;
                    if(comp is CursedSiteComponent && _cursedSiteWardDurationLeft.ContainsKey(settlement.StringId))
                    {
                        ((CursedSiteComponent)comp).WardHours = _cursedSiteWardDurationLeft[entry.Key];
                    }
                }
            }
            CollectSettlementData();
        }

        private void OnSettlementHourlyTick(Settlement settlement)
        {
            if(settlement.SettlementComponent is CursedSiteComponent)
            {
                var site = settlement.SettlementComponent as CursedSiteComponent;
                if (site.IsActive)
                {
                    var affectedParties = TORCommon.FindPartiesAroundPosition(settlement.Position2D, TORConstants.DEFAULT_CURSE_RADIUS, x => x.IsLordParty && x.LeaderHero != null && x.LeaderHero.GetDominantReligion() != site.Religion);
                    foreach (var party in affectedParties)
                    {
                        if (party.IsActive && !party.IsDisbanding && party.MapEvent == null && party.BesiegedSettlement == null && party.CurrentSettlement == null)
                        {
                            if (party.MemberRoster.TotalHealthyCount > party.MemberRoster.TotalManCount * 0.25f)
                            {
                                party.MemberRoster.WoundNumberOfTroopsRandomly((int)Math.Ceiling(party.MemberRoster.TotalHealthyCount * (_model.CalculateCursedRegionDamagePerHour(party) / 100f)));
                            }
                            foreach (var hero in party.GetMemberHeroes())
                            {
                                if (hero.HitPoints > 25 && hero.HitPoints <= hero.MaxHitPoints)
                                {
                                    hero.HitPoints -= _model.CalculateCursedRegionDamagePerHour(party);
                                }
                            }
                        }
                    }
                }
                else site.HourlyTick();
            }
        }

        #region ChaosPortal
        public void AddChaosPortalMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu("chaosportal_menu", "{LOCATION_DESCRIPTION}", ChaosMenuInit);
            starter.AddGameMenuOption("chaosportal_menu", "dobattle", "{BATTLE_OPTION_TEXT}", ChaosBattleCondition, ChaosBattleConsequence);
            starter.AddGameMenuOption("chaosportal_menu", "leave", "Leave...", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
        }

        private void ChaosMenuInit(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as TORBaseSettlementComponent;
            var text = component.IsActive ? GameTexts.FindText("customsettlement_intro", settlement.StringId) : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
            MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
            args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        }

        private bool ChaosBattleCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as TORBaseSettlementComponent;
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
            var text = GameTexts.FindText("customsettlement_battle", settlement.StringId);
            MBTextManager.SetTextVariable("BATTLE_OPTION_TEXT", text);
            if (Hero.MainHero.IsWounded)
            {
                args.Tooltip = new TextObject("{=UL8za0AO}You are wounded.", null);
                args.IsEnabled = false;
            }
            return component.IsActive;
        }

        private void ChaosBattleConsequence(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as ChaosPortalComponent;
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_patrol");
            Clan chaosClan = Clan.FindFirst(x => x.StringId == "chaos_clan_1");
            var party = RaidingPartyComponent.CreateRaidingParty(settlement.StringId + "_defender_party", settlement, "Portal Defenders", template, chaosClan, 250);
            PlayerEncounter.RestartPlayerEncounter(party.Party, PartyBase.MainParty, false);
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            component.IsBattleUnderway = true;
            CampaignMission.OpenBattleMission(component.BattleSceneName);
        }

        private void OnMissionEnded(IMission obj)
        {
            var battleSettlement = Settlement.FindFirst(delegate (Settlement settlement)
            {
                if(settlement.SettlementComponent is ChaosPortalComponent)
                {
                    var comp = settlement.SettlementComponent as ChaosPortalComponent;
                    return comp.IsBattleUnderway;
                }
                return false;
            });
            if(battleSettlement != null)
            {
                var comp = battleSettlement.SettlementComponent as ChaosPortalComponent;
                comp.IsBattleUnderway = false;
                var mission = obj as Mission;
                if (mission.MissionResult != null && mission.MissionResult.BattleResolved && mission.MissionResult.PlayerVictory)
                {
                    comp.IsActive = false;
                    var list = new List<InquiryElement>();
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(comp.RewardItemId);
                    list.Add(new InquiryElement(item, item.Name.ToString(), new ImageIdentifier(item)));
                    var inq = new MultiSelectionInquiryData("Victory!", "You are Victorious! Claim your reward!", list, false, 1, "OK", null, onRewardClaimed, null);
                    MBInformationManager.ShowMultiSelectionInquiry(inq);
                }
                else
                {
                    var inq = new InquiryData("Defeated!", "The enemy proved more than a match for you. Better luck next time!", true, false, "OK", null, null, null);
                    InformationManager.ShowInquiry(inq);
                }
            }
        }

        private void onRewardClaimed(List<InquiryElement> obj)
        {
            var item = obj[0].Identifier as ItemObject;
            Hero.MainHero.PartyBelongedTo.Party.ItemRoster.AddToCounts(item, 1);
        }
        #endregion

        #region Shrine
        public void AddShrineMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu("shrine_menu", "{LOCATION_DESCRIPTION}", ShrineMenuInit);
            starter.AddGameMenuOption("shrine_menu", "pray", "{PRAY_TEXT}", PrayCondition, (args) => GameMenu.SwitchToMenu("shrine_menu_praying"));
            starter.AddGameMenuOption("shrine_menu", "donate", "{=!}Give items as an offering", DonationCondition, (args) => InventoryManager.OpenScreenAsInventory());
            starter.AddGameMenuOption("shrine_menu", "leave", "Leave...", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
            starter.AddWaitGameMenu("shrine_menu_praying", "Praying...",
                delegate (MenuCallbackArgs args)
                {
                    _startWaitTime = CampaignTime.Now;
                    _numberOfTroops = 0;
                    PlayerEncounter.Current.IsPlayerWaiting = true;
                    args.MenuContext.GameMenu.StartWait();
                }, null, PrayConsequence,
                PrayingTick,
                GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
            starter.AddGameMenu("shrine_menu_pray_result", "{PRAY_RESULT} {NEWLINE} {FOLLOWERS_RESULT}", PrayResultInit);
            starter.AddGameMenuOption("shrine_menu_pray_result", "return_to_root", "Continue", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return true;
            }, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("shrine_menu"), true);
        }

        private void ShrineMenuInit(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as ShrineComponent;
            var text = component.IsActive ? GameTexts.FindText("customsettlement_intro", settlement.StringId) : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
            if (component.Religion != null)
            {
                MBTextManager.SetTextVariable("RELIGION_LINK", component.Religion.EncyclopediaLinkWithName);
            }
            MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
            args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        }

        private bool PrayCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as ShrineComponent;
            args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
            var godName = GameTexts.FindText("tor_religion_name_of_god", component.Religion.StringId);
            MBTextManager.SetTextVariable("PRAY_TEXT", "Pray to recieve the blessing of " + godName);
            if (MobileParty.MainParty.HasAnyActiveBlessing())
            {
                args.Tooltip = new TextObject("{=!}You already have an active blessing.", null);
                args.IsEnabled = false;
            }
            return component.IsActive && component.Religion != null && !component.Religion.HostileReligions.Contains(Hero.MainHero.GetDominantReligion());
        }

        private bool DonationCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as ShrineComponent;
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            if (!Hero.MainHero.GetPerkValue(TORPerks.Faith.Offering))
            {
                args.Tooltip = new TextObject("{=!}You need the Offering perk in the Faith skill line to perform this action.", null);
                args.IsEnabled = false;
            }
            return component.IsActive && component.Religion != null && !component.Religion.HostileReligions.Contains(Hero.MainHero.GetDominantReligion());
        }

        private void PrayConsequence(MenuCallbackArgs args)
        {
            PlayerEncounter.Current.IsPlayerWaiting = false;
            args.MenuContext.GameMenu.EndWait();
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
            GameMenu.SwitchToMenu("shrine_menu_pray_result");
        }

        private void PrayingTick(MenuCallbackArgs args, CampaignTime dt)
        {
            float progress = args.MenuContext.GameMenu.Progress;
            int diff = (int)_startWaitTime.ElapsedHoursUntilNow;
            if (diff > 0)
            {
                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
                if (args.MenuContext.GameMenu.Progress != progress)
                {
                    var settlement = Settlement.CurrentSettlement;
                    var component = settlement.SettlementComponent as ShrineComponent;
                    var heroReligion = Hero.MainHero.GetDominantReligion();
                    if(heroReligion == component.Religion)
                    {
                        var devotion = Hero.MainHero.GetDevotionLevelForReligion(heroReligion);
                        if((int)devotion >= (int)DevotionLevel.Devoted)
                        {
                            var troop = component.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
                            var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
                            var count = MBRandom.RandomInt(1, 4); //TODO adjust here for devotion effects
                            if(freeSlots > 0)
                            {
                                if(freeSlots < count) count = freeSlots;
                                MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
                                CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero, settlement, null, troop, count);
                                _numberOfTroops += count;
                            }
                        }
                    }
                }
            }
        }

        private void PrayResultInit(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as ShrineComponent;
            var godName = GameTexts.FindText("tor_religion_name_of_god", component.Religion.StringId);
            var troop = component.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
            MBTextManager.SetTextVariable("PRAY_RESULT", "You recieve the blessing of " + godName + ".");
            if(_numberOfTroops > 0)
            {
                MBTextManager.SetTextVariable("FOLLOWERS_RESULT", "Witnessing your prayers have inspired " + _numberOfTroops.ToString() + " " + troop.EncyclopediaLinkWithName + " to join your party.");
            }
            MobileParty.MainParty.AddBlessingToParty(component.Religion.StringId, _model.CalculateBlessingDurationForParty(MobileParty.MainParty));
            Hero.MainHero.AddReligiousInfluence(component.Religion, _model.CalculateDevotionIncreaseForPraying(Hero.MainHero));
            Hero.MainHero.AddSkillXp(TORSkills.Faith, _model.CalculateSkillXpForPraying(Hero.MainHero));
        }
        #endregion

        #region CursedSite

        private void AddCursedSiteMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu("cursedsite_menu", "{LOCATION_DESCRIPTION}", CursedSiteMenuInit);
            starter.AddGameMenuOption("cursedsite_menu", "purify", "{PURIFY_TEXT}", PurifyCondition, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu_purifying"));
            starter.AddGameMenuOption("cursedsite_menu", "ghosts", "Tap into the congealed essence of Dark Magic and bind some wraiths to your will.", GhostsCondition, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu_ghosts"));
            starter.AddGameMenuOption("cursedsite_menu", "leave", "Leave...", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
            starter.AddWaitGameMenu("cursedsite_menu_purifying", "Performing purification ritual...",
                delegate (MenuCallbackArgs args)
                {
                    _startWaitTime = CampaignTime.Now;
                    _numberOfTroops = 0;
                    PlayerEncounter.Current.IsPlayerWaiting = true;
                    args.MenuContext.GameMenu.StartWait();
                }, null, PurificationConsequence,
                PurifyingTick,
                GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
            starter.AddWaitGameMenu("cursedsite_menu_ghosts", "Performing binding ritual...",
                delegate (MenuCallbackArgs args)
                {
                    _startWaitTime = CampaignTime.Now;
                    _numberOfTroops = 0;
                    PlayerEncounter.Current.IsPlayerWaiting = true;
                    args.MenuContext.GameMenu.StartWait();
                }, null, GhostConsequence,
                BindingTick,
                GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
            starter.AddGameMenu("purification_result", "{PURIFICATION_RESULT} {NEWLINE} {WOUNDED_RESULT}", PurificationResultInit);
            starter.AddGameMenu("ghost_result", "{GHOST_RESULT}", GhostResultInit);
            starter.AddGameMenuOption("purification_result", "return_to_root", "Continue", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return true;
            }, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu"), true);
            starter.AddGameMenuOption("ghost_result", "return_to_root", "Continue", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return true;
            }, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu"), true);
        }

        private void CursedSiteMenuInit(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as CursedSiteComponent;
            var text = GameTexts.FindText("customsettlement_intro", settlement.StringId);
            if (component.IsActive)
            {
                MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
            }
            else
            {
                MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", new TextObject(text.ToString() + "{NEWLINE}" + " " + "{NEWLINE}" + "Currently there are wards in place holding back the malevolent energies of the curse. The wards will hold for " + component.WardHours + " hours more."));
            }
            args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        }

        private bool PurifyCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as CursedSiteComponent;
            var religion = Hero.MainHero.GetDominantReligion();
            if (component != null && religion != null && component.Religion.HostileReligions.Contains(religion) && religion.Affinity == ReligionAffinity.Order)
            {
                var godName = GameTexts.FindText("tor_religion_name_of_god", religion.StringId);
                args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
                MBTextManager.SetTextVariable("PURIFY_TEXT", "Perform a ritual of warding in the name of " + godName + ".");
                if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 10)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("You need at least 10 healthy party members to perform the ritual.");
                }
                return component.IsActive;
            }
            return false;
        }

        private bool GhostsCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as CursedSiteComponent;
            args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;
            if (!(Hero.MainHero.IsNecromancer() || Hero.MainHero.IsVampire()))
            {
                args.Tooltip = new TextObject("You are not a practicioner of necromancy.");
                args.IsEnabled = false;
            }
            if (_lastGhostRecruitmentTime.ContainsKey(Hero.MainHero.StringId) && _lastGhostRecruitmentTime[Hero.MainHero.StringId] >= (int)CampaignTime.Now.ToDays)
            {
                args.Tooltip = new TextObject("You can only perform this action once a day.");
                args.IsEnabled = false;
            }
            return Hero.MainHero.Culture.StringId == "khuzait" && component.IsActive;
        }

        private void PurificationConsequence(MenuCallbackArgs args)
        {
            PlayerEncounter.Current.IsPlayerWaiting = false;
            args.MenuContext.GameMenu.EndWait();
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
            GameMenu.SwitchToMenu("purification_result");
        }

        private void GhostConsequence(MenuCallbackArgs args)
        {
            PlayerEncounter.Current.IsPlayerWaiting = false;
            args.MenuContext.GameMenu.EndWait();
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
            GameMenu.SwitchToMenu("ghost_result");
        }

        private void PurifyingTick(MenuCallbackArgs args, CampaignTime dt)
        {
            float progress = args.MenuContext.GameMenu.Progress;
            int diff = (int)_startWaitTime.ElapsedHoursUntilNow;
            if (diff > 0)
            {
                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
                if (args.MenuContext.GameMenu.Progress != progress)
                {
                    if (MobileParty.MainParty.MemberRoster.TotalHealthyCount > MobileParty.MainParty.MemberRoster.TotalManCount * 0.25f)
                    {
                        MobileParty.MainParty.MemberRoster.WoundNumberOfTroopsRandomly((int)Math.Ceiling(MobileParty.MainParty.MemberRoster.TotalHealthyCount * 0.05f));
                    }
                    _numberOfTroops += (int)Math.Ceiling(MobileParty.MainParty.MemberRoster.TotalHealthyCount * 0.05f);
                }
            }
        }

        private void BindingTick(MenuCallbackArgs args, CampaignTime dt)
        {
            float progress = args.MenuContext.GameMenu.Progress;
            int diff = (int)_startWaitTime.ElapsedHoursUntilNow;
            if (diff > 0)
            {
                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
                if (args.MenuContext.GameMenu.Progress != progress)
                {
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_spirit_host");
                    var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
                    int raisePower = Math.Max(1, (int)Hero.MainHero.GetExtendedInfo().SpellCastingLevel);
                    var count = MBRandom.RandomInt(1, 3);
                    count *= raisePower;
                    if (freeSlots > 0)
                    {
                        if (freeSlots < count) count = freeSlots;
                        MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
                        CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero, Settlement.CurrentSettlement, null, troop, count);
                        _numberOfTroops += count;
                    }
                }
            }
        }

        private void PurificationResultInit(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as CursedSiteComponent;
            int duration = TORConstants.DEFAULT_WARDING_DURATION; //TODO modify this based on faith perks/skills?
            MBTextManager.SetTextVariable("PURIFICATION_RESULT", "Your party succeeds in placing seals and wards around the area dampening the effects of the curse. You estimate the wards will hold for " + duration + " hours.");
            if (_numberOfTroops > 0)
            {
                MBTextManager.SetTextVariable("WOUNDED_RESULT", "Staying in the cursed area while performing the ritual has taken a toll on your men. " + _numberOfTroops + " of your party members have become wounded.");
            }
            Hero.MainHero.AddReligiousInfluence(Hero.MainHero.GetDominantReligion(), TORConstants.DEFAULT_WARDING_DEVOTION_INCREASE);
            Hero.MainHero.AddSkillXp(TORSkills.Faith, 300);
            component.IsActive = false;
            component.WardHours = duration;
        }

        private void GhostResultInit(MenuCallbackArgs args)
        {
            if (_numberOfTroops > 0)
            {
                MBTextManager.SetTextVariable("GHOST_RESULT", "You successfully bind " + _numberOfTroops + " spirits to your command.");
                if (_lastGhostRecruitmentTime.ContainsKey(Hero.MainHero.StringId))
                {
                    _lastGhostRecruitmentTime[Hero.MainHero.StringId] = (int)CampaignTime.Now.ToDays;
                }
                else
                {
                    _lastGhostRecruitmentTime.Add(Hero.MainHero.StringId, (int)CampaignTime.Now.ToDays);
                }
            }
        }

        #endregion

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_customSettlementActiveStates", ref _customSettlementActiveStates);
            dataStore.SyncData("_cursedSiteWardDurationLeft", ref _cursedSiteWardDurationLeft);
            dataStore.SyncData("_lastGhostRecruitmentTime", ref _lastGhostRecruitmentTime);
        }

        private bool CanPartyGoToShrine(MobileParty party)
        {
            return party.IsLordParty &&
                !party.IsEngaging &&
                party.IsActive &&
                !party.IsDisbanding &&
                !party.IsCurrentlyUsedByAQuest &&
                party.CurrentSettlement == null &&
                party.MapEvent == null &&
                !party.Ai.IsDisabled &&
                party.Army == null &&
                !party.HasAnyActiveBlessing();
        }

        private bool CanPartyRecruitGhosts(MobileParty party)
        {
            return party.IsLordParty &&
                !party.IsEngaging &&
                party.IsActive &&
                party.Army == null &&
                !party.IsDisbanding &&
                !party.IsCurrentlyUsedByAQuest &&
                party.CurrentSettlement == null &&
                party.MapEvent == null &&
                !party.Ai.IsDisabled &&
                party.LeaderHero != null &&
                (party.LeaderHero.IsNecromancer() || party.LeaderHero.IsVampire()) &&
                (!_lastGhostRecruitmentTime.ContainsKey(party.LeaderHero.StringId) || _lastGhostRecruitmentTime[party.LeaderHero.StringId] < (int)CampaignTime.Now.ToDays);
        }
    }
}
