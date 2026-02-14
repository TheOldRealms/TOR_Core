using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Missions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class TrollCaveMenuLogic(CampaignGameStarter starter) : TORBaseSettlementMenuLogic(starter)
{
    private const int MeatCost = 10;
    private const float TotalWaitHours = 4f;
    private const string TrollTroopId = "tor_gs_trolls";
    private const int MaxTroopsForRaid = 20;
    private const int ClearTrollCount = 6; // Base trolls when clearing the cave
    private const int LuringAttackTrollCountMin = 2; // Min trolls when luring goes wrong
    private const int LuringAttackTrollCountMax = 4; // Max trolls when luring goes wrong

    private int _trollsRecruited = 0;
    private bool _trollsAggressive = false;
    private int _battleTrollCount = 0;
    private bool _playerWonBattle = false;
    private bool _isClearingCave = false; // True when clearing, false when luring gone wrong
    private Settlement _currentCaveSettlement = null; // Store reference before mission

    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddTrollCaveMenus(campaignGameStarter);
    }

    private void AddTrollCaveMenus(CampaignGameStarter starter)
    {
        // Main menu
        starter.AddGameMenu("trollcave_menu", "{LOCATION_DESCRIPTION}", TrollCaveMenuInit);

        // Lure trolls option (Greenskins only)
        starter.AddGameMenuOption("trollcave_menu", "lure_trolls",
            "{LURE_TROLLS_TEXT}",
            LureTrollsCondition,
            (args) => GameMenu.SwitchToMenu("trollcave_menu_luring"));

        // Clear the cave option (all factions)
        starter.AddGameMenuOption("trollcave_menu", "clear_cave",
            TORTextHelper.GetText("tor_trollcave_clear_option", "Clear the cave"),
            ClearCaveCondition,
            StartClearCave);

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
            BattleResultContinue, true);

        // Troll attack encounter menu
        starter.AddGameMenu("trollcave_attack", "{TROLLCAVE_ATTACK_TEXT}", TrollCaveAttackInit);
        starter.AddGameMenuOption("trollcave_attack", "trollcave_attack_fight",
            TORTextHelper.GetText("tor_trollcave_attack_fight", "Fight!"),
            delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                return true;
            },
            StartTrollBattle, false);
    }

    private void TrollCaveMenuInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as TrollCaveComponent;

        string descriptionText;
        var behavior = Campaign.Current.GetCampaignBehavior<TrollCaveCampaignBehavior>();

        if (behavior != null && behavior.IsCaveOnCooldown(settlement))
        {
            string baseText = "The cave is quiet. The trolls you slew still litter the ground, but their kind will return in time.";

            if (Game.Current.CheatMode)
            {
                int daysRemaining = behavior.GetCooldownDaysRemaining(settlement);
                baseText += $" ({daysRemaining} days)";
            }

            descriptionText = baseText;
        }
        else if (!component.IsActive)
        {
            descriptionText = TORTextHelper.GetTextObject("tor_trollcave_inactive",
                "The cave is empty. No trolls dwell here currently.",
                skipValidation: true).ToString();
        }
        else
        {
            descriptionText = TORTextHelper.GetTextObject("tor_customsettlement_intro", settlement.StringId,
                "A dark cave emanates a terrible stench. Massive footprints and gnawed bones litter the entrance - clear signs of troll habitation.",
                skipValidation: true).ToString();
        }

        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", descriptionText);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
    }

    private bool LureTrollsCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement == null) return false;

        var component = settlement.SettlementComponent as TrollCaveComponent;
        if (component == null) return false;

        // Check cooldown - always use current settlement
        var behavior = Campaign.Current.GetCampaignBehavior<TrollCaveCampaignBehavior>();
        if (behavior != null && behavior.IsCaveOnCooldown(settlement))
        {
            return false;
        }

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

    private bool ClearCaveCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement == null) return false;

        var component = settlement.SettlementComponent as TrollCaveComponent;
        if (component == null) return false;

        args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

        // Check cooldown - always use current settlement
        var behavior = Campaign.Current.GetCampaignBehavior<TrollCaveCampaignBehavior>();
        if (behavior != null && behavior.IsCaveOnCooldown(settlement))
        {
            return false;
        }

        // Check if wounded
        if (Hero.MainHero.IsWounded)
        {
            args.Tooltip = TORTextHelper.GetTextObject("tor_wounded", "You are wounded.");
            args.IsEnabled = false;
        }

        return component.IsActive;
    }

    private void StartClearCave(MenuCallbackArgs args)
    {
        _isClearingCave = true;
        _battleTrollCount = ClearTrollCount + MBRandom.RandomInt(0, 3); // 6-8 trolls

        var preSelectedTroops = TroopRoster.CreateDummyTroopRoster();
        var strongestTroops = MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, MaxTroopsForRaid, false);
        preSelectedTroops.Add(strongestTroops);

        // Filter out any trolls from pre-selection
        FilterOutTrolls(preSelectedTroops);

        args.MenuContext.OpenTroopSelection(
            MobileParty.MainParty.MemberRoster,
            preSelectedTroops,
            CanSelectTroopForCaveMission,
            OnClearTroopSelectionDone,
            MaxTroopsForRaid,
            0);
    }

    private void FilterOutTrolls(TroopRoster roster)
    {
        var trollCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(TrollTroopId);
        if (trollCharacter != null && roster.Contains(trollCharacter))
        {
            roster.RemoveTroop(trollCharacter, roster.GetTroopCount(trollCharacter));
        }
    }

    private bool CanSelectTroopForCaveMission(CharacterObject character)
    {
        // Don't allow selecting player, non-transferable troops, or trolls
        if (character.IsPlayerCharacter || character.IsNotTransferableInHideouts)
            return false;

        // Exclude trolls - can't bring trolls to fight trolls
        if (character.StringId == TrollTroopId)
            return false;

        return true;
    }

    private void OnClearTroopSelectionDone(TroopRoster selectedTroops)
    {
        // Player cancelled selection
        if (selectedTroops == null)
        {
            GameMenu.SwitchToMenu("trollcave_menu");
            return;
        }

        // Store settlement reference before mission
        _currentCaveSettlement = Settlement.CurrentSettlement;

        // Start mission with stealth mode (trolls not alerted) - player can go alone
        TorMissionManager.OpenTrollCaveMission(selectedTroops, _battleTrollCount, OnMissionEnd, stealthMode: true);
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

    private void StartTrollBattle(MenuCallbackArgs args)
    {
        _isClearingCave = false;
        // Luring gone wrong - fewer trolls based on how many were being recruited
        _battleTrollCount = MBRandom.RandomInt(LuringAttackTrollCountMin, LuringAttackTrollCountMax + 1);

        var preSelectedTroops = TroopRoster.CreateDummyTroopRoster();
        // Don't include heroes - player is spawned separately by the mission controller
        var strongestTroops = MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, MaxTroopsForRaid, false);
        preSelectedTroops.Add(strongestTroops);

        // Filter out trolls - can't bring trolls to fight trolls
        FilterOutTrolls(preSelectedTroops);

        args.MenuContext.OpenTroopSelection(
            MobileParty.MainParty.MemberRoster,
            preSelectedTroops,
            CanSelectTroopForCaveMission,
            OnTroopSelectionDone,
            MaxTroopsForRaid,
            0);
    }

    private void OnTroopSelectionDone(TroopRoster selectedTroops)
    {
        // Player cancelled selection
        if (selectedTroops == null)
        {
            GameMenu.SwitchToMenu("trollcave_attack");
            return;
        }

        // Store settlement reference before mission
        _currentCaveSettlement = Settlement.CurrentSettlement;

        // Luring gone wrong - trolls are already alerted (no stealth) - player can fight alone
        TorMissionManager.OpenTrollCaveMission(selectedTroops, _battleTrollCount, OnMissionEnd, stealthMode: false);
    }

    private void OnMissionEnd(bool playerWon)
    {
        _playerWonBattle = playerWon;

        // Set cooldown immediately when player wins
        if (playerWon && _currentCaveSettlement != null)
        {
            var behavior = Campaign.Current.GetCampaignBehavior<TrollCaveCampaignBehavior>();
            behavior?.SetCaveCleared(_currentCaveSettlement);
        }

        // Clear the stored settlement reference now that we're done with it
        _currentCaveSettlement = null;

        GameMenu.SwitchToMenu("trollcave_result_battle");
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
        if (_playerWonBattle)
        {
            MBTextManager.SetTextVariable("TROLLCAVE_BATTLE_RESULT",
                TORTextHelper.GetTextObject("tor_trollcave_battle_victory",
                    "You cleared the troll cave! The beasts have been slain. It will take time for more trolls to move in."));
        }
        else
        {
            MBTextManager.SetTextVariable("TROLLCAVE_BATTLE_RESULT",
                TORTextHelper.GetTextObject("tor_trollcave_battle_defeat",
                    "The trolls proved too powerful. You barely escaped with your life."));
        }
    }

    private void BattleResultContinue(MenuCallbackArgs args)
    {
        // Force re-check of cave status by switching to the main menu
        // The cooldown was already set in OnMissionEnd
        GameMenu.SwitchToMenu("trollcave_menu");
    }
}
