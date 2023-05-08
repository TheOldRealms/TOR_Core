using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public class CursedSiteComponent : TORBaseSettlementComponent
    {
        private int _wardHours = 0;
        public int WardHours
        {
            get { return _wardHours; }
            set 
            { 
                _wardHours = value;
                if (_wardHours == 0) IsActive = true;
            }
        }
        public void HourlyTick() => WardHours = Math.Max(0, WardHours - 1);
    }

    public class ShrineComponent : TORBaseSettlementComponent { }

    public class ChaosPortalComponent : BaseRaiderSpawnerComponent
    {
        public override string BattleSceneName => "TOR_chaos_portal_001_forceatmo";

        public override string RewardItemId => "tor_empire_weapon_sword_runefang_001";

        public override void SpawnNewParty()
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_patrol");
            Clan chaosClan = Clan.FindFirst(x => x.StringId == "chaos_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var chaosRaidingParty = RaidingPartyComponent.CreateRaidingParty("chaos_clan_1_party_" + RaidingPartyCount + 1, Settlement, "Chaos Raiders", template, chaosClan, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(chaosRaidingParty, find);
            ((RaidingPartyComponent)chaosRaidingParty.PartyComponent).Target = find;
        }
    }

    public class HerdStoneComponent : BaseRaiderSpawnerComponent
    {
        public override string BattleSceneName => "TOR_chaos_portal_001_forceatmo";

        public override string RewardItemId => "tor_empire_weapon_sword_runefang_001";

        public override void SpawnNewParty()
        {
            PartyTemplateObject template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("ungor_party");
            Clan beastmenClan = Clan.FindFirst(x => x.StringId == "beastmen_clan_1");
            var find = TORCommon.FindSettlementsAroundPosition(Settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var raidingParty = RaidingPartyComponent.CreateRaidingParty("beastmen_clan_1_party_" + RaidingPartyCount + 1, Settlement, "Ungor Raiders", template, beastmenClan, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(raidingParty, find);
            ((RaidingPartyComponent)raidingParty.PartyComponent).Target = find;
        }
    }
}
