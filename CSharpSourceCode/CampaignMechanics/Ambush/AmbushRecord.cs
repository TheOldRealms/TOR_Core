using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// One entry in the ambush history. Source of truth for all cooldown and reveal queries.
    /// </summary>
    public class AmbushRecord
    {
        [SaveableField(1)] public CampaignTime At;
        [SaveableField(2)] public MobileParty Ambusher;
        [SaveableField(3)] public MobileParty Victim;            // null for solo stealth-exit
        [SaveableField(4)] public AmbushResolution Resolution;
        [SaveableField(5)] public bool WasLoud;
        [SaveableField(6)] public bool AmbusherLeaderDown;
        [SaveableField(7)] public bool VictimLeaderDown;

        /// <summary>
        /// True when the victim beat the ambusher off (used by cooldown + shield queries).
        /// </summary>
        public bool AmbusherRepelled
            => Resolution == AmbushResolution.AmbusherDefeated
            || Resolution == AmbushResolution.VictimFled
            || AmbusherLeaderDown;

        public AmbushRecord() { }

        public AmbushRecord(CampaignTime at, MobileParty ambusher, MobileParty victim,
                             AmbushResolution resolution,
                             bool wasLoud = false,
                             bool ambusherLeaderDown = false,
                             bool victimLeaderDown = false)
        {
            At = at;
            Ambusher = ambusher;
            Victim = victim;
            Resolution = resolution;
            WasLoud = wasLoud;
            AmbusherLeaderDown = ambusherLeaderDown;
            VictimLeaderDown = victimLeaderDown;
        }

        /// <summary>
        /// True if the party is the ambusher or victim of this record.
        /// </summary>
        public bool Involves(MobileParty party)
            => party != null && (Ambusher == party || Victim == party);
    }
}
