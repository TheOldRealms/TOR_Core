using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
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
using TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public class TORCustomSettlementCampaignBehavior : CampaignBehaviorBase
    {
        private CampaignTime _startWaitTime = CampaignTime.Now;
        private int _numberOfTroops = 0;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, OnSettlementHourlyTick);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
            CampaignEvents.TickPartialHourlyAiEvent.AddNonSerializedListener(this, OnAiTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameStart);
        }

        private void OnNewGameStart(CampaignGameStarter starter)
        {
            var customSettlements = Settlement.FindAll(x => x.SettlementComponent is TORCustomSettlementComponent);
            foreach (var settlement in customSettlements)
            {
                var comp = settlement.SettlementComponent as TORCustomSettlementComponent;
                comp.SettlementType.IsActive = true;
            }
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party == MobileParty.MainParty) return;
            if(settlement.SettlementComponent is TORCustomSettlementComponent && ((TORCustomSettlementComponent)settlement.SettlementComponent).SettlementType is Shrine)
            {
                var shrine = ((TORCustomSettlementComponent)settlement.SettlementComponent).SettlementType as Shrine;
                party.AddBlessingToParty(shrine.Religion.StringId, TORConstants.DEFAULT_BLESSING_DURATION);
                LeaveSettlementAction.ApplyForParty(party);
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.Ai.RethinkAtNextHourlyTick = true;
            }
        }

        private void OnAiTick(MobileParty party)
        {
            if (CanPartyGoToShrine(party))
            {
                var settlements = TORCommon.FindSettlementsAroundPosition(party.Position2D, 20, x => x.SettlementComponent is TORCustomSettlementComponent && ((TORCustomSettlementComponent)x.SettlementComponent).SettlementType is Shrine);
                if(settlements.Count > 0)
                {
                    var shrine = ((TORCustomSettlementComponent)settlements.First().SettlementComponent).SettlementType as Shrine;
                    if(party.LeaderHero.GetDominantReligion() == shrine.Religion)
                    {
                        party.Ai.SetMoveGoToSettlement(settlements.First());
                        party.Ai.SetDoNotMakeNewDecisions(true);
                    }
                }
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddChaosPortalMenus(starter);
            AddShrineMenus(starter);
            AddCursedSiteMenus(starter);
        }

        private void OnSettlementHourlyTick(Settlement settlement)
        {
            if(settlement.SettlementComponent is TORCustomSettlementComponent)
            {
                var comp = settlement.SettlementComponent as TORCustomSettlementComponent;
                if(comp.SettlementType is CursedSite)
                {
                    var site = comp.SettlementType as CursedSite;
                    if (site.IsActive)
                    {
                        var affectedParties = TORCommon.FindPartiesAroundPosition(settlement.Position2D, 25, x => x.IsLordParty && x.LeaderHero != null && x.LeaderHero.GetDominantReligion() != site.Religion);
                        foreach (var party in affectedParties)
                        {
                            if(party.IsActive && !party.IsDisbanding && party.MapEvent == null && party.BesiegedSettlement == null)
                            {
                                if (party.MemberRoster.TotalHealthyCount > party.MemberRoster.TotalManCount * 0.25f)
                                {
                                    party.MemberRoster.WoundNumberOfTroopsRandomly((int)Math.Ceiling(party.MemberRoster.TotalHealthyCount * (TORConstants.DEFAULT_CURSE_WOUND_STRENGTH / 100f)));
                                }
                                foreach (var hero in party.GetMemberHeroes())
                                {
                                    if (hero.HitPoints > 25 && hero.HitPoints <= hero.MaxHitPoints)
                                    {
                                        hero.HitPoints -= TORConstants.DEFAULT_CURSE_WOUND_STRENGTH;
                                    }
                                }
                            }
                        }
                    }
                }
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
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var text = component.SettlementType.IsActive ? GameTexts.FindText("customsettlement_intro", settlement.StringId) : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
            MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
            args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        }

        private bool ChaosBattleCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
            var text = GameTexts.FindText("customsettlement_battle", settlement.StringId);
            MBTextManager.SetTextVariable("BATTLE_OPTION_TEXT", text);
            if (Hero.MainHero.IsWounded)
            {
                args.Tooltip = new TextObject("{=UL8za0AO}You are wounded.", null);
                args.IsEnabled = false;
            }
            return component.SettlementType.IsActive;
        }

        private void ChaosBattleConsequence(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var portal = component.SettlementType as ChaosPortal;
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_patrol");
            Clan chaosClan = Clan.FindFirst(x => x.StringId == "chaos_clan_1");
            var party = RaidingPartyComponent.CreateRaidingParty(settlement.StringId + "_defender_party", settlement, "Portal Defenders", template, chaosClan, 250);
            PlayerEncounter.RestartPlayerEncounter(party.Party, PartyBase.MainParty, true);
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            portal.IsBattleUnderway = true;
            CampaignMission.OpenBattleMission(portal.BattleSceneName);
        }

        private void OnMissionEnded(IMission obj)
        {
            var battleSettlement = Settlement.FindFirst(delegate (Settlement settlement)
            {
                if(settlement.SettlementComponent is TORCustomSettlementComponent && 
                ((TORCustomSettlementComponent)settlement.SettlementComponent).SettlementType is ChaosPortal)
                {
                    var comp = settlement.SettlementComponent as TORCustomSettlementComponent;
                    var portal = comp.SettlementType as ChaosPortal;
                    return portal.IsBattleUnderway;
                }
                return false;
            });
            if(battleSettlement != null)
            {
                var portal = ((TORCustomSettlementComponent)battleSettlement.SettlementComponent).SettlementType as ChaosPortal;
                portal.IsBattleUnderway = false;
                var mission = obj as Mission;
                if (mission.MissionResult != null && mission.MissionResult.BattleResolved && mission.MissionResult.PlayerVictory)
                {
                    portal.IsActive = false;
                    var list = new List<InquiryElement>();
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(portal.RewardItemId);
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
            starter.AddGameMenuOption("shrine_menu", "pray", "{PRAY_TEXT}", PrayCondition, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("shrine_menu_praying"));
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
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var shrine = component.SettlementType as Shrine;
            var text = component.SettlementType.IsActive ? GameTexts.FindText("customsettlement_intro", settlement.StringId) : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
            if (shrine.Religion != null)
            {
                MBTextManager.SetTextVariable("RELIGION_LINK", shrine.Religion.EncyclopediaLinkWithName);
            }
            MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
            args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        }

        private bool PrayCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var shrine = component.SettlementType as Shrine;
            args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
            var godName = GameTexts.FindText("tor_religion_name_of_god", shrine.Religion.StringId);
            MBTextManager.SetTextVariable("PRAY_TEXT", "Pray to recieve the blessing of " + godName + ".");
            if (MobileParty.MainParty.HasAnyActiveBlessing())
            {
                args.Tooltip = new TextObject("{=!}You already have an active blessing.", null);
                args.IsEnabled = false;
            }
            return shrine.IsActive;
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
                    var component = settlement.SettlementComponent as TORCustomSettlementComponent;
                    var shrine = component.SettlementType as Shrine;
                    var heroReligion = Hero.MainHero.GetDominantReligion();
                    if(heroReligion == shrine.Religion)
                    {
                        var devotion = Hero.MainHero.GetDevotionLevelForReligion(heroReligion);
                        if((int)devotion >= (int)DevotionLevel.Devoted)
                        {
                            var troop = shrine.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
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
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var shrine = component.SettlementType as Shrine;
            MobileParty.MainParty.AddBlessingToParty(shrine.Religion.StringId, TORConstants.DEFAULT_BLESSING_DURATION);
            var godName = GameTexts.FindText("tor_religion_name_of_god", shrine.Religion.StringId);
            var troop = shrine.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
            MBTextManager.SetTextVariable("PRAY_RESULT", "You recieve the blessing of " + godName + ".");
            if(_numberOfTroops > 0)
            {
                MBTextManager.SetTextVariable("FOLLOWERS_RESULT", "Witnessing your prayers have inspired " + _numberOfTroops.ToString() + " " + troop.EncyclopediaLinkWithName + " to join your party.");
            }
            Hero.MainHero.AddReligiousInfluence(shrine.Religion, TORConstants.DEFAULT_PRAYING_DEVOTION_INCREASE);
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
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var site = component.SettlementType as CursedSite;
            var text = GameTexts.FindText("customsettlement_intro", settlement.StringId);
            if (site.IsActive)
            {
                MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
            }
            else
            {
                MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", new TextObject(text.ToString() + "{NEWLINE}" + " " + "{NEWLINE}" + "Currently there are wards in place holding back the malevolent energies of the curse."));
            }
            args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        }

        private bool PurifyCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var site = component.SettlementType as CursedSite;
            var religion = Hero.MainHero.GetDominantReligion();
            if (site != null && religion != null && site.Religion.HostileReligions.Contains(religion) && religion.Affinity == ReligionAffinity.Order)
            {
                var godName = GameTexts.FindText("tor_religion_name_of_god", religion.StringId);
                args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
                MBTextManager.SetTextVariable("PURIFY_TEXT", "Perform a ritual of warding in the name of " + godName + ".");
                if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 10)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("You need at least 10 healthy party members to perform the ritual.");
                }
                return site.IsActive;
            }
            return false;
        }

        private bool GhostsCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var site = component.SettlementType as CursedSite;
            if (!Hero.MainHero.IsNecromancer() || !Hero.MainHero.IsVampire())
            {
                args.Tooltip = new TextObject("You are not a practicioner of necromancy.");
                args.IsEnabled = false;
            }
            return Hero.MainHero.Culture.StringId == "khuzait" && site.IsActive;
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
                    var settlement = Settlement.CurrentSettlement;
                    var component = settlement.SettlementComponent as TORCustomSettlementComponent;
                    var site = component.SettlementType as CursedSite;
                    var heroReligion = Hero.MainHero.GetDominantReligion();
                    if (heroReligion == site.Religion)
                    {
                        var devotion = Hero.MainHero.GetDevotionLevelForReligion(heroReligion);
                        if ((int)devotion >= (int)DevotionLevel.Devoted)
                        {
                            var troop = site.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
                            var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
                            var count = MBRandom.RandomInt(1, 4); //TODO adjust here for devotion effects
                            if (freeSlots > 0)
                            {
                                if (freeSlots < count) count = freeSlots;
                                MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
                                CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero, settlement, null, troop, count);
                                _numberOfTroops += count;
                            }
                        }
                    }
                }
            }
        }

        private void PurificationResultInit(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as TORCustomSettlementComponent;
            var site = component.SettlementType as CursedSite;
            int duration = TORConstants.DEFAULT_WARDING_DURATION; //TODO modify this based on faith perks/skills?
            MBTextManager.SetTextVariable("PURIFICATION_RESULT", "Your party succeeds in placing seals and wards around the area dampening the effects of the curse. You estimate the wards will hold for " + duration + " hours.");
            if (_numberOfTroops > 0)
            {
                MBTextManager.SetTextVariable("WOUNDED_RESULT", "Staying in the cursed area while performing the ritual has taken a toll on your men. " + _numberOfTroops + " of your party members have become wounded.");
            }
            Hero.MainHero.AddReligiousInfluence(Hero.MainHero.GetDominantReligion(), TORConstants.DEFAULT_WARDING_DEVOTION_INCREASE);
            site.IsActive = false;
        }

        private void GhostResultInit(MenuCallbackArgs args)
        {
            
        }

        #endregion

        public override void SyncData(IDataStore dataStore) { }

        private static bool CanPartyGoToShrine(MobileParty party)
        {
            return party.IsLordParty &&
                !party.IsEngaging &&
                party.IsActive &&
                !party.IsDisbanding &&
                !party.IsCurrentlyUsedByAQuest &&
                party.CurrentSettlement == null &&
                party.MapEvent == null &&
                !party.Ai.IsDisabled &&
                !party.HasAnyActiveBlessing();
        }
    }

    public class TORCustomSettlementComponentSaveableTypeDefiner : SaveableTypeDefiner
    {
        public TORCustomSettlementComponentSaveableTypeDefiner() : base(529011) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(TORCustomSettlementComponent), 7070);
            AddClassDefinition(typeof(BaseSettlementType), 7071);
            AddClassDefinition(typeof(CursedSite), 7072);
            AddClassDefinition(typeof(Shrine), 7073);
            AddClassDefinition(typeof(ChaosPortal), 7074);
            AddClassDefinition(typeof(HerdStone), 7075);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<Settlement, Dictionary<CharacterObject, int>>));
        }
    }
}
