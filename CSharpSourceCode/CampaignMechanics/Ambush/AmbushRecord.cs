using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Single event in the ambush history. Every cooldown and reveal
    /// query reads this record list — it is the one source of truth
    /// for "who was involved in what ambush, when, and how did it
    /// resolve." No per-party cooldown dicts; no per-player cooldown
    /// field; no reveal reference.
    ///
    /// Written from a single subscriber on <c>AmbushCampaignBehavior</c>
    /// that listens to <c>TORCampaignEvents.AmbushResolved</c>; mission
    /// end logics (Phase D / Phase F) and AI-on-AI resolution all fire
    /// that one event. The solo stealth-exit path (cooldown stamp with
    /// no strike attempted) writes a record directly with
    /// <see cref="Resolution"/> = <c>None</c> and <c>Victim = null</c>.
    /// Pruned on weekly tick, dropped when older than ~3 game-days.
    ///
    /// Downstream code (cooldown model, disorganize shield, instant-
    /// ambush query) reads via <see cref="Models.TORAmbushModel"/>
    /// classifiers rather than branching on the raw fields.
    /// </summary>
    public class AmbushRecord
    {
        [SaveableField(1)] public CampaignTime At;
        [SaveableField(2)] public MobileParty Ambusher;
        [SaveableField(3)] public MobileParty Victim;            // null for solo stealth-exit
        [SaveableField(4)] public AmbushResolution Resolution;
        [SaveableField(5)] public bool WasLoud;                  // player-ambusher mission side; drives loudness-extended cooldown
        [SaveableField(6)] public bool AmbusherLeaderDown;       // ambusher's hero went down during the engagement
        [SaveableField(7)] public bool VictimLeaderDown;         // victim's hero went down during the engagement

        /// <summary>Derived "did the victim beat the ambusher off?" used
        /// by cooldown + shield queries. True on <c>AmbusherDefeated</c>
        /// (victim held the field) and <c>VictimFled</c> (ambusher got
        /// no kills). <c>AmbusherLeaderDown</c> alone also counts since
        /// a hero-down pre-strike aborts the strike.</summary>
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

        public bool Involves(MobileParty party)
            => party != null && (Ambusher == party || Victim == party);
    }
}
