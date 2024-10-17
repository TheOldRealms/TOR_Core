using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class CursedSiteMenuLogic(CampaignGameStarter starter) : TORBaseSettlementMenuLogic(starter)
{
    public static int MinimumDaysBetweenRaisingGhosts = 3;
    
    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddCursedSiteMenus(campaignGameStarter);
    }
    
    private const int _empoweringUndeadCost = 100;
    private int _empoweredUndead = 0;
    private Dictionary<string, int> _leveledUpUndead = [];
    
     private void AddCursedSiteMenus(CampaignGameStarter starter)
        {
            MBTextManager.SetTextVariable("DARKENERGYICON", CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
            starter.AddGameMenu("cursedsite_menu", "{LOCATION_DESCRIPTION}", CursedSiteMenuInit);
            starter.AddGameMenuOption("cursedsite_menu", "purify", "{PURIFY_TEXT}", PurifyCondition, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu_purifying"));
            starter.AddGameMenuOption("cursedsite_menu", "ghosts", "{tor_custom_settlement_menu_cursed_site_ghost_str}Tap into the congealed essence of Dark Magic and bind some wraiths to your will.", GhostsCondition, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu_ghosts"));
            starter.AddGameMenuOption("cursedsite_menu", "ghosts", "{tor_custom_settlement_menu_cursed_site_ghost_str}Empower your undead minions using Dark Energy (100{DARKENERGYICON}) .", EmpoweringUndeadCondition, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu_empower_minions"));
            starter.AddGameMenuOption("cursedsite_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
            starter.AddWaitGameMenu("cursedsite_menu_purifying", "{=tor_custom_settlement_cursed_site_purify_progress_str}Performing purification ritual...",
                delegate (MenuCallbackArgs args)
                {
                    _startWaitTime = CampaignTime.Now;
                    numberOfTroopsFromInteraction = 0;
                    PlayerEncounter.Current.IsPlayerWaiting = true;
                    args.MenuContext.GameMenu.StartWait();
                }, null, PurificationConsequence,
                PurifyingTick,
                GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
            starter.AddWaitGameMenu("cursedsite_menu_ghosts", "{=tor_custom_settlement_cursed_site_ghosts_progress_str}Performing binding ritual...",
                delegate (MenuCallbackArgs args)
                {
                    _startWaitTime = CampaignTime.Now;
                    numberOfTroopsFromInteraction = 0;
                    PlayerEncounter.Current.IsPlayerWaiting = true;
                    args.MenuContext.GameMenu.StartWait();
                }, null, GhostConsequence,
                BindingTick,
                GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
            starter.AddWaitGameMenu("cursedsite_menu_empower_minions", "{=tor_custom_settlement_cursed_site_ghosts_progress_str}Empowering your minions...",
                delegate (MenuCallbackArgs args)
                {
                    _startWaitTime = CampaignTime.Now;
                    PlayerEncounter.Current.IsPlayerWaiting = true;
                    args.MenuContext.GameMenu.StartWait();
                }, null, EmporingConsequence,
                EmpowerUndeadMinionsTick,
                GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
            starter.AddGameMenu("purification_result", "{PURIFICATION_RESULT} {NEWLINE} {WOUNDED_RESULT}", PurificationResultInit);
            starter.AddGameMenu("ghost_result", "{GHOST_RESULT}", GhostResultInit);
            starter.AddGameMenu("empowering_result", "{EMPOWERING_RESULT} \n{EMPOWERING_LIST}", EmporingResultInit);
            starter.AddGameMenuOption("purification_result", "return_to_root", "{tor_custom_settlement_menu_continue_str}Continue", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return true;
            }, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu"), true);
            starter.AddGameMenuOption("ghost_result", "return_to_root", "{tor_custom_settlement_menu_continue_str}Continue", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return true;
            }, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("cursedsite_menu"), true);

            starter.AddGameMenuOption("empowering_result", "return_to_root", "{tor_custom_settlement_menu_continue_str}Continue", delegate (MenuCallbackArgs args)
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
                MBTextManager.SetTextVariable("CURSEDSITE_WARDHOURS", component.WardHours);
                var wardText = new TextObject("{=tor_custom_settlement_cursed_site_ward_text_str}Currently there are wards in place holding back the malevolent energies of the curse. The wards will hold for {CURSEDSITE_WARDHOURS} hours more");
                MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", new TextObject(text.ToString() + "{NEWLINE}" + " " + "{NEWLINE}" + wardText));
            }
            args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
        }

        private bool PurifyCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var religion = Hero.MainHero.GetDominantReligion();
            if (settlement.SettlementComponent is CursedSiteComponent component && religion != null && component.Religion.HostileReligions.Contains(religion) && religion.Affinity == ReligionAffinity.Order)
            {
                var godName = GameTexts.FindText("tor_religion_name_of_god", religion.StringId);
                MBTextManager.SetTextVariable("GOD_NAME", godName);
                args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
                MBTextManager.SetTextVariable("PURIFY_TEXT", "{=tor_custom_settlement_cursed_site_purify_text_str}Perform a ritual of warding in the name of {GOD_NAME}.");
                if (MobileParty.MainParty.MemberRoster.TotalHealthyCount < 10)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_purify_fail_text_str}You need at least 10 healthy party members to perform the ritual.");
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

            if (!(Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.IsNecromancer()) || Hero.MainHero.IsVampire()))
            {
                args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_not_necromancer_text_str}You are not a practitioner of necromancy.");
                args.IsEnabled = false;
            }
            else
            {
                var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;

                if (freeSlots <= 0)
                {
                    args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_not_enough_free_slots_text_str}You have not enough space in your party.");
                    args.IsEnabled = false;
                }
            
                var lastGhostRecruitmentTime = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().LastGhostRecruitmentTime(Hero.MainHero);
                if (lastGhostRecruitmentTime >= (int)CampaignTime.Now.ToDays)
                {
                    args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_once_a_day_text_str}You can only perform this action once a day.");
                    args.IsEnabled = false;
                }
            }
            

            return component.IsActive;
        }

        private bool EmpoweringUndeadCondition(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as CursedSiteComponent;

            if (!(Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.IsNecromancer()) || Hero.MainHero.IsVampire()))
            {
                args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_not_necromancer_text_str}You are not a practitioner of necromancy.");
                args.IsEnabled = false;
            }
            else
            {
                if (!Hero.MainHero.PartyBelongedTo.MemberRoster.ToFlattenedRoster().Any(x => x.Troop.IsUndead()))
                {
                    args.Tooltip = new TextObject("There are no undead in your party.");
                    args.IsEnabled = false;
                }
                if (Hero.MainHero.GetCustomResourceValue("DarkEnergy") < _empoweringUndeadCost)
                {
                    args.Tooltip = new TextObject("You have not enough Dark Energy({DARKENERGYICON}).");
                    args.IsEnabled = false;
                }
            }

            return component.IsActive;
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

        private void EmporingConsequence(MenuCallbackArgs args)
        {
            PlayerEncounter.Current.IsPlayerWaiting = false;
            args.MenuContext.GameMenu.EndWait();
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
            GameMenu.SwitchToMenu("empowering_result");
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
                    numberOfTroopsFromInteraction += (int)Math.Ceiling(MobileParty.MainParty.MemberRoster.TotalHealthyCount * 0.05f);
                }
            }
        }

        private void BindingTick(MenuCallbackArgs args, CampaignTime dt)
        {
            float progress = args.MenuContext.GameMenu.Progress;
            int diff = (int)_startWaitTime.ElapsedHoursUntilNow;
            if (diff > 0)
            {
                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.1f);
                if (args.MenuContext.GameMenu.Progress != progress)
                {
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_spirit_host");
                    var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
                    float raisePower = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);

                    var chance = raisePower / 200;

                    if (MBRandom.RandomFloat < chance)
                    {
                        var count = 1;
                        if (freeSlots > 0)
                        {
                            if (freeSlots < count) count = freeSlots;
                            MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
                            CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero, Settlement.CurrentSettlement, null, troop, count);
                            numberOfTroopsFromInteraction += count;
                        }
                    }
                    

                }

                foreach (var hero in MobileParty.MainParty.GetMemberHeroes())
                {
                    var nagash = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash");
                    if (nagash != null)
                    {
                        hero.AddReligiousInfluence(nagash, 1, false);
                    }
                }
            }
        }

        private void EmpowerUndeadMinionsTick(MenuCallbackArgs args, CampaignTime dt)
        {
            var xp = 250;
            float progress = args.MenuContext.GameMenu.Progress;
            int diff = (int)_startWaitTime.ElapsedHoursUntilNow;
            if (diff > 0)
            {
                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
                if (args.MenuContext.GameMenu.Progress != progress)
                {
                    var party = Hero.MainHero.PartyBelongedTo;
                    for (int i = 0; i < Hero.MainHero.PartyBelongedTo.MemberRoster.Count; i++)
                    {
                        var troopCharacter = party.MemberRoster.GetCharacterAtIndex(i);
                        if (!troopCharacter.IsUndead()) continue;
                        if (troopCharacter.IsHero) continue;
                        var a = Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetElementCopyAtIndex(i).Xp;

                        var model = Campaign.Current.Models.PartyTroopUpgradeModel;

                        if (model.IsTroopUpgradeable(Hero.MainHero.PartyBelongedTo.Party, troopCharacter))
                        {
                            var xpa = Campaign.Current.Models.PartyTroopUpgradeModel.GetXpCostForUpgrade(Hero.MainHero.PartyBelongedTo.Party, troopCharacter, troopCharacter.UpgradeTargets[0]);
                            a %= xpa;
                            if (a + xp >= xpa && a != 0)
                            {
                                _empoweredUndead++;
                                if (_leveledUpUndead.ContainsKey(troopCharacter.Name.ToString()))
                                {
                                    _leveledUpUndead[troopCharacter.Name.ToString()]++;
                                }
                                else
                                {
                                    _leveledUpUndead.Add(troopCharacter.Name.ToString(), 1);
                                }

                            }
                            Hero.MainHero.PartyBelongedTo.MemberRoster.AddXpToTroop(250, troopCharacter);
                        }
                    }
                }
            }
        }

        private void PurificationResultInit(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as CursedSiteComponent;
            int duration = TORConstants.DEFAULT_WARDING_DURATION; //TODO modify this based on faith perks/skills?
            MBTextManager.SetTextVariable("PURIFICATION_DURATION", duration);
            MBTextManager.SetTextVariable("PURIFICATION_RESULT", "{=tor_custom_settlement_cursed_site_purify_success_str}Your party succeeds in placing seals and wards around the area dampening the effects of the curse. You estimate the wards will hold for {PURIFICATION_DURATION} hours.");
            if (numberOfTroopsFromInteraction > 0)
            {
                MBTextManager.SetTextVariable("PURIFICATION_WOUNDED_RESULT_NUMBER", numberOfTroopsFromInteraction);
                MBTextManager.SetTextVariable("WOUNDED_RESULT", "{=tor_custom_settlement_cursed_site_purify_wounded_result_str}Staying in the cursed area while performing the ritual has taken a toll on your men. {PURIFICATION_WOUNDED_RESULT_NUMBER} of your party members have become wounded.");
            }
            Hero.MainHero.AddReligiousInfluence(Hero.MainHero.GetDominantReligion(), TORConstants.DEFAULT_WARDING_DEVOTION_INCREASE);
            Hero.MainHero.AddSkillXp(TORSkills.Faith, 300);
            component.IsActive = false;
            component.WardHours = duration;
        }

        private void GhostResultInit(MenuCallbackArgs args)
        {
            if (numberOfTroopsFromInteraction > 0)
            {
                MBTextManager.SetTextVariable("GHOST_RESULT_NUMBER", numberOfTroopsFromInteraction);
                MBTextManager.SetTextVariable("GHOST_RESULT", "{=tor_custom_settlement_cursed_site_ghosts_result_str}You successfully bind {GHOST_RESULT_NUMBER} spirits to your command.");
                
               Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().SetLastGhostRecruitmentTime(Hero.MainHero, (int) CampaignTime.Now.ToDays);
                
     
            }
        }

        private void EmporingResultInit(MenuCallbackArgs args)
        {
            if (_empoweredUndead > 0)
            {
                MBTextManager.SetTextVariable("UNDEAD_UPGRADES", _empoweredUndead);
                MBTextManager.SetTextVariable("EMPOWERING_RESULT", "{UNDEAD_UPGRADES} of your minions grew stronger.");
                var result = "";
                foreach (var item in _leveledUpUndead)
                {
                    result += item.Key + " - " + item.Value + "\n";
                }
                MBTextManager.SetTextVariable("EMPOWERING_LIST", result);
                _empoweredUndead = 0;
                _leveledUpUndead.Clear();
            }
            Hero.MainHero.AddCustomResource("DarkEnergy", -100);
        }
        
        public static bool CanPartyRecruitGhosts(MobileParty party)
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
                   (party.LeaderHero.IsNecromancer() || 
                    party.LeaderHero.IsVampire()) && 
                   party.PartySizeRatio < 0.8f;
        }

       
}