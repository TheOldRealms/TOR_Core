using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ScreenSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.WaaaghMeter;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomResourceBehavior;

public class WaaaghBehavior : CampaignBehaviorBase
{
    private const float DailyWaaaghDecay = 5f; // Daily passive Waaagh decrease
    private const float MaxWaaagh = 1000f; // Maximum Waaagh value
    private float _renownBefore;
    private float _initialCombatRatio;
    private WaaaghLevel _previousWaaaghLevel = WaaaghLevel.InternalFightin;
    private List<CharacterObject> _troops;

    public override void RegisterEvents()
    {
        CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, InitialCombatStrengthCalculation);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, CalculateWaaaghGainFromBattle);
        CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
        ScreenManager.OnPushScreen += ScreenManager_OnPushScreen;
    }


    private void ScreenManager_OnPushScreen(ScreenBase pushedScreen)
    {
        if (pushedScreen is not MapScreen mapScreen) return;
        // Only add WaaaghMeter for Greenskin players
        if (Hero.MainHero?.Culture?.StringId != TORConstants.Cultures.GREENSKIN) return;

  

        var mapView = mapScreen.GetMapView<WaaaghMeterMapView>();
        if (mapView == null)
        {
            mapScreen.AddMapView<WaaaghMeterMapView>();
        }
    }

    private void InitialCombatStrengthCalculation(IMission mission)
    {
        _renownBefore = Clan.PlayerClan?.Renown ?? 0f;

        if (Campaign.Current != null)
        {
            _initialCombatRatio = 0;
            var playerEvent = Campaign.Current.MainParty.MapEvent;

            if (playerEvent == null) return;

            playerEvent.GetStrengthsRelativeToParty(playerEvent.PlayerSide, out float playerStrength,
                out float enemyStrength);

            if (enemyStrength > 0)
            {
                _initialCombatRatio = playerStrength / enemyStrength;
            }
        }
    }

    private void CalculateWaaaghGainFromBattle(MapEvent mapEvent)
    {
        // Only apply to Greenskin players
        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN) return;
        if (mapEvent == null || !mapEvent.IsPlayerMapEvent) return;

        var playerWon = mapEvent.WinningSide == mapEvent.PlayerSide;
        if (!playerWon)
        {
            const int WAAAGH_LOSS_ON_DEFEAT = 200; // Player lost - decrease Waaagh
            Hero.MainHero.AddCustomResource("Waaagh", -WAAAGH_LOSS_ON_DEFEAT);
            UpdateWaaaghState();
            return;
        }

        // Calculate Waaagh gain based on battle difficulty
        var ratio = _initialCombatRatio;
        if (ratio <= 0f)
        {
            mapEvent.GetStrengthsRelativeToParty(mapEvent.PlayerSide,
                out var playerStrength, out var enemyStrength);
            if (enemyStrength > 0f) ratio = playerStrength / enemyStrength;
        }

        var renownAfter = Clan.PlayerClan?.Renown ?? 0f;
        var renownDelta = Math.Max(0.0, renownAfter - _renownBefore);

        // Scale based on battle difficulty (small battles give less)
        double scale;
        if (ratio > 2.0f) // Easy battle (player much stronger)
        {
            scale = 0.20; // Only 20% Waaagh gain
        }
        else if (ratio > 1.5f)
        {
            scale = 0.50; // 50% Waaagh gain
        }
        else if (ratio < 0.8f) // Hard battle (player weaker)
        {
            scale = 2.00; // Double Waaagh gain
        }
        else
        {
            scale = 1.0;
        }

        var delta = (int)Math.Round(renownDelta * 10.0 * scale, MidpointRounding.AwayFromZero);

        if (delta != 0)
        {
            Hero.MainHero.AddCustomResource("Waaagh", delta);
        }

        UpdateWaaaghState();
    }

    private void OnDailyTick()
    {
        // Only apply to Greenskin players
        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN) return;

        // Passive daily Waaagh decrease
        Hero.MainHero.AddCustomResource("Waaagh", -(int)DailyWaaaghDecay);

        // Daily Waaagh effects
        ApplyDailyWaaaghEffects();

        // Update Waaagh state after daily tick
        UpdateWaaaghState();
    }

    private void ApplyDailyWaaaghEffects()
    {
        if (Hero.MainHero.PartyBelongedTo == null) return;

        // Wargh1: Internal fighting causes wounded troops
        if (Hero.MainHero.HasAttribute("Wargh1"))
        {
            WoundTroopsFromInfighting(0.05f); // 5% chance per troop
        }
        // Wargh2: Smaller chance of troops getting wounded from squabbling
        else if (Hero.MainHero.HasAttribute("Wargh2"))
        {
            WoundTroopsFromInfighting(0.02f); // 2% chance per troop (smaller than Wargh1)
        }

        // Wargh3: Small chance of recruiting tier 1-3 Greenskin troops
        if (Hero.MainHero.HasAttribute("Wargh3"))
        {
            float recruitChance = 0.3f; // 30% chance
            if (MBRandom.RandomFloat < recruitChance)
            {
                RecruitRandomGreenskinTroop();
            }
        }
        // Wargh4: Big chance of recruiting tier 1-3 Greenskin troops
        else if (Hero.MainHero.HasAttribute("Wargh4"))
        {
            float recruitChance = 0.6f; // 60% chance
            if (MBRandom.RandomFloat < recruitChance)
            {
                RecruitRandomGreenskinTroop();
            }
        }
    }

    private void WoundTroopsFromInfighting(float woundChancePerTroop)
    {
        var roster = Hero.MainHero.PartyBelongedTo.MemberRoster;
        var list = roster.GetTroopRoster();

        // Internal fighting wounds troops
        for (var index = 0; index < list.Count; index++)
        {
            var element = list[index];
            int healthyTroops = element.Number - element.WoundedNumber;

            if (healthyTroops > 0)
            {
                // Chance for each healthy troop to get wounded
                int troopsToWound = 0;
                for (int i = 0; i < healthyTroops; i++)
                {
                    if (MBRandom.RandomFloat < woundChancePerTroop)
                    {
                        troopsToWound++;
                    }
                }

                if (troopsToWound > 0)
                {
                    roster.AddToCountsAtIndex(index, 0, troopsToWound, 0, true);
                }
            }
        }
    }

    private void RecruitRandomGreenskinTroop()
    {
        // Get all Greenskin troop types (tier 1-3)

        if (_troops == null)
        {
            _troops = CharacterObject.All.WhereQ(x =>
                x.Culture.StringId == TORConstants.Cultures.GREENSKIN && x.Tier >= 1 && x.Tier <= 3 && x.Occupation == Occupation.Soldier &&
                !x.IsHero).ToList();
        }

        if (_troops.Count == 0) return;

        var party = Hero.MainHero.PartyBelongedTo;
        if (party == null) return;

        var sizeLimit = Campaign.Current.Models.PartySizeLimitModel
            .GetPartyMemberSizeLimit(party.Party, false)
            .ResultNumber;

        var currentSize = party.MemberRoster.TotalManCount;

        var freeSlots = (int)sizeLimit - currentSize;
        if (freeSlots <= 0)
        {
            // exceed size -> skip
            return;
        }

        // pick random troop type
        var randomTroop = _troops.GetRandomElement();

        // 1 - 3 troops
        int troopCount = MBRandom.RandomInt(1, 4);
        troopCount = Math.Min(troopCount, freeSlots);

        if (troopCount <= 0)
            return;

        party.MemberRoster.AddToCounts(randomTroop, troopCount);
    }

    private void UpdateWaaaghState()
    {
        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN) return;

        var waaaghValue = Hero.MainHero.GetCustomResourceValue("Waaagh");

        // Cap Waaagh at maximum
        if (waaaghValue > MaxWaaagh)
        {
            Hero.MainHero.AddCustomResource("Waaagh", -(int)(waaaghValue - MaxWaaagh));
            waaaghValue = MaxWaaagh;
        }

        // Cap Waaagh at minimum (0)
        if (waaaghValue < 0)
        {
            Hero.MainHero.AddCustomResource("Waaagh", (int)(-waaaghValue));
            waaaghValue = 0;
        }

        var currentLevel = WaaaghHelper.GetWaaaghLevelForResource(waaaghValue);

        // Check if dropping from WAAAGH (level 4) to 'Ere We Go! (level 3) - apply steeper punishment
        if (_previousWaaaghLevel == WaaaghLevel.WAAAGH && currentLevel == WaaaghLevel.EreWeGo)
        {
            // Drop all the way down to Internal Fightin' (level 1)
            waaaghValue = 0;
            Hero.MainHero.AddCustomResource("Waaagh", -(int)Hero.MainHero.GetCustomResourceValue("Waaagh"));
            currentLevel = WaaaghLevel.InternalFightin;
        }

        // Update previous level for next check
        _previousWaaaghLevel = currentLevel;

        // Remove all Wargh state attributes
        Hero.MainHero.RemoveAttribute("Waaagh0");
        Hero.MainHero.RemoveAttribute("Waaagh1");
        Hero.MainHero.RemoveAttribute("Waaagh2");
        Hero.MainHero.RemoveAttribute("Waaagh3");

        // Add the appropriate attribute for the current state
        switch (currentLevel)
        {
            case WaaaghLevel.InternalFightin:
                Hero.MainHero.AddAttribute("Waaagh0");
                break;
            case WaaaghLevel.PettySquabblin:
                Hero.MainHero.AddAttribute("Waaagh1");
                break;
            case WaaaghLevel.EreWeGo:
                Hero.MainHero.AddAttribute("Waaagh2");
                break;
            case WaaaghLevel.WAAAGH:
                Hero.MainHero.AddAttribute("Waaagh3");
                break;
        }
    }
    private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
    {
        if (prisoner != Hero.MainHero)
            return;

        if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
            return;

        var currentWaaagh = Hero.MainHero.GetCustomResourceValue("Waaagh");
        if (currentWaaagh <= 0f)
        {
            UpdateWaaaghState();
            return;
        }
        Hero.MainHero.AddCustomResource("Waaagh", -(int)currentWaaagh);
        UpdateWaaaghState();
    }

    public override void SyncData(IDataStore dataStore)
    {
    }
}