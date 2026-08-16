using Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Missions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement;

public class GreenskinBrawlBehavior : CampaignBehaviorBase
{
    private bool _isMissionStarted;
    private BrawlMissionResult _lastBrawlResult;
    private const string _goblinTroopId = "tor_gs_goblin";
    private const string _orcTroopId = "tor_gs_orc_boy";
    private const string _blackOrcTroopId = "tor_gs_black_orc";

    private TroopRoster _lastTroopRoster;
    private Dictionary<string, CampaignTime> _brawlCooldowns = new();


    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, AfterSessionLaunched);
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnded);
    }


    private void OnBattleEnded(MapEvent mapevent)
    {

        if (_isMissionStarted && mapevent.WinningSide == mapevent.PlayerSide)
        {
            _isMissionStarted = false;

        }
    }


    private void AfterSessionLaunched(CampaignGameStarter obj)
    {
        var menu = Campaign.Current.GameMenuManager.GetGameMenu("town");
        TORSettlementMenuHelpers.RearrangeTownMenus(menu, "town_greenskin_brawl", "recruit_volunteers");
    }

    private void OnSessionStart(CampaignGameStarter starter)
    {
        AddTownMenuButton(starter);

    }


    private void AddTownMenuButton(CampaignGameStarter starter)
    {
        var culture = Hero.MainHero.Culture;
        starter.AddGameMenuOption("town", "town_greenskin_brawl", GameTexts.FindText("tor_greenskin_brawl_start").ToString(),
            args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                bool canBrawl = CanStartBrawl();
                bool shouldBeDisabled = ShouldBeDisabled(canBrawl, out TextObject disableReason);

                
                bool isHireling = Hero.MainHero.IsEnlisted();
                if (canBrawl && isHireling)
                {
                    var hirelingDisableReason = GameTexts.FindText("tor_greenskin_brawl_disabled_hireling");
                    return MenuHelper.SetOptionProperties(args, false, true, hirelingDisableReason);
                }



                if (shouldBeDisabled)
                {
                    canBrawl = false;
                }
                return MenuHelper.SetOptionProperties(args, canBrawl, shouldBeDisabled, disableReason ?? TextObject.GetEmpty());
            },
            args =>
            {
                StartBrawl();
            },
            false, 7, false, null);

        AddBrawlResultMenus(starter);
    }

    private bool ShouldBeDisabled(bool canBrawl, out TextObject disableReason)
    {
        disableReason = null;

        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
        {
            return false; // The button should not even appear. the true statement prevents it
        }

        if (canBrawl)
        {
            // Check cooldown
            var settlementId = Settlement.CurrentSettlement.StringId;
            if (_brawlCooldowns.ContainsKey(settlementId))
            {
                var lastBrawlTime = _brawlCooldowns[settlementId];
                var cooldownPeriod = CampaignTime.Weeks(1); // 1 week cooldown
                var timeSinceLastBrawl = lastBrawlTime.ElapsedHoursUntilNow;

                if (timeSinceLastBrawl < cooldownPeriod.ToHours)
                {
                    var remainingHours = cooldownPeriod.ToHours - timeSinceLastBrawl;
                    var remainingDays = (int)Math.Ceiling(remainingHours / 24.0);

                    var disableText = GameTexts.FindText("tor_greenskin_brawl_disabled");
                    disableText.SetTextVariable("COOLDOWN_DAYS", remainingDays);
                    disableReason = disableText;
                    return true; // Still on cooldown
                }
            }
        }

        return false;
    }
    private void AddBrawlResultMenus(CampaignGameStarter starter)
    {
        var text = GameTexts.FindText("tor_greenskin_brawl_intro");
        starter.AddGameMenu("brawl_victory", "{BRAWL_WIN_DESCRIPTION}", CalculateWinResult, GameMenu.MenuOverlayType.None);
        starter.AddGameMenuOption("brawl_victory", "brawl_victory_accept", GameTexts.FindText("tor_greenskin_brawl_victory_accept").ToString(),
            args =>
            {
                return MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty());
            },
            args =>
            {
                GameMenu.SwitchToMenu("town");
            });

        starter.AddGameMenu("brawl_defeat", "{BRAWL_DEFEAT_DESCRIPTION}", SetDefeat, GameMenu.MenuOverlayType.None);
        starter.AddGameMenuOption("brawl_defeat", "brawl_defeat_accept", GameTexts.FindText("tor_greenskin_brawl_defeat_accept").ToString(),
            args => MenuHelper.SetOptionProperties(args, true, true, TextObject.GetEmpty()),
            args =>
            {
                GameMenu.SwitchToMenu("town");
            });
    }

    private bool CanStartBrawl()
    {
        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
        {
            return false;
        }
        if (Settlement.CurrentSettlement?.Culture?.StringId != TORConstants.Cultures.GREENSKIN)
            return false;

        if (!Settlement.CurrentSettlement.IsTown || Settlement.CurrentSettlement.Town?.GarrisonParty?.MemberRoster?.TotalManCount <= 0)
        {
            return false;
        }

        return true;
    }

    private void StartBrawl()
    {
        // Record cooldown
        var settlementId = Settlement.CurrentSettlement.StringId;
        _brawlCooldowns[settlementId] = CampaignTime.Now;


        _lastTroopRoster = null;
        TroopRoster playerRoster = TroopRoster.CreateDummyTroopRoster();

        var enemyPartySize = 0;
        foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
        {
            playerRoster.AddToCounts(hero.CharacterObject, 1);
            enemyPartySize += 5;
        }
        //Sly : why is this pulling from the militia party when the condition to start a brawl checks the garrison party?
        var completeRoster = Settlement.CurrentSettlement.MilitiaPartyComponent.MobileParty.MemberRoster;
        var enemyRoster = TroopRoster.CreateDummyTroopRoster();

        var spawnsLeft = enemyPartySize;
        foreach (var troopElement in completeRoster.GetTroopRoster())
        {
            if (spawnsLeft <= 0) break;

            var shouldAdd = troopElement.Character.Tier switch
            {
                > 6 => MBRandom.RandomFloat < 0.3f,  // High tier - 30% chance
                >= 4 and <= 6 => MBRandom.RandomFloat < 0.6f,  // Mid tier - 60% chance
                < 4 => true,  // Low tier - always taken
            };

            if (!shouldAdd) continue;

            var addCount = Math.Min(troopElement.Number, spawnsLeft);
            enemyRoster.AddToCounts(troopElement.Character, addCount);
            spawnsLeft -= addCount;
        }

        // Get the tavern location and check if the scene is a greenskin or dwarf tavern
        // If not and we have more than 4 companions, use the center (city streets) instead
        // Other culture taverns are too small for large brawls
        Location location = LocationComplex.Current.GetLocationWithId("tavern");
        var companionCount = playerRoster.TotalHeroes;

        if (companionCount > 4)
        {
            var wallLevel = Settlement.CurrentSettlement.Town?.GetWallLevel() ?? 3;
            var sceneName = location.GetSceneName(wallLevel);

            if (!sceneName.Contains("greenskin") && !sceneName.Contains("dwarf"))
            {
                // Use the center location instead - it's larger and can accommodate more fighters
                location = LocationComplex.Current.GetLocationWithId("center");
            }
        }

        TroopRoster playerSideTroops = playerRoster;//Sly : why is this passed then not used and the list is rebuilt?
        TroopRoster rivalSideTroops = enemyRoster;

        _lastTroopRoster = rivalSideTroops;


        _isMissionStarted = true;
        var mission = TorMissionManager.OpenBrawlFightMission(location, playerSideTroops, rivalSideTroops, enemyPartySize, OnBrawlMissionEnd);
        mission.DoesMissionRequireCivilianEquipment = false;
    }

    private void OnBrawlMissionEnd(BrawlMissionResult result)
    {
        _isMissionStarted = false;
        _lastBrawlResult = result;

        var victory = result.PlayerVictory;

        if (victory)
        {
            TORCampaignEvents.Instance.OnBrawlWon(Hero.MainHero);
            GameMenu.SwitchToMenu("brawl_victory");
        }
        else
        {
            GameMenu.SwitchToMenu("brawl_defeat");
        }
    }

    private void CalculateWinResult(MenuCallbackArgs args)
    {
        var text = GameTexts.FindText("tor_greenskin_brawl_victory_desc");
        var resultScore = 0f;
        var entries = new List<string>();
        var enemyRosterSize = _lastTroopRoster.TotalManCount;

        foreach (var entry in _lastBrawlResult.Scores)
        {
            var hero = entry.Key;
            entries.Add(hero.Name + " " + entry.Value);
            if (entry.Key == Hero.MainHero)
            {
                resultScore += entry.Value;
                resultScore += 10;  //survival bonus
            }
            if (entry.Key != Hero.MainHero)
            {
                resultScore += entry.Value / 2;
                resultScore += 5;  //survival bonus
            }
        }

        if (enemyRosterSize > 50)
        {
            resultScore += enemyRosterSize;
        }

        var pouchedRoster = TroopRoster.CreateDummyTroopRoster();

        // Base troop recruitment on enemy roster size, not inflated by performance scores
        var availablePartySpace = Hero.MainHero.PartyBelongedTo.Party.PartySizeLimit - Hero.MainHero.PartyBelongedTo.MemberRoster.TotalManCount;
        var troopRecruitmentSize = Math.Min(Math.Min(enemyRosterSize, 20), availablePartySpace); // Cap at 20 troops max and available party space

        for (int i = 0; i < troopRecruitmentSize; i++)
        {
            if (MBRandom.RandomFloat < 0.05f)
            {
                var blackOrc = MBObjectManager.Instance.GetObject<CharacterObject>(_blackOrcTroopId);

                pouchedRoster.AddToCounts(blackOrc, 1);
                continue;
            }
            if (MBRandom.RandomFloat < 0.25f)
            {
                var orc = MBObjectManager.Instance.GetObject<CharacterObject>(_orcTroopId);
                pouchedRoster.AddToCounts(orc, 1);
                continue;
            }
            var goblin = MBObjectManager.Instance.GetObject<CharacterObject>(_goblinTroopId);
            pouchedRoster.AddToCounts(goblin, 1);
            continue;
        }

        int goldReward = (int)(MBRandom.RandomInt(50 * (int)resultScore, 150 * (int)resultScore));
        int renownReward = (int)resultScore / 3;
        int teefWin = (int)resultScore;

        var customResourceIcon = Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText();

        var description = GameTexts.FindText("tor_greenskin_brawl_victory_reward");
        GameTexts.SetVariable("GOLD_REWARD", goldReward);
        GameTexts.SetVariable("RENOWN_REWARD", renownReward);
        GameTexts.SetVariable("TEEF_REWARD", teefWin);
        GameTexts.SetVariable("CR_ICON", customResourceIcon);

        Hero.MainHero.ChangeHeroGold(goldReward);
        Clan.PlayerClan.AddRenown(renownReward);
        Hero.MainHero.AddCultureSpecificCustomResource(teefWin);

        if (pouchedRoster.Count > 0)
        {
            var impressedTroopDescription = GameTexts.FindText("tor_greenskin_brawl_victory_troops");
            var pouchedTroopLines = new List<string>();

            foreach (var element in pouchedRoster.GetTroopRoster())
            {
                pouchedTroopLines.Add("\n" + element.Character.Name + " " + element.Number);
                Hero.MainHero.PartyBelongedTo.MemberRoster.Add(element);
            }

            var stringBuilder = new StringBuilder();
            foreach (var line in pouchedTroopLines)
            {
                stringBuilder.Append(line);
            }

            GameTexts.SetVariable("POUCHED_TROOPS", stringBuilder.ToString());
            GameTexts.SetVariable("TROOP_REWARD", impressedTroopDescription);
        }
        else if (availablePartySpace <= 0)
        {
            // Show message when no troops could be recruited due to full party
            var noSpaceMessage = GameTexts.FindText("tor_greenskin_brawl_no_space");
            GameTexts.SetVariable("TROOP_REWARD", noSpaceMessage);
        }
        else
        {
            // No troops recruited due to randomness, not party space
            GameTexts.SetVariable("TROOP_REWARD", TextObject.GetEmpty());
        }

        GameTexts.SetVariable("BRAWL_RESULT", description);

        MBTextManager.SetTextVariable("BRAWL_WIN_DESCRIPTION", text);
    }

    private void SetDefeat(MenuCallbackArgs args)
    {
        var enemyCount = _lastTroopRoster.TotalManCount;

        int goldLoss = MBRandom.RandomInt(25 * enemyCount, 100 * enemyCount);

        int teef = enemyCount;

        Hero.MainHero.ChangeHeroGold(-goldLoss);
        Hero.MainHero.AddCultureSpecificCustomResource(-teef);

        GameTexts.SetVariable("TEEF_LOSS", teef);
        GameTexts.SetVariable("GOLD_LOSS", goldLoss);
        GameTexts.SetVariable("CR_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());

        var text = GameTexts.FindText("tor_greenskin_brawl_defeat_desc");
        MBTextManager.SetTextVariable("BRAWL_DEFEAT_DESCRIPTION", text);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_brawlCooldowns", ref _brawlCooldowns);
    }


}

public class BrawlMissionResult(MissionResult missionResult, Dictionary<Hero, int> scores, List<Hero> losses) :
    MissionResult(missionResult.BattleState,
    missionResult.PlayerVictory, missionResult.PlayerDefeated, missionResult.EnemyRetreated)
{
    public Dictionary<Hero, int> Scores = scores;
    public List<Hero> Losses = losses;
}