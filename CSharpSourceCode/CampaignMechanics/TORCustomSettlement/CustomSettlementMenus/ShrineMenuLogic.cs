using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class ShrineMenuLogic : TORBaseSettlementMenuLogic
{
    private const int DefilingDarkEnergyPerTick = 125;
    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddShrineMenus(campaignGameStarter);
    }

    public void AddShrineMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenu("shrine_menu", "{LOCATION_DESCRIPTION}", ShrineMenuInit);
        starter.AddGameMenuOption("shrine_menu", "pray", "{PRAY_TEXT}", PrayCondition, (args) => GameMenu.SwitchToMenu("shrine_menu_praying"));
        starter.AddGameMenuOption("shrine_menu", "defile", "Defile the Shrine for Dark Energy. Followers of {GOD_NAME} will remember this", DefileCondtion, (args) => GameMenu.SwitchToMenu("shrine_menu_defiling"));
        starter.AddGameMenuOption("shrine_menu", "donate", "{=tor_custom_settlement_shrine_offering_label_str}Give items as an offering", DonationCondition, (args) => InventoryManager.OpenScreenAsInventory());
        starter.AddGameMenuOption("shrine_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
        starter.AddWaitGameMenu("shrine_menu_praying", "Praying...", delegate(MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            numberOfTroopsFromInteraction = 0;
            PlayerEncounter.Current.IsPlayerWaiting = true;
            args.MenuContext.GameMenu.StartWait();
        }, null, PrayConsequence, PrayingTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
        starter.AddGameMenu("shrine_menu_pray_result", "{PRAY_RESULT} {NEWLINE} {FOLLOWERS_RESULT}", PrayResultInit);
        starter.AddGameMenuOption("shrine_menu_pray_result", "return_to_root", "Continue", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;

            if (Hero.MainHero.GetDominantReligion() != null && Hero.MainHero.GetPerkValue(TORPerks.Faith.Miracle) && MBRandom.RandomInt(0, 100) <= TORConstants.MIRACLE_CHANCE)
            {
                var religion = Hero.MainHero.GetDominantReligion();
                if (religion.ReligiousArtifacts.Count > 0) InkStoryManager.OpenStory("Miracle");
            }

            return true;
        }, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("shrine_menu"), true);
        starter.AddWaitGameMenu("shrine_menu_defiling", "Defiling the shrine...", delegate(MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            PlayerEncounter.Current.IsPlayerWaiting = true;
            args.MenuContext.GameMenu.StartWait();
        }, null, DefileConsequence, DefilingTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
        starter.AddGameMenu("shrine_menu_defile_result", "You sucessfully gathered " + DefilingDarkEnergyPerTick * 4 + " Dark Energy {DARKENERGYICON}. Followers of {GOD_NAME} will perceive this as a crime.", DefileResultInit);
        starter.AddGameMenuOption("shrine_menu_defile_result", "return_to_root", "Continue", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;
        }, (MenuCallbackArgs args) => GameMenu.SwitchToMenu("shrine_menu"), true);
    }

    private bool DefileCondtion(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return false;

        args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;

        if (Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.IsNecromancer()) || Hero.MainHero.IsVampire())
        {
            var lastDefileTime = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().LastDefileTime(Hero.MainHero);
            if (lastDefileTime >= (int)CampaignTime.Now.ToDays - 5)
            {
                args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_once_a_day_text_str}You can only perform this action every 20 days.");
                args.IsEnabled = false;
            }

            return component.IsActive;
        }

        return false;
    }

    private void ShrineMenuInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;
        var text = component.IsActive ? GameTexts.FindText("customsettlement_intro", settlement.StringId) : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
        if (component.Religion != null) MBTextManager.SetTextVariable("RELIGION_LINK", component.Religion.EncyclopediaLinkWithName);
        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
    }

    private bool PrayCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return false;
        args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
        var godName = GameTexts.FindText("tor_religion_name_of_god", component.Religion.StringId);
        MBTextManager.SetTextVariable("GOD_NAME", godName);
        MBTextManager.SetTextVariable("PRAY_TEXT", "{=tor_custom_settlement_shrine_pray_text_str}Pray to recieve the blessing of {GOD_NAME}");
        if (MobileParty.MainParty.HasAnyActiveBlessing())
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_blessing_already_active_str}You already have an active blessing.", null);
            args.IsEnabled = false;
        }

        if (CareerHelper.IsPriestCareer() && CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer()) != component.Religion.StringId)
        {
            var careerGod = CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer());
            var god = ReligionObject.All.FirstOrDefault(x => x.StringId == careerGod);
            MBTextManager.SetTextVariable("CAREERGOD_NAME", god.DeityName);
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_blessing_already_active_str}You devoted your live to {CAREERGOD_NAME}. You can't pray here.", null);
            args.IsEnabled = false;
        }

        return component.IsActive && component.Religion != null && !component.Religion.HostileReligions.Contains(Hero.MainHero.GetDominantReligion());
    }

    private bool DonationCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return false;

        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        if (!Hero.MainHero.GetPerkValue(TORPerks.Faith.Offering))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_donation_perk_info_str}You need the Offering perk in the Faith skill line to perform this action.", null);
            args.IsEnabled = false;
        }

        return component.IsActive && component.Religion != null && !component.Religion.HostileReligions.Contains(Hero.MainHero.GetDominantReligion());
    }

    private void DefileConsequence(MenuCallbackArgs args)
    {
        PlayerEncounter.Current.IsPlayerWaiting = false;
        args.MenuContext.GameMenu.EndWait();
        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
        GameMenu.SwitchToMenu("shrine_menu_defile_result");
    }

    private void PrayConsequence(MenuCallbackArgs args)
    {
        PlayerEncounter.Current.IsPlayerWaiting = false;
        args.MenuContext.GameMenu.EndWait();
        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
        GameMenu.SwitchToMenu("shrine_menu_pray_result");
    }

    private void DefilingTick(MenuCallbackArgs args, CampaignTime dt)
    {
        var progress = args.MenuContext.GameMenu.Progress;
        var diff = (int)_startWaitTime.ElapsedHoursUntilNow;
        if (diff > 0)
        {
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
            if (args.MenuContext.GameMenu.Progress != progress) Hero.MainHero.AddCustomResource("DarkEnergy", DefilingDarkEnergyPerTick);
        }
    }

    private void PrayingTick(MenuCallbackArgs args, CampaignTime dt)
    {
        var progress = args.MenuContext.GameMenu.Progress;
        var diff = (int)_startWaitTime.ElapsedHoursUntilNow;
        if (diff > 0)
        {
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
            if (args.MenuContext.GameMenu.Progress != progress)
            {
                var settlement = Settlement.CurrentSettlement;
                if (settlement.SettlementComponent is not ShrineComponent component) return;
                var heroReligion = Hero.MainHero.GetDominantReligion();
                if (heroReligion == component.Religion)
                {
                    var devotion = Hero.MainHero.GetDevotionLevelForReligion(heroReligion);
                    if ((int)devotion >= (int)DevotionLevel.Devoted)
                    {
                        var troop = component.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
                        if (troop == null)
                        {
                            return;
                        }
                        var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
                        var count = MBRandom.RandomInt(1, 4); //TODO adjust here for devotion effects
                        if (freeSlots > 0)
                        {
                            if (freeSlots < count) count = freeSlots;
                            MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
                            CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero, settlement, null, troop, count);
                            numberOfTroopsFromInteraction += count;
                        }
                    }
                }
            }
        }
    }

    private void DefileResultInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;
        var shrineReligion = component.Religion;

        foreach (var hero in Campaign.Current.AliveHeroes)
        {
            var dominantReligion = hero.GetDominantReligion();
            if (dominantReligion == null) continue;
            if (dominantReligion.HostileReligions.Contains(shrineReligion)) continue;
            var relation = hero.GetRelationWithPlayer();

            if (dominantReligion == shrineReligion)
            {
                var devotionLevel = hero.GetDevotionLevelForReligion(shrineReligion);
                switch (devotionLevel)
                {
                    case DevotionLevel.None:
                        continue;
                    case DevotionLevel.Follower:
                    {
                        hero.SetPersonalRelation(Hero.MainHero, (int)relation - 20);
                        continue;
                    }
                    case DevotionLevel.Devoted:
                    {
                        hero.SetPersonalRelation(Hero.MainHero, (int)relation - 30);
                        continue;
                    }
                    case DevotionLevel.Fanatic:
                    {
                        hero.SetPersonalRelation(Hero.MainHero, (int)relation - 100);
                        continue;
                    }
                    default:
                        continue;
                }
            }
            else
            {
                if (shrineReligion.Affinity == dominantReligion.Affinity) hero.SetPersonalRelation(Hero.MainHero, (int)relation - 10);
            }
        }
    }


    public static bool CanPartyGoToShrine(MobileParty party)
    {
        return party.IsLordParty && !party.IsEngaging && party.IsActive && !party.IsDisbanding && !party.IsCurrentlyUsedByAQuest && party.CurrentSettlement == null && party.MapEvent == null && !party.Ai.IsDisabled && party.Army == null && !party.HasAnyActiveBlessing();
    }

    private void PrayResultInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as ShrineComponent;
        var godName = GameTexts.FindText("tor_religion_name_of_god", component.Religion.StringId);
        var troop = component.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
        MBTextManager.SetTextVariable("GOD_NAME", godName);
        MBTextManager.SetTextVariable("PRAY_RESULT", "{=tor_custom_settlement_shrine_pray_result_text_str}You received the blessing of {GOD_NAME}.");
        if (numberOfTroopsFromInteraction > 0)
        {
            MBTextManager.SetTextVariable("FOLLOWER_RESULT_NUMBER", numberOfTroopsFromInteraction.ToString());
            MBTextManager.SetTextVariable("FOLLOWER_RESULT_TROOP", troop.EncyclopediaLinkWithName);
            MBTextManager.SetTextVariable("FOLLOWERS_RESULT", "{=tor_custom_settlement_shrine_follower_result_str}Witnessing your prayers have inspired {FOLLOWER_RESULT_NUMBER} {FOLLOWER_RESULT_TROOP} to join your party.");
        }

        var model = Campaign.Current.Models.GetFaithModel();

        model.AddBlessingToParty(MobileParty.MainParty, component.Religion.StringId);
    }


    public ShrineMenuLogic(CampaignGameStarter campaignGameStarter) : base(campaignGameStarter)
    {
    }
}