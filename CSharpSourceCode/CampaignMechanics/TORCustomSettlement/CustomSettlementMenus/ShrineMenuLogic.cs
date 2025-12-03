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
    private const int DefillingCooldownInDays = 5;
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
        starter.AddGameMenuOption("shrine_menu", "loot", "Loot the Shrine for resources. Followers of {GOD_NAME} will remember this", LootCondition, (args) => GameMenu.SwitchToMenu("shrine_menu_looting"));
        starter.AddGameMenuOption("shrine_menu", "donate", "{=tor_custom_settlement_shrine_offering_label_str}Give items as an offering", DonationCondition, (args) => InventoryScreenHelper.OpenScreenAsInventory());//the xp calculation is performed in ReligionCampaignBehavior.OnItemsDiscarded
        starter.AddGameMenuOption("shrine_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate (MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
        starter.AddWaitGameMenu("shrine_menu_praying", "Praying...", delegate (MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            numberOfTroopsFromInteraction = 0;
            PlayerEncounter.Current.IsPlayerWaiting = true;
            args.MenuContext.GameMenu.StartWait();
        }, null, PrayConsequence, PrayingTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, TORConstants.SHRINE_PRAYING_DURATION, GameMenu.MenuFlags.None, null);
        starter.AddGameMenu("shrine_menu_pray_result", "{PRAY_RESULT} {NEWLINE} {FOLLOWERS_RESULT}", PrayResultInit);
        starter.AddGameMenuOption("shrine_menu_pray_result", "return_to_root", "Continue", args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;

        }, args =>
        {
            var model = Campaign.Current.Models.GetFaithModel();
            var settlement = Settlement.CurrentSettlement;
            var component = settlement.SettlementComponent as ShrineComponent;
            model.AddBlessingToParty(MobileParty.MainParty, component.Religion.StringId);

            if (Hero.MainHero.GetDominantReligion() != null && Hero.MainHero.GetPerkValue(TORPerks.Faith.Miracle) && Hero.MainHero.GetDominantReligion() == component.Religion) //&& MBRandom.RandomInt(0, 100) <= TORConstants.MIRACLE_CHANCE) Sly : 100% chance to trigger if someone reaches high enough faith - will need to be better controlled to not trigger constantly when praying at a shrine. Should be considered if someone can receive multiple artifacts, if they can receive an extra copy if they lose it, etc... These items can probably be set to not be lost on becoming prisoner so the player will never lose it unless they discard it which would allow us to have it only trigger once per god/campaign.
            {
                var religion = Hero.MainHero.GetDominantReligion();
                if (religion.ReligiousArtifacts.Count > 0) InkStoryManager.OpenStory("Miracle");
            }
            GameMenu.SwitchToMenu("shrine_menu");

        }, true);
        starter.AddWaitGameMenu("shrine_menu_defiling", "Defiling the shrine...", delegate (MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            PlayerEncounter.Current.IsPlayerWaiting = true;
            args.MenuContext.GameMenu.StartWait();
        }, null, DefileConsequence, DefilingTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
        starter.AddGameMenu("shrine_menu_defile_result", "You successfully gathered " + DefilingDarkEnergyPerTick * 4 + " Dark Energy {DARKENERGYICON}. Followers of {GOD_NAME} will perceive this as a crime.", null);
        starter.AddGameMenuOption("shrine_menu_defile_result", "return_to_root", "Continue", args =>
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;
        }, args =>
        {
            DefileResultConsequence();
            GameMenu.SwitchToMenu("shrine_menu");
        }, true);
        starter.AddWaitGameMenu("shrine_menu_looting", "Looting the shrine...", delegate (MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            PlayerEncounter.Current.IsPlayerWaiting = true;
            args.MenuContext.GameMenu.StartWait();
        }, null, LootConsequence, LootingTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, 4f, GameMenu.MenuFlags.None, null);
        starter.AddGameMenu("shrine_menu_loot_result", "{LOOT_RESULT}. Followers of {GOD_NAME} will perceive this as a crime.", null);
        starter.AddGameMenuOption("shrine_menu_loot_result", "return_to_root", "Continue", args =>
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
            if (lastDefileTime >= (int)CampaignTime.Now.ToDays - DefillingCooldownInDays)
            {
                GameTexts.SetVariable("DEFILE_COOLDOWN_DAYS", DefillingCooldownInDays.ToString());
                args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_once_a_day_text_str}You can only perform this action every {DEFILE_COOLDOWN_DAYS} days.");
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
                if (lastDefileTime >= (int)CampaignTime.Now.ToDays - DefillingCooldownInDays)
                {
                    GameTexts.SetVariable("DEFILE_COOLDOWN_DAYS", DefillingCooldownInDays.ToString());
                    args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_once_a_day_text_str}You can only perform this action every {DEFILE_COOLDOWN_DAYS} days.");
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
                args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_greenskin_own}You can't loot your own shrine, ya git!", null);
                args.IsEnabled = false;
                return true;
            }

            var lastDefileTime = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().LastDefileTime(Hero.MainHero);
            if (lastDefileTime >= (int)CampaignTime.Now.ToDays - DefillingCooldownInDays)
            {
                GameTexts.SetVariable("DEFILE_COOLDOWN_DAYS", DefillingCooldownInDays.ToString());
                args.Tooltip = new TextObject("{=tor_custom_settlement_cursed_site_once_a_day_text_str}You can only perform this action every {DEFILE_COOLDOWN_DAYS} days.");
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
        MBTextManager.SetTextVariable("PRAY_TEXT", "{=tor_custom_settlement_shrine_pray_text_str}Pray to receive the blessing of {GOD_NAME}");

        // Vampires, Necromancers, and Black Grail Knights cannot pray
        if (Hero.MainHero.IsVampire() ||
            Hero.MainHero.IsNecromancer() ||
            Hero.MainHero.HasCareer(TORCareers.BlackGrailKnight))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_undead_no_pray}The undead do not commune with the gods.", null);
            args.IsEnabled = false;
            return true;
        }

        // Culture restriction: can only pray at shrines of your own culture
        if (!IsCultureCompatibleWithShrine(Hero.MainHero, component.Religion))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_wrong_culture}This shrine is not meant for your kind.", null);
            args.IsEnabled = false;
            return true;
        }

        if (CareerHelper.IsPriestCareer(Hero.MainHero.GetCareer()) && CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer()) != component.Religion.StringId)
        {
            var careerGod = CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer());
            var god = ReligionObject.All.FirstOrDefault(x => x.StringId == careerGod);
            MBTextManager.SetTextVariable("CAREERGOD_NAME", god.DeityName);
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_blessing_already_active_str}You devoted your live to {CAREERGOD_NAME}. You can't pray here.", null);
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
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_vampire_no_donate}The undead do not make offerings to the gods.", null);
            args.IsEnabled = false;
            return true;
        }

        // Only Order affinity + Greenskins (Destruction) can donate
        var playerReligion = Hero.MainHero.GetDominantReligion();
        if (playerReligion != null && playerReligion.Affinity != ReligionAffinity.Order && playerReligion.Affinity != ReligionAffinity.Destruction)
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_no_donate_affinity}You do not honor the gods through offerings.", null);
            args.IsEnabled = false;
            return true;
        }

        // Must have compatible culture to donate (uses same logic as praying)
        if (!IsCultureCompatibleWithShrine(Hero.MainHero, component.Religion))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_wrong_culture_donate}You cannot make offerings at a shrine not meant for your kind.", null);
            args.IsEnabled = false;
            return true;
        }

        // Priest careers can only donate at shrines of their deity
        if (CareerHelper.IsPriestCareer(Hero.MainHero.GetCareer()) && CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer()) != component.Religion.StringId)
        {
            var careerGod = CareerHelper.GetGodCareerIsDevotedTo(Hero.MainHero.GetCareer());
            var god = ReligionObject.All.FirstOrDefault(x => x.StringId == careerGod);
            MBTextManager.SetTextVariable("CAREERGOD_NAME", god.DeityName);
            args.Tooltip = new TextObject("{=tor_custom_settlement_shrine_blessing_already_active_str}You devoted your live to {CAREERGOD_NAME}. You can't donate here.", null);
            args.IsEnabled = false;
            return true;
        }

        if (!Hero.MainHero.GetPerkValue(TORPerks.Faith.Offering))
        {
            args.Tooltip = new TextObject("{=tor_custom_settlement_donation_perk_info_str}You need the Offering perk in the Faith skill line to perform this action.", null);
            args.IsEnabled = false;
        }

        return component.IsActive && component.Religion != null;
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
        var godName = GameTexts.FindText("tor_religion_name_of_god", component.Religion.StringId);
        MBTextManager.SetTextVariable("GOD_NAME", godName);

        GameMenu.SwitchToMenu("shrine_menu_loot_result");
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
                    MBTextManager.SetTextVariable("LOOT_RESULT", $"You successfully looted {totalGold} gold, {totalFood} meat, and {totalTeef} Teef");
                }
            }
        }
    }

    private void PrayingTick(MenuCallbackArgs args, CampaignTime dt)
    {
        //Sly : with blessings able to be refreshed before the current one has ran out, do we care that someone can sit at a shrine and spam out praying to acquire a bunch of religious troops in a row?
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
                        var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
                        if (freeSlots < 1) return;

                        var troop = component.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
                        if (troop == null)
                        {
                            return;
                        }

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
            }
        }
    }

    private void DefileResultConsequence()
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;
        var shrineReligion = component.Religion;

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

            if (shrineReligion.Affinity == dominantReligion.Affinity) hero.SetPersonalRelation(Hero.MainHero, (int)relation - 10);
        }
        Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().SetLastDefileTime(Hero.MainHero, (int)CampaignTime.Now.ToDays);
    }

    private void LootResultConsequence()
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement.SettlementComponent is not ShrineComponent component) return;
        var shrineReligion = component.Religion;

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
            else
            {
                if (shrineReligion.Affinity == dominantReligion.Affinity) hero.SetPersonalRelation(Hero.MainHero, (int)relation - 10);
            }

            Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().SetLastDefileTime(Hero.MainHero, (int)CampaignTime.Now.ToDays);
        }
    }


    /// <summary>
    /// Checks if a hero's culture is compatible with a shrine's religion culture for praying.
    /// </summary>
    private static bool IsCultureCompatibleWithShrine(Hero hero, ReligionObject religion)
    {
        if (hero == null || religion == null || religion.Culture == null) return false;

        // Direct culture match
        if (hero.Culture == religion.Culture) return true;

        var heroCultureId = hero.Culture.StringId;
        var religionCultureId = religion.Culture.StringId;

        // Special case: Wood Elves (battania) and High Elves (eonir) can pray at each other's shrines
        bool heroIsElf = heroCultureId == "battania" || heroCultureId == "eonir";
        bool shrineIsElf = religionCultureId == "battania" || religionCultureId == "eonir";
        if (heroIsElf && shrineIsElf) return true;

        // Special case: Human cultures (Empire, Bretonnia) share the same pantheon
        // Note: Mousillon and Sylvania are NOT included - they cannot pray at human shrines
        bool heroIsHuman = heroCultureId == "empire" || heroCultureId == "vlandia";
        bool shrineIsHuman = religionCultureId == "empire" || religionCultureId == "vlandia";
        if (heroIsHuman && shrineIsHuman) return true;

        return false;
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
        var godName = GameTexts.FindText("tor_religion_name_of_god", component.Religion.StringId);
        MBTextManager.SetTextVariable("GOD_NAME", godName);
        MBTextManager.SetTextVariable("PRAY_RESULT", "{=tor_custom_settlement_shrine_pray_result_text_str}You received the blessing of {GOD_NAME}.");
        if (numberOfTroopsFromInteraction > 0)
        {
            var troop = component.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
            MBTextManager.SetTextVariable("FOLLOWER_RESULT_NUMBER", numberOfTroopsFromInteraction.ToString());
            MBTextManager.SetTextVariable("FOLLOWER_RESULT_TROOP", troop.EncyclopediaLinkWithName);
            MBTextManager.SetTextVariable("FOLLOWERS_RESULT", "{=tor_custom_settlement_shrine_follower_result_str}Witnessing your prayers have inspired {FOLLOWER_RESULT_NUMBER} {FOLLOWER_RESULT_TROOP} to join your party.");
            numberOfTroopsFromInteraction = 0;
        }
    }


    public ShrineMenuLogic(CampaignGameStarter campaignGameStarter) : base(campaignGameStarter)
    {
    }
}