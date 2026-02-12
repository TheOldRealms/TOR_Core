using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class TrollCaveMenuLogic(CampaignGameStarter starter) : TORBaseSettlementMenuLogic(starter)
{
    private const int MeatCost = 10;
    private const float TotalWaitHours = 4f;
    private const string TrollTroopId = "tor_gs_trolls";

    private int _trollsRecruited = 0;
    private bool _trollsAggressive = false;

    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddTrollCaveMenus(campaignGameStarter);
    }

    private void AddTrollCaveMenus(CampaignGameStarter starter)
    {
        // Main menu
        starter.AddGameMenu("trollcave_menu", "{LOCATION_DESCRIPTION}", TrollCaveMenuInit);

        // Lure trolls option
        starter.AddGameMenuOption("trollcave_menu", "lure_trolls",
            "{LURE_TROLLS_TEXT}",
            LureTrollsCondition,
            (args) => GameMenu.SwitchToMenu("trollcave_menu_luring"));

        // Leave option
        starter.AddGameMenuOption("trollcave_menu", "leave",
            TORTextHelper.GetText("tor_custom_settlement_menu_leave", "Leave..."),
            delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            },
            (args) => PlayerEncounter.Finish(true), true);

        // Wait menu for luring
        starter.AddWaitGameMenu("trollcave_menu_luring",
            "{LURING_PROGRESS_TEXT}",
            LuringInit,
            null,
            LuringConsequence,
            LuringTick,
            GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption,
            GameMenu.MenuOverlayType.None,
            TotalWaitHours,
            GameMenu.MenuFlags.None,
            null);

        // Success result menu
        starter.AddGameMenu("trollcave_result_success", "{TROLLCAVE_SUCCESS_RESULT}", TrollCaveSuccessInit);
        starter.AddGameMenuOption("trollcave_result_success", "return_to_root",
            TORTextHelper.GetText("tor_custom_settlement_menu_continue", "Continue"),
            delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return true;
            },
            (args) => GameMenu.SwitchToMenu("trollcave_menu"), true);

        // Battle result menu (shown after returning from battle)
        starter.AddGameMenu("trollcave_result_battle", "{TROLLCAVE_BATTLE_RESULT}", TrollCaveBattleResultInit);
        starter.AddGameMenuOption("trollcave_result_battle", "return_to_root",
            TORTextHelper.GetText("tor_custom_settlement_menu_continue", "Continue"),
            delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return true;
            },
            (args) => GameMenu.SwitchToMenu("trollcave_menu"), true);

        // Troll attack encounter menu
        starter.AddGameMenu("trollcave_attack", "{TROLLCAVE_ATTACK_TEXT}", TrollCaveAttackInit);
        starter.AddGameMenuOption("trollcave_attack", "trollcave_attack_fight",
            TORTextHelper.GetText("tor_trollcave_attack_fight", "Fight!"),
            delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                return true;
            },
            (args) => StartTrollBattle(), false);
    }

    private void TrollCaveMenuInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as TrollCaveComponent;

        var text = TORTextHelper.GetTextObject("tor_customsettlement_intro", settlement.StringId,
            "A dark cave emanates a terrible stench. Massive footprints and gnawed bones litter the entrance - clear signs of troll habitation.",
            skipValidation: true);

        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
    }

    private bool LureTrollsCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as TrollCaveComponent;

        // Set up the text variable for meat cost
        var meatIcon = CustomResourceManager.GetResourceObject("Meat").GetCustomResourceIconAsText();
        MBTextManager.SetTextVariable("MEAT_COST", MeatCost);
        MBTextManager.SetTextVariable("MEATICON", meatIcon);

        var lureText = TORTextHelper.GetTextObject("tor_trollcave_lure_option",
            "Lure some trolls ({MEAT_COST} {MEATICON})");
        MBTextManager.SetTextVariable("LURE_TROLLS_TEXT", lureText);

        args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveTroops;

        // Only Greenskin culture can interact
        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
        {
            return false;
        }

        // Check meat cost (inventory item, not custom resource)
        var currentMeat = MobileParty.MainParty.ItemRoster.GetItemNumber(DefaultItems.Meat);
        if (currentMeat < MeatCost)
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_trollcave_not_enough_meat",
                "You need at least {MEAT_COST} Meat to lure the trolls.");
            args.IsEnabled = false;
        }

        // Check party space
        var freeSlots = MobileParty.MainParty.Party.PartySizeLimit - MobileParty.MainParty.MemberRoster.TotalManCount;
        if (freeSlots <= 0)
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_trollcave_party_full",
                "Your party is full. You cannot recruit any trolls.");
            args.IsEnabled = false;
        }

        // Check if wounded
        if (Hero.MainHero.IsWounded)
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_wounded", "You are wounded.");
            args.IsEnabled = false;
        }

        return component.IsActive;
    }

    private void LuringInit(MenuCallbackArgs args)
    {
        _startWaitTime = CampaignTime.Now;
        _trollsRecruited = 0;
        _trollsAggressive = false;
        numberOfTroopsFromInteraction = 0;

        // Deduct meat cost (from inventory)
        MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Meat, -MeatCost);

        var luringText = TORTextHelper.GetTextObject("tor_trollcave_luring_progress",
            "Laying out meat to lure the trolls...");
        MBTextManager.SetTextVariable("LURING_PROGRESS_TEXT", luringText);

        PlayerEncounter.Current.IsPlayerWaiting = true;
        args.MenuContext.GameMenu.StartWait();
    }

    private void LuringTick(MenuCallbackArgs args, CampaignTime dt)
    {
        float progress = args.MenuContext.GameMenu.Progress;
        int hoursElapsed = (int)_startWaitTime.ElapsedHoursUntilNow;

        if (hoursElapsed > 0)
        {
            // Update progress (0.25 per hour for 4 hours total)
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(hoursElapsed * 0.25f);

            // Only process on hour change
            if (args.MenuContext.GameMenu.Progress != progress)
            {
                // Check for aggression first - can happen any tick
                float aggressionChance = CalculateAggressionChance();
                if (MBRandom.RandomFloat < aggressionChance)
                {
                    _trollsAggressive = true;
                    // End the wait early - trolls attack!
                    args.MenuContext.GameMenu.EndWait();
                    args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
                    PlayerEncounter.Current.IsPlayerWaiting = false;
                    GameMenu.SwitchToMenu("trollcave_attack");
                    return;
                }

                // Calculate recruitment chance based on Scouting skill
                float recruitChance = CalculateRecruitmentChance();
                if (MBRandom.RandomFloat < recruitChance)
                {
                    var freeSlots = MobileParty.MainParty.Party.PartySizeLimit -
                                   MobileParty.MainParty.MemberRoster.TotalManCount - _trollsRecruited;
                    if (freeSlots > 0)
                    {
                        _trollsRecruited++;
                        numberOfTroopsFromInteraction++;
                    }
                }
            }
        }
    }

    private float CalculateRecruitmentChance()
    {
        // Base 10% chance, modified by Scouting skill
        // At 300 Scouting: 10% + 30% = 40% chance per tick
        float baseChance = 0.10f;
        float scoutingBonus = Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) / 1000f;
        return baseChance + scoutingBonus;
    }

    private float CalculateAggressionChance()
    {
        // Base 5% chance per tick, reduced by Scouting skill
        // At 300 Scouting: 5% - 2.25% = 2.75% chance per tick
        float baseChance = 0.05f;
        float scoutingReduction = Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) / 4000f;
        return Math.Max(0.01f, baseChance - scoutingReduction); // Minimum 1% chance
    }

    private void LuringConsequence(MenuCallbackArgs args)
    {
        PlayerEncounter.Current.IsPlayerWaiting = false;
        args.MenuContext.GameMenu.EndWait();
        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);

        // If trolls didn't become aggressive, add recruited trolls and show success
        if (!_trollsAggressive)
        {
            if (_trollsRecruited > 0)
            {
                var trollCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(TrollTroopId);
                if (trollCharacter != null)
                {
                    MobileParty.MainParty.MemberRoster.AddToCounts(trollCharacter, _trollsRecruited);
                    CampaignEventDispatcher.Instance.OnTroopRecruited(Hero.MainHero,
                        Settlement.CurrentSettlement, null, trollCharacter, _trollsRecruited);
                }
            }
            GameMenu.SwitchToMenu("trollcave_result_success");
        }
        // If aggressive, battle is already started in LuringTick
    }

    private void TrollCaveAttackInit(MenuCallbackArgs args)
    {
        var attackText = TORTextHelper.GetTextObject("tor_trollcave_attack_text",
            "The trolls have turned aggressive! They emerge from the cave, hungry for more than just meat!");
        MBTextManager.SetTextVariable("TROLLCAVE_ATTACK_TEXT", attackText);
    }

    private void StartTrollBattle()
    {
        var settlement = Settlement.CurrentSettlement;

        // Determine number of trolls to fight (recruited trolls + 1-2 additional)
        int battleTrolls = _trollsRecruited + MBRandom.RandomInt(1, 3);

        // Create enemy party with trolls
        var trollCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(TrollTroopId);
        if (trollCharacter == null)
        {
            // Fallback if troll troop doesn't exist
            GameMenu.SwitchToMenu("trollcave_result_battle");
            return;
        }

        // Get the troll party template and a greenskin clan for the battle
        var trollPartyTemplate = MBObjectManager.Instance.GetObject<PartyTemplateObject>("troll_party_template");
        // Use an existing greenskin clan for now (troll_clan_1 can be added later with proper setup)
        Clan trollClan = Clan.FindFirst(x => x.StringId == "troll_clan_1");
        trollClan ??= Clan.FindFirst(x => x.Culture?.StringId == TORConstants.Cultures.GREENSKIN && x.Leader != null);

        if (trollClan == null || trollPartyTemplate == null)
        {
            // Fallback - just go to result menu
            GameMenu.SwitchToMenu("trollcave_result_battle");
            return;
        }

        // Create a party for the battle
        var party = RaidingPartyComponent.CreateRaidingParty(
            settlement.StringId + "_troll_defenders_" + (int)CampaignTime.Now.ElapsedSecondsUntilNow,
            settlement,
            "Angry Trolls",
            trollPartyTemplate,
            trollClan,
            battleTrolls
        );

        // Clear the party and add the exact number of trolls we want
        party.MemberRoster.Clear();
        party.MemberRoster.AddToCounts(trollCharacter, battleTrolls);

        // Start the battle
        PlayerEncounter.RestartPlayerEncounter(party.Party, PartyBase.MainParty, false);
        if (PlayerEncounter.Battle == null)
        {
            PlayerEncounter.StartBattle();
            PlayerEncounter.Update();
        }

        // Use a field battle scene
        CampaignMission.OpenBattleMission("battle_terrain_001", false);
    }

    private void TrollCaveSuccessInit(MenuCallbackArgs args)
    {
        if (_trollsRecruited > 0)
        {
            MBTextManager.SetTextVariable("TROLLS_RECRUITED", _trollsRecruited);
            MBTextManager.SetTextVariable("TROLLCAVE_SUCCESS_RESULT",
                TORTextHelper.GetTextObject("tor_trollcave_success",
                    "The smell of meat drew {TROLLS_RECRUITED} troll(s) from the depths. They seem content to follow you... for now."));
        }
        else
        {
            MBTextManager.SetTextVariable("TROLLCAVE_SUCCESS_RESULT",
                TORTextHelper.GetTextObject("tor_trollcave_no_trolls",
                    "The trolls were not interested in your offerings. Perhaps more meat, or better scouting skills, would help next time."));
        }
    }

    private void TrollCaveBattleResultInit(MenuCallbackArgs args)
    {
        MBTextManager.SetTextVariable("TROLLCAVE_BATTLE_RESULT",
            TORTextHelper.GetTextObject("tor_trollcave_battle_aftermath",
                "The trolls turned hostile! You survived the encounter."));
    }
}
