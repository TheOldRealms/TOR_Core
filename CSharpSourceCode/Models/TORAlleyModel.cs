using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Models;

public class TORAlleyModel : DefaultAlleyModel
{
    // This is an extremely ugly fix, i would exploit the culture object for this. however setting in the regular culture  bandit bandit, bandit expert etc, are crashing at launch (though it shouldn't... ), so I just think this ugly hardcoded setting is the fastest.
    private CharacterObject _thug
    {
        get
        {
            var culture = Campaign.Current.MainParty.CurrentSettlement.Culture;

            if (culture != null)
            {
                if (culture.StringId == TORConstants.Cultures.EONIR || culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    return MBObjectManager.Instance.GetObject<CharacterObject>("tor_eo_thug");
                }
            }
            
            
            return MBObjectManager.Instance.GetObject<CharacterObject>("gangster_1");
        }
    }

    private CharacterObject _expertThug
    {
        get
        {
            var culture = Campaign.Current.MainParty.CurrentSettlement.Culture;
            if (culture != null)
            {
                if (culture.StringId == TORConstants.Cultures.EONIR || culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    return MBObjectManager.Instance.GetObject<CharacterObject>("tor_eo_expert_thug");
                }
            }
            return MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");
        }
    }

    private CharacterObject _masterThug
    {
        get
        {
            var culture = Campaign.Current.MainParty.CurrentSettlement.Culture;
            if (culture != null)
            {
                if (culture.StringId == TORConstants.Cultures.EONIR || culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    return MBObjectManager.Instance.GetObject<CharacterObject>("tor_eo_master_thug");
                }
            }
            return MBObjectManager.Instance.GetObject<CharacterObject>("gangster_2");
        }
    }
    
    public override TroopRoster GetTroopsOfAIOwnedAlley(Alley alley)
    {
        return this.GetTroopsOfAlleyInternal(alley);
    }
    
    
    public override TroopRoster GetTroopsOfAlleyForBattleMission(Alley alley)
    {
        TroopRoster troopsOfAlleyInternal = this.GetTroopsOfAlleyInternal(alley);
        TroopRoster dummyTroopRoster = TroopRoster.CreateDummyTroopRoster();
        foreach (TroopRosterElement troopRosterElement in (List<TroopRosterElement>) troopsOfAlleyInternal.GetTroopRoster())
            dummyTroopRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number * 2);
        return dummyTroopRoster;
    }
    
    private TroopRoster GetTroopsOfAlleyInternal(Alley alley)
    {
        TroopRoster dummyTroopRoster = TroopRoster.CreateDummyTroopRoster();
        Hero owner = alley.Owner;
        if ((double) owner.Power <= 100.0)
        {
            if ((double) owner.RandomValue > 0.5)
            {
                dummyTroopRoster.AddToCounts(this._thug, 3);
            }
            else
            {
                dummyTroopRoster.AddToCounts(this._thug, 2);
                dummyTroopRoster.AddToCounts(this._masterThug, 1);
            }
        }
        else if ((double) owner.Power <= 200.0)
        {
            if ((double) owner.RandomValue > 0.5)
            {
                dummyTroopRoster.AddToCounts(this._thug, 2);
                dummyTroopRoster.AddToCounts(this._expertThug, 1);
                dummyTroopRoster.AddToCounts(this._masterThug, 2);
            }
            else
            {
                dummyTroopRoster.AddToCounts(this._thug, 1);
                dummyTroopRoster.AddToCounts(this._expertThug, 2);
                dummyTroopRoster.AddToCounts(this._masterThug, 2);
            }
        }
        else if ((double) owner.Power <= 300.0)
        {
            if ((double) owner.RandomValue > 0.5)
            {
                dummyTroopRoster.AddToCounts(this._thug, 3);
                dummyTroopRoster.AddToCounts(this._expertThug, 2);
                dummyTroopRoster.AddToCounts(this._masterThug, 2);
            }
            else
            {
                dummyTroopRoster.AddToCounts(this._thug, 1);
                dummyTroopRoster.AddToCounts(this._expertThug, 3);
                dummyTroopRoster.AddToCounts(this._masterThug, 3);
            }
        }
        else if ((double) owner.RandomValue > 0.5)
        {
            dummyTroopRoster.AddToCounts(this._thug, 3);
            dummyTroopRoster.AddToCounts(this._expertThug, 3);
            dummyTroopRoster.AddToCounts(this._masterThug, 3);
        }
        else
        {
            dummyTroopRoster.AddToCounts(this._thug, 1);
            dummyTroopRoster.AddToCounts(this._expertThug, 4);
            dummyTroopRoster.AddToCounts(this._masterThug, 4);
        }
        return dummyTroopRoster;
    }
    
    public override TroopRoster GetTroopsToRecruitFromAlleyDependingOnAlleyRandom(
        Alley alley,
        float random)
    {
        TroopRoster dummyTroopRoster = TroopRoster.CreateDummyTroopRoster();
        if ((double) random >= 0.5)
            return dummyTroopRoster;
        Clan settlementFaction = alley.Settlement.Owner.Clan;
        if ((double) random > 0.30000001192092896)
        {
            dummyTroopRoster.AddToCounts(this._thug, 1);
            dummyTroopRoster.AddToCounts(settlementFaction.BasicTroop, 1);
        }
        else if ((double) random > 0.15000000596046448)
        {
            dummyTroopRoster.AddToCounts(this._thug, 2);
            dummyTroopRoster.AddToCounts(settlementFaction.BasicTroop, 1);
            dummyTroopRoster.AddToCounts(settlementFaction.BasicTroop.UpgradeTargets[0], 1);
        }
        else if ((double) random > 0.05000000074505806)
        {
            dummyTroopRoster.AddToCounts(this._thug, 3);
            dummyTroopRoster.AddToCounts(settlementFaction.BasicTroop, 2);
            dummyTroopRoster.AddToCounts(settlementFaction.BasicTroop.UpgradeTargets[0], 1);
        }
        else
        {
            dummyTroopRoster.AddToCounts(this._thug, 2);
            dummyTroopRoster.AddToCounts(settlementFaction.BasicTroop, 3);
            dummyTroopRoster.AddToCounts(settlementFaction.BasicTroop.UpgradeTargets[0], 3);
        }
        return dummyTroopRoster;
    }
    
}