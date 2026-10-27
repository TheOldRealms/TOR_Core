using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Single event in the ambush history. Every cooldown and reveal
    /// query reads this record list — it is the one source of truth
    /// for "who was involved in what ambush, when, and did it get
    /// repelled". No per-party cooldown dicts; no per-player cooldown
    /// field; no reveal reference.
    ///
    /// Written in four places on <c>AiAmbushBehavior</c> /
    /// <c>AmbushCampaignBehavior</c>: stealth exit (no strike), AI
    /// ambushing the player, AI vs AI skirmish, player ambushing AI.
    /// Pruned on weekly tick, dropped when older than ~3 game-days.
    /// </summary>
    public class AmbushRecord
    {
        [SaveableField(1)] public CampaignTime At;
        [SaveableField(2)] public MobileParty Ambusher;
        [SaveableField(3)] public MobileParty Victim;        // null when the record is a solo stealth-exit
        [SaveableField(4)] public bool AmbusherRepelled;     // true when victim beat them off
        [SaveableField(5)] public bool WasLoud;              // player-ambusher side only; drives the model's loudness-extended cooldown

        public AmbushRecord() { }

        public AmbushRecord(CampaignTime at, MobileParty ambusher, MobileParty victim, bool repelled, bool wasLoud)
        {
            At = at;
            Ambusher = ambusher;
            Victim = victim;
            AmbusherRepelled = repelled;
            WasLoud = wasLoud;
        }

        public bool Involves(MobileParty party)
            => party != null && (Ambusher == party || Victim == party);
    }
}
