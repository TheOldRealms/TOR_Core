using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class ShrineMenuLogic : TORBaseSettlementMenuLogic
{
    private const int DefilingCooldownInDays = 5;
    private const int DefilingDarkEnergyPerTick = 125;
    private const int PrayerTroopRewardCooldownInDays = 5;
    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddShrineMenus(campaignGameStarter);
    }

    public void AddShrineMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenu("shrine_menu", "{LOCATION_DESCRIPTION}", ShrineMenuInit);
        starter.AddGameMenuOption("shrine_menu", "pray", "{PRAY_TEXT}", PrayCondition, (args) => GameMenu.SwitchToMenu("shrine_menu_praying"));
        starter.AddGameMenuOption("shrine_menu", "defile", TORTextHelper.GetText("tor_shrine_defile_option", "Defile the Shrine for Dark Energy. Followers of {GOD_NAME} will remember this."), DefileCondtion, (args) => GameMenu.SwitchToMenu("shrine_menu_defiling"));
        starter.AddGameMenuOption("shrine_menu", "loot", TORTextHelper.GetText("tor_shrine_loot_option", "Loot the Shrine for resources. Followers of {GOD_NAME} will remember this."), LootCondition, (args) => GameMenu.SwitchToMenu("shrine_menu_looting"));
        starter.AddGameMenuOption("shrine_menu", "donate", TORTextHelper.GetText("tor_custom_settlement_shrine_offering_label", "Give items as an offering"), DonationCondition, (args) => InventoryScreenHelper.OpenScreenAsInventory());//the xp calculation is performed in ReligionCampaignBehavior.OnItemsDiscarded
        starter.AddGameMenuOption("shrine_menu", "leave", TORTextHelper.GetText("tor_custom_settlement_menu_leave", "Leave..."), delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
        starter.AddWaitGameMenu("shrine_menu_praying", TORTextHelper.GetText("tor_shrine_praying_progress", "Praying..."), delegate (MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            numberOfTroopsFromInteraction = 0;
            PlayerEncounter.Current.IsPlayerWaiting = true;
            args.MenuContext.GameMenu.StartWait();
        }, null, PrayConsequence, PrayingTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, TORConstants.SHRINE_PRAYING_DURATION, GameMenu.MenuFlags.None, null);
        starter.AddGameMenu("shrine_menu_pray_result", "{PRAY_RESULT} {NEWLINE} {FOLLOWERS_RESULT}", PrayResultInit);
        starter.AddGameMenuOption("shrine_menu_pray_result", "return_to_root", TORTextHelper.GetText("tor_shrine_continue", "Continue"), args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;

        }, args =>
        {
            var model = Campaign.Current.Models.GetFaithModel();
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as ShrineComponent;
            model.AddBlessingToParty(MobileParty.MainParty, component.Religion.StringId);

            // Track the last time the player prayed at a shrine
            var behavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
            behavior.SetLastPrayerTime(Hero.MainHero, (int)CampaignTime.Now.ToDays);

            // Fire shrine prayer event for any systems that need to track it
            TORCampaignEvents.Instance.OnShrinePrayer(Hero.MainHero, component.Religion, settlement);

            if (Hero.MainHero.GetDominantReligion() != null && Hero.MainHero.GetPerkValue(TORPerks.Faith.Miracle) && Hero.MainHero.GetDominantReligion() == component.Religion) //&& MBRandom.RandomInt(0, 100) <= TORConstants.MIRACLE_CHANCE) Sly : 100% chance to trigger if someone reaches high enough faith - will need to be better controlled to not trigger constantly when praying at a shrine. Should be considered if someone can receive multiple artifacts, if they can receive an extra copy if they lose it, etc... These items can probably be set to not be lost on becoming prisoner so the player will never lose it unless they discard it which would allow us to have it only trigger once per god/campaign.
            {
                var religion = Hero.MainHero.GetDominantReligion();
                if (religion.ReligiousArtifacts.Count > 0) InkStoryManager.OpenStory("Miracle");
            }
            GameMenu.SwitchToMenu("shrine_menu");

        }, true);
        starter.AddWaitGameMenu("shrine_menu_defiling", TORTextHelper.GetText("tor_shrine_defiling_progress", "Defiling the shrine..."), delegate (MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            PlayerEncounter.Current.IsPlayerWaiting = true;
            args.MenuContext.GameMenu.StartWait();
        }, null, DefileConsequence, DefilingTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
        starter.AddGameMenu("shrine_menu_defile_result", TORTextHelper.GetText("tor_shrine_defile_result", "You successfully gathered {DEFILE_AMOUNT} Dark Energy {DARKENERGYICON}. Followers of {GOD_NAME} will perceive this as a crime."), DefileResultInit);
        starter.AddGameMenuOption("shrine_menu_defile_result", "return_to_root", TORTextHelper.GetText("tor_shrine_continue", "Continue"), args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;
        }, args =>
        {
            DefileResultConsequence();
            GameMenu.SwitchToMenu("shrine_menu");
        }, true);
        starter.AddWaitGameMenu("shrine_menu_looting", TORTextHelper.GetText("tor_shrine_looting_progress", "Looting the shrine..."), delegate (MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            PlayerEncounter.Current.IsPlayerWaiting = true;
            args.MenuContext.GameMenu.StartWait();
        }, null, LootConsequence, LootingTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
        starter.AddGameMenu("shrine_menu_loot_result", TORTextHelper.GetText("tor_shrine_loot_result", "{LOOT_RESULT}. Followers of {GOD_NAME} will perceive this as a crime."), null);
        starter.AddGameMenuOption("shrine_menu_loot_result", "return_to_root", TORTextHelper.GetText("tor_shrine_continue", "Continue"), args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;
        }, args =>
        {
            LootResultConsequence();
            GameMenu.SwitchToMenu("shrine_menu");
        }, true);
    }

    private bool DefileCondtion(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return false;

        args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;

        // Vampires, Necromancers, and Black Grail Knights can defile
        if (Hero.MainHero.IsVampire() ||
            Hero.MainHero.IsNecromancer() ||
            Hero.MainHero.HasCareer(TORCareers.BlackGrailKnight))
        {
            var lastDefileTime = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().LastDefileTime(Hero.MainHero);
            if (lastDefileTime >= (int)CampaignTime.Now.ToDays - DefilingCooldownInDays)
            {
                GameTexts.SetVariable("DEFILE_COOLDOWN_DAYS", DefilingCooldownInDays.ToString());
                args.Tooltip = TORTextHelper.GetTextObject("tor_shrine_defiling_cooldown", "You can only perform this action every {DEFILE_COOLDOWN_DAYS} days.");
                args.IsEnabled = false;
            }

            return component.IsActive;
        }

        // Mousillon and Sylvania cultures can defile if they have a vampire or necromancer companion
        if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.MOUSILLON ||
            Hero.MainHero.Culture.StringId == TORConstants.Cultures.SYLVANIA)
        {
            if (Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.IsVampire() || x.IsNecromancer()))
            {
                var lastDefileTime = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().LastDefileTime(Hero.MainHero);
                if (lastDefileTime >= (int)CampaignTime.Now.ToDays - DefilingCooldownInDays)
                {
                    GameTexts.SetVariable("DEFILE_COOLDOWN_DAYS", DefilingCooldownInDays);
                    args.Tooltip = TORTextHelper.GetTextObject("tor_shrine_defiling_cooldown", "You can only perform this action every {DEFILE_COOLDOWN_DAYS} days.");
                    args.IsEnabled = false;
                }

                return component.IsActive;
            }
        }

        return false;
    }

    private bool LootCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return false;
        
        args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;

        if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
        {
            // Greenskins can loot any non-Greenskin shrine
            if (component.Religion.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_shrine_greenskin_own", "You can't loot your own shrine, ya git!");
                args.IsEnabled = false;
                return true;
            }

            var lastDefileTime = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().LastDefileTime(Hero.MainHero);
            if (lastDefileTime >= (int)CampaignTime.Now.ToDays - DefilingCooldownInDays)
            {
                GameTexts.SetVariable("DEFILE_COOLDOWN_DAYS", DefilingCooldownInDays.ToString());
                args.Tooltip = TORTextHelper.GetTextObject("tor_shrine_defiling_cooldown", "You can only perform this action every {DEFILE_COOLDOWN_DAYS} days.");
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
        var text = component.IsActive ? TORTextHelper.GetTextObject("tor_customsettlement_intro", settlement.StringId, "A shrine.", skipValidation: true) : TORTextHelper.GetTextObject("tor_customsettlement_disabled", settlement.StringId, "This shrine is currently inactive.", skipValidation: true);
        if (component.Religion != null) MBTextManager.SetTextVariable("RELIGION_LINK", component.Religion.EncyclopediaLinkWithName);
        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        
        var godName = TORTextHelper.GetTextObject("tor_religion_name_of_god", component.Religion.StringId, "the god", skipValidation: true);
        GameTexts.SetVariable("GOD_NAME", godName);//attempting setting name on initialization so the variable is available for all the options
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
    }

    private bool PrayCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return false;
        args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
        var godName = TORTextHelper.GetTextObject("tor_religion_name_of_god", component.Religion.StringId, "the god", skipValidation: true);
        MBTextManager.SetTextVariable("GOD_NAME", godName);
        MBTextManager.SetTextVariable("PRAY_TEXT", TORTextHelper.GetTextObject("tor_custom_settlement_shrine_pray_text", "Pray to receive the blessing of {GOD_NAME}"));

        // Vampires, Necromancers, and Black Grail Knights cannot pray
        if (Hero.MainHero.IsVampire() ||
            Hero.MainHero.IsNecromancer() ||
            Hero.MainHero.HasCareer(TORCareers.BlackGrailKnight))
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_shrine_undead_no_pray", "The undead do not commune with the gods.");
            args.IsEnabled = false;
            return true;
        }

        // Culture restriction: can only pray at shrines of your own culture
        if (!IsCultureCompatibleWithShrine(Hero.MainHero, component.Religion))
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_shrine_wrong_culture", "This shrine is not meant for your kind.");
            args.IsEnabled = false;
            return true;
        }

        if (CareerHelper.IsPriestCareer(Hero.MainHero.GetCareer()) && CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer()) != component.Religion.StringId)
        {
            var careerGod = CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer());
            var god = ReligionObject.All.FirstOrDefault(x => x.StringId == careerGod);
            MBTextManager.SetTextVariable("CAREERGOD_NAME", god.DeityName);
            args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_shrine_blessing_already_active", "You devoted your live to {CAREERGOD_NAME}. You can't pray here.");
            args.IsEnabled = false;
        }

        return component.IsActive && component.Religion != null;
    }

    private bool DonationCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return false;

        args.optionLeaveType = GameMenuOption.LeaveType.Trade;

        // Vampires, Necromancers, and Black Grail Knights cannot donate
        if (Hero.MainHero.IsVampire() ||
            Hero.MainHero.IsNecromancer() ||
            Hero.MainHero.HasCareer(TORCareers.BlackGrailKnight))
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_shrine_vampire_no_donate", "The undead do not make offerings to the gods.");
            args.IsEnabled = false;
            return true;
        }

        // Undead, Chaos, and Greenskin pantheon cannot donate
        var playerReligion = Hero.MainHero.GetDominantReligion();
        if (playerReligion != null && (playerReligion.Pantheon == Pantheon.Undead ||
            playerReligion.Pantheon == Pantheon.Chaos || playerReligion.Pantheon == Pantheon.Greenskin))
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_shrine_no_donate_affinity", "You do not honor the gods through offerings.");
            args.IsEnabled = false;
            return true;
        }

        // Must have compatible culture to donate (uses same logic as praying)
        if (!IsCultureCompatibleWithShrine(Hero.MainHero, component.Religion))
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_shrine_wrong_culture_donate", "You cannot make offerings at a shrine not meant for your kind.");
            args.IsEnabled = false;
            return true;
        }

        // Priest careers can only donate at shrines of their deity
        if (CareerHelper.IsPriestCareer(Hero.MainHero.GetCareer()) && CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer()) != component.Religion.StringId)
        {
            var careerGod = CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer());
            var god = ReligionObject.All.FirstOrDefault(x => x.StringId == careerGod);
            MBTextManager.SetTextVariable("CAREERGOD_NAME", god.DeityName);
            args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_shrine_blessing_already_active", "You devoted your live to {CAREERGOD_NAME}. You can't donate here.");
            args.IsEnabled = false;
            return true;
        }

        if (!Hero.MainHero.GetPerkValue(TORPerks.Faith.Offering))
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_custom_settlement_donation_perk_info", "You need the Offering perk in the Faith skill line to perform this action.");
            args.IsEnabled = false;
        }

        return component.IsActive && component.Religion != null;
    }

    private void DefileResultInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;
        var godName = TORTextHelper.GetTextObject("tor_religion_name_of_god", component.Religion.StringId, "the god", skipValidation: true);
        MBTextManager.SetTextVariable("GOD_NAME", godName);
        MBTextManager.SetTextVariable("DEFILE_AMOUNT", DefilingDarkEnergyPerTick * 4);
    }

    private void DefileConsequence(MenuCallbackArgs args)
    {
        PlayerEncounter.Current.IsPlayerWaiting = false;
        args.MenuContext.GameMenu.EndWait();
        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
        GameMenu.SwitchToMenu("shrine_menu_defile_result");
    }

    private void LootConsequence(MenuCallbackArgs args)
    {
        PlayerEncounter.Current.IsPlayerWaiting = false;
        args.MenuContext.GameMenu.EndWait();
        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);

        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;
        var godName = TORTextHelper.GetTextObject("tor_religion_name_of_god", component.Religion.StringId, "the god", skipValidation: true);
        MBTextManager.SetTextVariable("GOD_NAME", godName);

        // Fire shrine looted event
        TORCampaignEvents.Instance.OnShrineLooted(Hero.MainHero, component.Religion, settlement);

        GameMenu.SwitchToMenu("shrine_menu_loot_result");
    }

    private void PrayConsequence(MenuCallbackArgs args)
    {
        PlayerEncounter.Current.IsPlayerWaiting = false;
        args.MenuContext.GameMenu.EndWait();
        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
        //cd for shrine troops
        if (numberOfTroopsFromInteraction > 0)
        {
            Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>()
                .SetLastPrayerTroopRewardTime(Hero.MainHero, (int)CampaignTime.Now.ToDays);
        }
        GameMenu.SwitchToMenu("shrine_menu_pray_result");
    }

    private void DefilingTick(MenuCallbackArgs args, CampaignTime dt)
    {
        var progress = args.MenuContext.GameMenu.Progress;
        var diff = (int)_startWaitTime.ElapsedHoursUntilNow;
        if (diff > 0)
        {
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
            if (args.MenuContext.GameMenu.Progress != progress)
            {
                var darkEnergyGain = DefilingDarkEnergyPerTick;

                // UnhallowedSoulPassive2: More Dark Energy from defiling shrines
                if (Hero.MainHero.HasCareerChoice("UnhallowedSoulPassive2"))
                {
                    darkEnergyGain = (int)(darkEnergyGain * 1.5f); // 50% more Dark Energy
                }

                Hero.MainHero.AddCustomResource("DarkEnergy", darkEnergyGain);
            }
        }
    }

    private void LootingTick(MenuCallbackArgs args, CampaignTime dt)
    {
        var progress = args.MenuContext.GameMenu.Progress;
        var diff = (int)_startWaitTime.ElapsedHoursUntilNow;
        if (diff > 0)
        {
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * 0.25f);
            if (args.MenuContext.GameMenu.Progress != progress)
            {
                Hero.MainHero.ChangeHeroGold(100);
                MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Meat, 10);
                Hero.MainHero.AddCustomResource("Teef", 25);

                if (args.MenuContext.GameMenu.Progress >= 1f)
                {
                    var totalGold = 400;
                    var totalFood = 40;
                    var totalTeef = 100;
                    var lootResultText = TORTextHelper.GetTextObject("tor_shrine_loot_result_detail", "You successfully looted {TOTAL_GOLD} gold, {TOTAL_FOOD} meat, and {TOTAL_TEEF} Teef");
                    lootResultText.SetTextVariable("TOTAL_GOLD", totalGold);
                    lootResultText.SetTextVariable("TOTAL_FOOD", totalFood);
                    lootResultText.SetTextVariable("TOTAL_TEEF", totalTeef);
                    MBTextManager.SetTextVariable("LOOT_RESULT", lootResultText);
                }
            }
        }
    }

    private void PrayingTick(MenuCallbackArgs args, CampaignTime dt)
    {
        const float ProgressPerHour = 0.25f;

        var previousProgress = args.MenuContext.GameMenu.Progress;
        var diff = (int)_startWaitTime.ElapsedHoursUntilNow;
        if (diff <= 0) return;

        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff * ProgressPerHour);

        var currentProgress = args.MenuContext.GameMenu.Progress;
        if (currentProgress == previousProgress) return;

        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;

        var heroReligion = Hero.MainHero.GetDominantReligion();
        if (heroReligion != component.Religion) return;

        var behavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
        var lastRewardDay = behavior.LastPrayerTroopRewardTime(Hero.MainHero);
        if (lastRewardDay >= 0 && (int)CampaignTime.Now.ToDays - lastRewardDay < PrayerTroopRewardCooldownInDays)
        {
            return;
        }

        var devotion = Hero.MainHero.GetDevotionLevelForReligion(heroReligion);
        if ((int)devotion < (int)DevotionLevel.Devoted) return;

        var troop = component.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
        if (troop == null) return;

        var previousStep = (int)(previousProgress / ProgressPerHour);
        var currentStep = (int)(currentProgress / ProgressPerHour);

        var stepsAdvanced = currentStep - previousStep;
        if (stepsAdvanced < 1) stepsAdvanced = 1;

        for (int i = 0; i < stepsAdvanced; i++)
        {
            var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
            if (freeSlots < 1) break;

            var count = MBRandom.RandomInt(1, 4);

            if (Hero.MainHero.HasCareerChoice("AxeOfGrimnirPassive4"))
            {
                count *= 2;
            }

            if (freeSlots < count) count = freeSlots;

            MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
            CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero, settlement, null, troop, count);
            numberOfTroopsFromInteraction += count;
        }
    }

    private void DefileResultConsequence()
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;
        var shrineReligion = component.Religion;

        // Necrarch career perk for shrine defiling
        bool hasUnhallowedSoul = Hero.MainHero.HasCareerChoice("UnhallowedSoulPassive2");

        // UnhallowedSoulPassive2: Wraith summoning chance
        if (hasUnhallowedSoul)
        {
            int wraithChance = 15; // 15% chance

            if (MBRandom.RandomInt(0, 100) < wraithChance)
            {
                var wraith = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_cairn_wraith");
                if (wraith != null)
                {
                    int wraithCount = MBRandom.RandomInt(1, 3);
                    MobileParty.MainParty.MemberRoster.AddToCounts(wraith, wraithCount);
                    var text = TORTextHelper.GetTextObject("tor_shrine_defile_wraith_summon", "The dark energies have drawn {WRAITH_COUNT} wraith(s) to your service.");
                    text.SetTextVariable("WRAITH_COUNT", wraithCount);
                    MBInformationManager.AddQuickInformation(text);
                }
            }
        }

        foreach (var hero in Campaign.Current.AliveHeroes)
        {
            if (hero.IsNotable) continue;//excluded from extended info, unable to have a religion

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

            if (shrineReligion.Pantheon == dominantReligion.Pantheon) hero.SetPersonalRelation(Hero.MainHero, (int)relation - 10);
        }
        Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().SetLastDefileTime(Hero.MainHero, (int)CampaignTime.Now.ToDays);
    }

    private void LootResultConsequence()
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;
        var shrineReligion = component.Religion;

        // Orc Shaman perks - grant bonuses after successful looting
        if (Hero.MainHero.HasCareerChoice("BonesAnFirepitzPassive4"))
        {
            Hero.MainHero.AddSkillXp(TORSkills.Spellcraft, 200);
        }

        if (Hero.MainHero.HasCareerChoice("GiftzFromDaGreatGreenPassive1"))
        {
            Hero.MainHero.AddSkillXp(TORSkills.Faith, 200);
        }

        if (Hero.MainHero.HasCareerChoice("VisionsUvDaOrcaynePassive3"))
        {
            Hero.MainHero.AddCustomResource("WindsOfMagic", 10);
        }

        foreach (var hero in Campaign.Current.AliveHeroes)
        {
            if (hero.IsNotable) continue;

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

            if (shrineReligion.Pantheon == dominantReligion.Pantheon) hero.SetPersonalRelation(Hero.MainHero, (int)relation - 10);
        }
        Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().SetLastDefileTime(Hero.MainHero, (int)CampaignTime.Now.ToDays);
    }


    /// <summary>
    /// Checks if a hero's culture is compatible with a shrine's religion for praying.
    /// Uses Pantheon matching - Undead cultures (Mousillon/Sylvania) won't match Human shrines.
    /// </summary>
    private static bool IsCultureCompatibleWithShrine(Hero hero, ReligionObject religion)
    {
        if (hero == null || religion == null) return false;

        return ReligionObjectHelper.GetPantheon(hero.Culture?.StringId) == religion.Pantheon;
    }

    public static bool CanPartyGoToShrine(MobileParty party)
    {
        if (party.LeaderHero == null) return false;

        // Vampires, Necromancers, and Black Grail Knights cannot visit shrines for praying
        if (party.LeaderHero.IsVampire() ||
            party.LeaderHero.IsNecromancer() ||
            party.LeaderHero.HasCareer(TORCareers.BlackGrailKnight))
        {
            return false;
        }

        return party.IsLordParty && !party.IsEngaging && party.IsActive && !party.IsDisbanding && !party.IsCurrentlyUsedByAQuest && party.CurrentSettlement == null && party.MapEvent == null && !party.Ai.IsDisabled && party.Army == null && !party.HasAnyActiveBlessing();
    }

    private void PrayResultInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as ShrineComponent;
        var godName = TORTextHelper.GetTextObject("tor_religion_name_of_god", component.Religion.StringId, "the god", skipValidation: true);
        MBTextManager.SetTextVariable("GOD_NAME", godName);
        MBTextManager.SetTextVariable("PRAY_RESULT", TORTextHelper.GetTextObject("tor_custom_settlement_shrine_pray_result", "You received the blessing of {GOD_NAME}."));
        MBTextManager.SetTextVariable("FOLLOWERS_RESULT", string.Empty);
        MBTextManager.SetTextVariable("FOLLOWER_RESULT_NUMBER", string.Empty);
        MBTextManager.SetTextVariable("FOLLOWER_RESULT_TROOP", string.Empty); // it leaks
        if (numberOfTroopsFromInteraction > 0)
        {
            var troop = component.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
            MBTextManager.SetTextVariable("FOLLOWER_RESULT_NUMBER", numberOfTroopsFromInteraction.ToString());
            MBTextManager.SetTextVariable("FOLLOWER_RESULT_TROOP", troop.EncyclopediaLinkWithName);
            MBTextManager.SetTextVariable("FOLLOWERS_RESULT", TORTextHelper.GetTextObject("tor_custom_settlement_shrine_follower_result", "Witnessing your prayers have inspired {FOLLOWER_RESULT_NUMBER} {FOLLOWER_RESULT_TROOP} to join your party."));
            numberOfTroopsFromInteraction = 0;
        }
    }


    public ShrineMenuLogic(CampaignGameStarter campaignGameStarter) : base(campaignGameStarter)
    {
    }
}