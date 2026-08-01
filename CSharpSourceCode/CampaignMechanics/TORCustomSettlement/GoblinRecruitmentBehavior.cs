using Helpers;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Missions;
using TOR_Core.Utilities;


namespace TOR_Core.CampaignMechanics.TORCustomSettlement;

public class GoblinRecruitmentBehavior : CampaignBehaviorBase
{
    private bool _isMissionStarted;
    private const string _goblinTroopId = "tor_gs_goblin";
    private const int _basePartyStrengthThreshold = 100; // Base minimum party strength to avoid fight
    private const int _baseGoblinGroupSize = 15; // Base number of goblins in encounter

    private float _maximumWaitTime = 1;
    private CampaignTime _startWaitTime;
    private float _progress;
    private Dictionary<string, CampaignTime> _goblinRecruitmentCooldowns = new();
    private int _pendingGoblins = 0;

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, AfterSessionLaunched);
        CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, OnOrcRecruitedAddGoblins);
        CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, AddExtraGoblinsForOrcRecruitment);
        CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnAIOrcRecruitedAddGoblins);
    }

    private void AddExtraGoblinsForOrcRecruitment(MobileParty party, Settlement settlement)
    {
        if (party != MobileParty.MainParty) return;
        if (_pendingGoblins <= 0) return;

        var goblinCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(_goblinTroopId);
        if (goblinCharacter == null)
        {
            _pendingGoblins = 0;
            return;
        }

        int availableSpace = party.Party.PartySizeLimit - party.Party.NumberOfAllMembers;
        int goblinsToAdd = Math.Min(_pendingGoblins, Math.Max(0, availableSpace));
        _pendingGoblins = 0;

        if (goblinsToAdd > 0)
        {
            party.AddElementToMemberRoster(goblinCharacter, goblinsToAdd);
            var messageText = TORTextHelper.GetTextObject("tor_goblin_orc_recruitment_message", "{GOBLIN_COUNT} goblin(s) joined by recruiting da big boys!");
            messageText.SetTextVariable("GOBLIN_COUNT", goblinsToAdd);
            InformationManager.DisplayMessage(new InformationMessage(messageText.ToString(), Colors.Green));
        }
    }

    // Very stupid TW Event: it fires for each troop card individually (amount is always 1 ). the check for left size doesnt apply correctly and a single message display is not possible.
    private void OnOrcRecruitedAddGoblins(CharacterObject troop, int amount)
    {
        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN) return;
        if (!troop.IsOrc()) return;

        _pendingGoblins += CalculateGoblinsForOrcs(amount);
    }

    // AI greenskin lords also get goblins when recruiting orcs
    private void OnAIOrcRecruitedAddGoblins(Hero recruiter, Settlement settlement, Hero recruitmentSource, CharacterObject troop, int amount)
    {
        if (recruiter == null || recruiter == Hero.MainHero) return;
        if (recruiter.Culture?.StringId != TORConstants.Cultures.GREENSKIN) return;
        if (!troop.IsOrc()) return;
        if (recruiter.PartyBelongedTo == null) return;

        var party = recruiter.PartyBelongedTo;
        int availableSpace = party.Party.PartySizeLimit - party.Party.NumberOfAllMembers;
        if (availableSpace <= 0) return;

        var goblinCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(_goblinTroopId);
        if (goblinCharacter == null) return;

        int goblinsToAdd = Math.Min(CalculateGoblinsForOrcs(amount), availableSpace);
        if (goblinsToAdd > 0)
        {
            party.AddElementToMemberRoster(goblinCharacter, goblinsToAdd);
        }
    }

    /// <summary>
    /// Calculates how many goblins should join based on orcs recruited (0-2 per orc).
    /// </summary>
    private int CalculateGoblinsForOrcs(int orcAmount)
    {
        int goblins = 0;
        for (var i = 0; i < orcAmount; i++)
        {
            goblins += MBRandom.RandomInt(0, 3);
        }
        return goblins;
    }


    private void OnBattleEnded(MapEvent mapEvent)
    {
        if (_isMissionStarted && mapEvent.WinningSide == mapEvent.PlayerSide)
        {
            _isMissionStarted = false;
            // Player won the goblin fight, add goblins to party
            AddGoblinsToParty(out _);
        }
        else if (_isMissionStarted)
        {
            _isMissionStarted = false;
            // Player lost or retreated
        }
    }

    private void AfterSessionLaunched(CampaignGameStarter obj)
    {
        var menu = Campaign.Current.GameMenuManager.GetGameMenu("town_artisan");
        TORSettlementMenuHelpers.RearrangeTownMenus(menu, "town_goblin_recruitment", "town_artisan_enchanting");
    }

    private void OnSessionStart(CampaignGameStarter starter)
    {
        AddTownMenuButton(starter);
    }

    private void AddTownMenuButton(CampaignGameStarter starter)
    {
        starter.AddGameMenuOption("town_artisan", "town_goblin_recruitment",
            GameTexts.FindText("tor_goblin_recruitment_start").ToString(),
            args =>
            {
                // Hide entirely for non-greenskin players
                if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
                    return false;

                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                bool canRecruit = CanStartGoblinRecruitment();
                bool shouldBeDisabled = ShouldBeDisabled(canRecruit, out TextObject disableReason);
                if (shouldBeDisabled)
                {
                    canRecruit = false;
                }
                return MenuHelper.SetOptionProperties(args, canRecruit, shouldBeDisabled, disableReason ?? TextObject.GetEmpty());
            },
            args =>
            {
                // Record cooldown when recruitment starts
                var settlementId = Settlement.CurrentSettlement.StringId;
                _goblinRecruitmentCooldowns[settlementId] = CampaignTime.Now;

                GameMenu.SwitchToMenu("goblin_recruitment_waiting");
            },
            false, 8, false, null);

        AddGoblinRecruitmentResultMenus(starter);
    }

    private void AddGoblinRecruitmentResultMenus(CampaignGameStarter starter)
    {
        starter.AddWaitGameMenu("goblin_recruitment_waiting", "Looking for goblins...", delegate (MenuCallbackArgs args)
        {
            _startWaitTime = CampaignTime.Now;
            args.MenuContext.GameMenu.StartWait();
        }, null, WaitingConsequence, GoblinSearchTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.None, _maximumWaitTime, GameMenu.MenuFlags.None, null);

        // Goblin encounter choice menu
        starter.AddGameMenu("goblin_encounter", "{GOBLIN_ENCOUNTER_DESCRIPTION}",
            SetGoblinEncounter, GameMenu.MenuOverlayType.None);
        starter.AddGameMenuOption("goblin_encounter", "goblin_encounter_continue",
            GameTexts.FindText("tor_goblin_encounter_continue").ToString(),
            args => MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty()),
            args => HandleGoblinEncounter());

        // Instant bullying success menu
        starter.AddGameMenu("goblin_bullying_success", "{GOBLIN_BULLYING_DESCRIPTION}",
            SetBullyingSuccess, GameMenu.MenuOverlayType.None);
        starter.AddGameMenuOption("goblin_bullying_success", "goblin_bullying_accept",
            GameTexts.FindText("tor_goblin_bullying_accept").ToString(),
            args => MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty()),
            args => GameMenu.SwitchToMenu("town"));

        // Fight victory menu
        starter.AddGameMenu("goblin_fight_victory", "{GOBLIN_FIGHT_VICTORY_DESCRIPTION}",
            SetFightVictory, GameMenu.MenuOverlayType.None);
        starter.AddGameMenuOption("goblin_fight_victory", "goblin_fight_accept",
            GameTexts.FindText("tor_goblin_fight_victory_accept").ToString(),
            args => MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty()),
            args => GameMenu.SwitchToMenu("town"));

        // Fight defeat menu
        starter.AddGameMenu("goblin_fight_defeat", "{GOBLIN_FIGHT_DEFEAT_DESCRIPTION}",
            SetFightDefeat, GameMenu.MenuOverlayType.None);
        starter.AddGameMenuOption("goblin_fight_defeat", "goblin_fight_defeat_accept",
            GameTexts.FindText("tor_goblin_fight_defeat_accept").ToString(),
            args => MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty()),
            args => GameMenu.SwitchToMenu("town"));
    }

    private bool ShouldBeDisabled(bool canRecruit, out TextObject disableReason)
    {
        disableReason = null;

        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
        {
            return true; // The button should not even appear.
        }

        // Check if party is full
        if (Hero.MainHero.PartyBelongedTo.MemberRoster.TotalManCount >= Hero.MainHero.PartyBelongedTo.Party.PartySizeLimit)
        {
            disableReason = GameTexts.FindText("tor_goblin_recruitment_party_full");
            return true;
        }

        if (canRecruit)
        {
            // Check cooldown
            var settlementId = Settlement.CurrentSettlement.StringId;
            if (_goblinRecruitmentCooldowns.ContainsKey(settlementId))
            {
                var lastRecruitmentTime = _goblinRecruitmentCooldowns[settlementId];
                var cooldownPeriod = CampaignTime.Weeks(2); // 2 weeks cooldown
                var timeSinceLastRecruitment = lastRecruitmentTime.ElapsedHoursUntilNow;

                if (timeSinceLastRecruitment < cooldownPeriod.ToHours)
                {
                    var remainingHours = cooldownPeriod.ToHours - timeSinceLastRecruitment;
                    var remainingDays = (int)Math.Ceiling(remainingHours / 24.0);

                    var disableText = GameTexts.FindText("tor_goblin_recruitment_cooldown");
                    disableText.SetTextVariable("COOLDOWN_DAYS", remainingDays);
                    disableReason = disableText;
                    return true; // Still on cooldown
                }
            }
        }

        return false;
    }

    private bool CanStartGoblinRecruitment()
    {
        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
        {
            return false;
        }

        if (Settlement.CurrentSettlement?.Culture?.StringId != TORConstants.Cultures.GREENSKIN)
            return false;

        if (!Settlement.CurrentSettlement.IsTown)
        {
            return false;
        }

        // Check if player party has available space
        if (Hero.MainHero.PartyBelongedTo.MemberRoster.TotalManCount >= Hero.MainHero.PartyBelongedTo.Party.PartySizeLimit)
        {
            return false;
        }

        return true;
    }

    private void WaitingConsequence(MenuCallbackArgs args)
    {
        args.MenuContext.GameMenu.EndWait();
        args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
        // If we reach here, no goblins were found during the wait
        InformationManager.DisplayMessage(new InformationMessage(
            GameTexts.FindText("tor_goblin_recruitment_no_goblins").ToString()));
        GameMenu.SwitchToMenu("town_artisan");
    }

    private void GoblinSearchTick(MenuCallbackArgs args, CampaignTime dt)
    {
        var progress = args.MenuContext.GameMenu.Progress;
        var diff = _startWaitTime.ElapsedHoursUntilNow;

        if (diff > 0)
        {
            // Update progress bar
            args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(diff / _maximumWaitTime);

            // Check for goblins every hour with increasing chance
            if (args.MenuContext.GameMenu.Progress != progress)
            {
                // Base 15% chance per hour, increasing with time
                float goblinChance = 0.15f + (diff / _maximumWaitTime) * 0.35f; // 15% to 50% chance

                if (MBRandom.RandomFloat < goblinChance)
                {
                    // Goblins found! End the wait and show encounter menu
                    args.MenuContext.GameMenu.EndWait();
                    args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
                    GameMenu.SwitchToMenu("goblin_encounter");
                }
            }
        }
    }

    private void SetGoblinEncounter(MenuCallbackArgs args)
    {
        var text = GameTexts.FindText("tor_goblin_encounter_desc");
        MBTextManager.SetTextVariable("GOBLIN_ENCOUNTER_DESCRIPTION", text);
    }

    private void HandleGoblinEncounter()
    {
        float partyStrength = CalculatePartyStrength();

        // Add random multiplier to threshold (0.8 to 1.3)
        float randomThreshold = _basePartyStrengthThreshold * MBRandom.RandomFloatRanged(0.8f, 1.3f);

        if (partyStrength >= randomThreshold)
        {
            // Player is strong enough to bully goblins
            BullyGoblins();
        }
        else
        {
            // Goblins attack the player
            StartGoblinFight();
        }
    }

    private float CalculatePartyStrength()
    {
        float totalStrength = 0f;

        foreach (var element in Hero.MainHero.PartyBelongedTo.MemberRoster.GetTroopRoster())
        {
            var character = element.Character;
            var count = element.Number;

            // Calculate strength based on tier and count
            float troopStrength = (character.Tier + 1) * 10f; // Base strength per tier
            totalStrength += troopStrength * count;
        }

        return totalStrength;
    }

    private void BullyGoblins()
    {
        GameMenu.SwitchToMenu("goblin_bullying_success");
    }

    private void StartGoblinFight()
    {
        TroopRoster playerRoster = TroopRoster.CreateDummyTroopRoster();
        TroopRoster goblinRoster = TroopRoster.CreateDummyTroopRoster();

        // Add player party to roster
        foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
        {
            playerRoster.AddToCounts(hero.CharacterObject, 1);
        }

        // Add goblins with random multiplier (0.7 to 1.4)
        var goblinCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(_goblinTroopId);

        if (goblinCharacter == null)
        {
            return;
        }
        int randomGoblinCount = (int)(_baseGoblinGroupSize * MBRandom.RandomFloatRanged(0.7f, 1.4f));
        goblinRoster.AddToCounts(goblinCharacter, randomGoblinCount);

        Location location = LocationComplex.Current.GetLocationWithId("tavern");

        _isMissionStarted = true;
        var mission = TorMissionManager.OpenBrawlFightMission(location, playerRoster, goblinRoster,
            randomGoblinCount, OnGoblinFightEnd);
        mission.DoesMissionRequireCivilianEquipment = false;
    }

    private void OnGoblinFightEnd(BrawlMissionResult result)
    {
        _isMissionStarted = false;

        if (result.PlayerVictory)
        {
            GameMenu.SwitchToMenu("goblin_fight_victory");
        }
        else
        {
            // Player lost - face shame from orc boys
            GameMenu.SwitchToMenu("goblin_fight_defeat");
        }
    }

    private void AddGoblinsToParty(out int goblinsToAdd)
    {
        goblinsToAdd = 0;
        var goblinCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(_goblinTroopId);
        if (goblinCharacter != null)
        {
            // Base range 3-8, with random multiplier (0.8 to 1.5)
            int baseGoblins = MBRandom.RandomInt(3, 8);
            goblinsToAdd = (int)(baseGoblins * MBRandom.RandomFloatRanged(0.8f, 1.5f));
            Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCounts(goblinCharacter, goblinsToAdd);
        }
    }

    private void SetBullyingSuccess(MenuCallbackArgs args)
    {
        var goblinCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(_goblinTroopId);

        // Base range 5-12, with random multiplier (0.7 to 1.4)
        int baseGoblins = MBRandom.RandomInt(5, 12);
        int goblinsRecruited = (int)(baseGoblins * MBRandom.RandomFloatRanged(0.7f, 1.4f));

        if (goblinCharacter != null)
        {
            Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCounts(goblinCharacter, goblinsRecruited);
        }

        // Base teef reward with random multiplier (0.8 to 1.3)
        int baseTeef = MBRandom.RandomInt(10, 25);
        var teefReward = (int)(baseTeef * MBRandom.RandomFloatRanged(0.8f, 1.3f));
        Hero.MainHero.AddCultureSpecificCustomResource(teefReward);

        GameTexts.SetVariable("GOBLINS_RECRUITED", goblinsRecruited);
        GameTexts.SetVariable("TEEF_REWARD", teefReward);
        GameTexts.SetVariable("CR_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());

        var text = GameTexts.FindText("tor_goblin_bullying_success_desc");
        MBTextManager.SetTextVariable("GOBLIN_BULLYING_DESCRIPTION", text);
    }

    private void SetFightVictory(MenuCallbackArgs args)
    {
        AddGoblinsToParty(out int goblinsRecruited);

        // Base teef reward with random multiplier (0.7 to 1.2)
        int baseTeef = MBRandom.RandomInt(5, 15);
        var teefReward = (int)(baseTeef * MBRandom.RandomFloatRanged(0.7f, 1.2f));

        Hero.MainHero.AddCultureSpecificCustomResource(teefReward);

        GameTexts.SetVariable("GOBLINS_RECRUITED", goblinsRecruited);
        GameTexts.SetVariable("TEEF_REWARD", teefReward);
        GameTexts.SetVariable("CR_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());

        var text = GameTexts.FindText("tor_goblin_fight_victory_desc");
        MBTextManager.SetTextVariable("GOBLIN_FIGHT_VICTORY_DESCRIPTION", text);
    }

    private void SetFightDefeat(MenuCallbackArgs args)
    {
        // Player loses teef and faces shame from orc boys with random multipliers
        int baseTeefLoss = MBRandom.RandomInt(15, 30);
        var teefLoss = (int)(baseTeefLoss * MBRandom.RandomFloatRanged(0.8f, 1.4f));

        int baseRenownLoss = MBRandom.RandomInt(5, 15);
        var renownLoss = (int)(baseRenownLoss * MBRandom.RandomFloatRanged(0.7f, 1.3f));

        Hero.MainHero.AddCultureSpecificCustomResource(-teefLoss);
        Clan.PlayerClan.AddRenown(-renownLoss, true);

        // Some orc boys might leave the party in shame
        var orcBoysTroopId = "tor_gs_orc_boy";
        var orcBoysCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(orcBoysTroopId);

        if (orcBoysCharacter != null)
        {
            var currentOrcBoys = Hero.MainHero.PartyBelongedTo.MemberRoster.GetTroopCount(orcBoysCharacter);
            if (currentOrcBoys > 0)
            {
                var leavingOrcBoys = MBRandom.RandomInt(1, Math.Min(3, currentOrcBoys));
                Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCounts(orcBoysCharacter, -leavingOrcBoys);
                GameTexts.SetVariable("ORC_BOYS_LEFT", leavingOrcBoys);
            }
            else
            {
                GameTexts.SetVariable("ORC_BOYS_LEFT", 0);
            }
        }

        GameTexts.SetVariable("TEEF_LOSS", teefLoss);
        GameTexts.SetVariable("RENOWN_LOSS", renownLoss);
        GameTexts.SetVariable("CR_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());

        var text = GameTexts.FindText("tor_goblin_fight_defeat_desc");
        MBTextManager.SetTextVariable("GOBLIN_FIGHT_DEFEAT_DESCRIPTION", text);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_goblinRecruitmentCooldowns", ref _goblinRecruitmentCooldowns);
    }
}