using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Missions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomDialogs;

public class DuelBehavior : CampaignBehaviorBase
{
    private Hero _currentDuelTarget;
    private bool _isDuelInProgress;
    private Action _currentVictoryAction;
    private Action _currentDefeatAction;
    private string _textOverride;
    private TroopRoster _currentTroopRoster;
    private Dictionary<string, CampaignTime> _duelCooldowns = new();

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        AddDuelDialogs(campaignGameStarter, _isDuelInProgress);
        AddDuelMenus(campaignGameStarter);
    }

    private void AddDuelDialogs(CampaignGameStarter campaignGameStarter , bool isDuelInProgress)
    {
        if (isDuelInProgress)   //if we ever add another duel we can clarify for advanced
        {
            AddGreenskinDuelDialogs(campaignGameStarter, out _currentVictoryAction, out _currentDefeatAction);
        }
        else
        {
            AddGreenskinDuelDialogs(campaignGameStarter, out _, out _);
        }
    }

    private void AddGreenskinDuelDialogs(CampaignGameStarter campaignGameStarter, out Action victoryAction, out Action defeatAction)
    {
        victoryAction = HandleGreenskinVictory;
        defeatAction = HandleGreenskinDefeat;
        // Greenskin challenge dialog
        campaignGameStarter.AddPlayerLine("tor_greenskin_duel_challenge", "lord_talk_speak_diplomacy_2", "tor_greenskin_duel_response",
            GameTexts.FindText("tor_greenskin_duel_challenge").ToString(),
            CanOfferGreenskinDuel, null, 100);

        // Target accepts Greenskin challenge
        campaignGameStarter.AddDialogLine("tor_greenskin_duel_accept", "tor_greenskin_duel_response", "close_window",
            GameTexts.FindText("tor_greenskin_duel_accept").ToString(),
            WillAcceptGreenskinDuel, () => StartGreenskinDuel(), 100);

        // Target declines due to recent duel (cooldown)
        campaignGameStarter.AddDialogLine("tor_greenskin_duel_decline_cooldown", "tor_greenskin_duel_response", "lord_talk_speak_diplomacy_2", 
            "{DUEL_DECLINE_TEXT}",
            () =>
            { 
                var text = GameTexts.FindText("tor_greenskin_duel_decline_cooldown");
                MBTextManager.SetTextVariable("DUEL_DECLINE_TEXT", text); 
                return IsOnDuelCooldown(); 
            }, null, 100);

        // Target declines Greenskin challenge (other reasons)
        campaignGameStarter.AddDialogLine("tor_greenskin_duel_decline", "tor_greenskin_duel_response", "lord_talk_speak_diplomacy_2",
            GameTexts.FindText("tor_greenskin_duel_decline").ToString(),
            () => !WillAcceptGreenskinDuel() && !IsOnDuelCooldown(), null, 100);


        bool CanOfferGreenskinDuel()
        {
            
            if (Hero.OneToOneConversationHero?.IsLord != true) return false;
            if (_isDuelInProgress) return false;

            var playerCulture = Hero.MainHero.Culture.StringId;
            var targetCulture = Hero.OneToOneConversationHero.Culture.StringId;

            var test = IsOnDuelCooldown();

            // Greenskins can challenge other Greenskins
            return playerCulture == TORConstants.Cultures.GREENSKIN && targetCulture == TORConstants.Cultures.GREENSKIN;
        }

      

        bool WillAcceptGreenskinDuel()
        {
            var target = Hero.OneToOneConversationHero;
            if (target == null) return false;

            // Don't accept if on cooldown
            if (IsOnDuelCooldown()) return false;

            // Check relation - very low relation means they might decline
            var relation = Hero.MainHero.GetRelation(target);
            if (relation < -50) return false;

            // Greenskins are generally more willing to fight
            return true;
        }

        void StartGreenskinDuel()
        {
            GameTexts.SetVariable("TEEF_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
            _currentDuelTarget = Hero.OneToOneConversationHero;

            // Record cooldown for this lord
            var targetId = Hero.OneToOneConversationHero.StringId;
            _duelCooldowns[targetId] = CampaignTime.Now;

            StartDuel(HandleGreenskinVictory, HandleGreenskinDefeat, "greenskin");
        }
        
        void HandleGreenskinVictory()
        {
            // Gain teef from victory
            var teefGain = MBRandom.RandomInt(50, 150);

            // GetToDaChoppasPassive4: 50% extra Teef after dueling Warbosses
            if (Hero.MainHero.HasCareer(TORCareers.OrcBoss) && Hero.MainHero.HasCareerChoice("GetToDaChoppasPassive4"))
            {
                // Check if target is a Warboss (clan leader or has warboss attribute)
                if (_currentDuelTarget.IsClanLeader || _currentDuelTarget.HasAttribute("Warboss"))
                {
                    var choice = TORCareerChoices.GetChoice("GetToDaChoppasPassive4");
                    if (choice != null)
                    {
                        teefGain = (int)(teefGain * 1.5f); // 50% bonus
                    }
                }
            }

            var enemyTroops = _currentDuelTarget.PartyBelongedTo?.MemberRoster;

            // Set text to indicate some troops want to join
            GameTexts.SetVariable("TROOP_GAIN_DUEL", GameTexts.FindText("tor_duel_troops_want_to_join").ToString());

            Hero.MainHero.AddCultureSpecificCustomResource(teefGain);
            GameTexts.SetVariable("TEEF_GAIN_DUEL", teefGain);
            
            
            // Filter for received troops - create a random selection from enemy party
            var filteredRoster = TaleWorlds.CampaignSystem.Roster.TroopRoster.CreateDummyTroopRoster();
            if (enemyTroops != null)
            {
                var availableTroops = enemyTroops.GetTroopRoster().Where(t => !t.Character.IsHero).ToList();
                var baseTroopsToGain = Math.Min(enemyTroops.TotalManCount / 4, 10); // Same calculation as before
                var troopsAdded = 0;
                
                foreach (var troopElement in availableTroops)
                {
                    if (troopsAdded >= baseTroopsToGain) break;

                    // Randomly select how many of this troop type to offer (1 to all available, but limited by remaining slots)
                    var maxToAdd = Math.Min(troopElement.Number, baseTroopsToGain - troopsAdded);
                    var countToAdd = MBRandom.RandomInt(1, maxToAdd + 1);

                    if (countToAdd > 0)
                    {
                        filteredRoster.AddToCounts(troopElement.Character, countToAdd);
                        troopsAdded += countToAdd;
                    }
                }
                
                _currentTroopRoster = filteredRoster.CloneRosterData();
            }
            PartyScreenHelper.OpenScreenAsReceiveTroops(filteredRoster, GameTexts.FindText("tor_duel_troop_selection_title"),
                (party, roster, prisonRoster, ownerParty, memberRoster, rightPrisonRoster, cancel) => 
            {
                //troops taken need to be removed from the original party.

                roster.GetTroopRoster();
                
                foreach (var element in _currentTroopRoster.GetTroopRoster())
                { 
                    //find out how many elements have been taken
                    var count = element.Number- roster.GetTroopCount(element.Character);
                    _currentDuelTarget.PartyBelongedTo.MemberRoster.AddToCounts(element.Character, -count);
                }
                PlayerEncounter.Finish();
                _currentDuelTarget = null;
                _currentTroopRoster = null;
            });
            
        }

        void HandleGreenskinDefeat()
        {
            // Lose teef and some best troops
            // Build troop loss list string similar to brawl behavior
            var troopLines = new List<string>();
            var stringBuilder = new StringBuilder();

            var playerParty = Hero.MainHero.PartyBelongedTo;
            if (playerParty?.MemberRoster != null)
            {
                var troopsToLose = Math.Min(playerParty.MemberRoster.TotalManCount / 8, 5); // Lose 1/8 of troops, max 5
                var troopsLost = 0;
                var playerTroops = playerParty.MemberRoster.GetTroopRoster().Where(t => !t.Character.IsHero).OrderByDescending(t => t.Character.Level).ToList();

                foreach (var troopElement in playerTroops)
                {
                    if (troopsLost >= troopsToLose) break;

                    var countToTransfer = Math.Min(troopElement.Number, troopsToLose - troopsLost);
                    if (countToTransfer > 0)
                    {
                        // Remove from player
                        playerParty.MemberRoster.AddToCounts(troopElement.Character, -countToTransfer);
                        // Add to enemy
                        _currentDuelTarget.PartyBelongedTo.MemberRoster.AddToCounts(troopElement.Character, countToTransfer);
                        troopLines.Add($"\n{troopElement.Character.Name} {countToTransfer}");
                        troopsLost += countToTransfer;
                    }
                }
            }

            foreach (var line in troopLines)
            {
                stringBuilder.Append(line);
            }
            string troops = stringBuilder.ToString();
            GameTexts.SetVariable("TROOP_LOSS_DUEL", troops);

            var teefLoss = MBRandom.RandomInt(25, 75);
            Hero.MainHero.AddCultureSpecificCustomResource(-teefLoss);

            GameTexts.SetVariable("TEEF_LOSS_DUEL", teefLoss);


        }
    }
    
    private bool IsOnDuelCooldown()
    {
        var targetId = Hero.OneToOneConversationHero?.StringId;
        if (targetId == null) return false;

        if (_duelCooldowns.ContainsKey(targetId))
        {
            var lastDuelTime = _duelCooldowns[targetId];
            var cooldownPeriod = CampaignTime.Weeks(2); // 2 week cooldown
            var timeSinceLastDuel = lastDuelTime.ElapsedHoursUntilNow;

            if (timeSinceLastDuel < cooldownPeriod.ToHours)
            {
                // Set variables for the cooldown message
                var remainingHours = cooldownPeriod.ToHours - timeSinceLastDuel;
                var remainingDays = (int)Math.Ceiling(remainingHours / CampaignTime.HoursInDay);
                MBTextManager.SetTextVariable("COOLDOWN_DAYS",remainingDays);
                
                return true; // Still on cooldown
            }
        }

        return false;
    }




    private void AddDuelMenus(CampaignGameStarter campaignGameStarter)
    {
        // Main duel preparation menu
        campaignGameStarter.AddGameMenu("duel_preparation", "{DUEL_PREPARATION_TEXT}",
            SetDuelPreparationText, GameMenu.MenuOverlayType.None);

        // Start the actual duel button
        campaignGameStarter.AddGameMenuOption("duel_preparation", "duel_start_combat",
            GameTexts.FindText("tor_duel_start_combat").ToString(),
            args => MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty()), args =>
            {
                ExecuteDuel(); 
        
            }, false, -1, false, null);
        
        
        // Defeat menu
        campaignGameStarter.AddGameMenu("duel_defeat", "{DUEL_DEFEAT_TEXT}",
            SetDuelDefeatText, GameMenu.MenuOverlayType.None);
      //"army encounter" is the menu we look for, if the player is in a town its "town" in castle "castle"
        campaignGameStarter.AddGameMenuOption("duel_defeat", "duel_accept_defeat",
            GameTexts.FindText("tor_duel_accept_defeat").ToString(),
            args => MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty()), args =>
            {
                _currentDefeatAction?.Invoke();
            }, true, -1, false, null);

        // Victory menu
        campaignGameStarter.AddGameMenu("duel_victory", "{DUEL_VICTORY_TEXT}",
            SetDuelVictoryText, GameMenu.MenuOverlayType.None);

        campaignGameStarter.AddGameMenuOption("duel_victory", "duel_accept_victory",
            GameTexts.FindText("tor_duel_accept_victory").ToString(),
            args => MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty()), args =>
            {
                _currentVictoryAction?.Invoke();
            }, true, -1, false, null);
    }

    private void SetDuelPreparationText(MenuCallbackArgs args)
    {
        var duelTextDescription = GameTexts.FindText("tor_duel_prep");
        if (_textOverride!=null)
        {
            duelTextDescription = GameTexts.FindText("tor_duel_prep", _textOverride);
        }
        duelTextDescription.SetTextVariable("DUEL_TARGET_NAME", _currentDuelTarget.Name);
        MBTextManager.SetTextVariable("DUEL_PREPARATION_TEXT", duelTextDescription);
    }

    private void SetDuelDefeatText(MenuCallbackArgs args)
    {
        var defeatTextDescription = GameTexts.FindText("tor_duel_defeat");
        if (_textOverride != null)
        {
            defeatTextDescription = GameTexts.FindText("tor_duel_defeat", _textOverride);
        }
        defeatTextDescription.SetTextVariable("DUEL_TARGET_NAME", _currentDuelTarget==null? _currentDuelTarget.Name.ToString(): "Unkown");
        MBTextManager.SetTextVariable("DUEL_DEFEAT_TEXT", defeatTextDescription);
    }

    private void SetDuelVictoryText(MenuCallbackArgs args)
    {
        var victoryTextDescription = GameTexts.FindText("tor_duel_victory");
        if (_textOverride != null)
        {
            victoryTextDescription = GameTexts.FindText("tor_duel_victory", _textOverride);
        }

        victoryTextDescription.SetTextVariable("DUEL_TARGET_NAME", _currentDuelTarget==null? _currentDuelTarget.Name.ToString(): "Unkown");
        MBTextManager.SetTextVariable("DUEL_VICTORY_TEXT", victoryTextDescription);
    }

    private void StartDuel(Action onVictory, Action onDefeat, string textOverride)
    {
        _textOverride =  textOverride;
        _currentDuelTarget = Hero.OneToOneConversationHero;
        _isDuelInProgress = true;
        _currentVictoryAction = onVictory;
        _currentDefeatAction = onDefeat;

        // Switch to the duel preparation menu
        GameMenu.SwitchToMenu("duel_preparation");
        
        
    }

    private void ExecuteDuel()
    {
        // This is where the actual duel logic would go
        // For now, we'll simulate the outcome

        _isDuelInProgress = false;
        
        
        TorMissionManager.OpenDuelMission(EvaluateDuel,_currentDuelTarget);
        

        // Return to the town or wherever the player was
    }

    private void EvaluateDuel(bool playerVictory)
    {
        if (playerVictory)
        {
            // Trigger lord duel won event if the opponent is a lord
            if (_currentDuelTarget != null && _currentDuelTarget.IsLord)
            {
                TORCampaignEvents.Instance.OnLordDuelWon(Hero.MainHero, _currentDuelTarget);
            }

            GameMenu.SwitchToMenu("duel_victory");
        }
        else
        {
            _currentDefeatAction?.Invoke();
            GameMenu.SwitchToMenu("duel_defeat");
        }

        _isDuelInProgress = false;
    }



    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_isDuelInProgress", ref _isDuelInProgress);
        dataStore.SyncData("_currentDuelTarget", ref _currentDuelTarget);
        dataStore.SyncData("_textOverride", ref _textOverride);
        dataStore.SyncData("_duelCooldowns", ref _duelCooldowns);
    }
}