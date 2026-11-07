using System;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Thrown by <see cref="AmbushMissionController"/> when
    /// <c>DoCustomAmbusherSpawn</c> or <c>DoCustomVictimSpawn</c> runs
    /// without a shared <c>PartyGroupTroopSupplier</c> instance. The
    /// mission opener (<c>OpenAmbushMission</c> / <c>OpenAmbushedMission</c>
    /// in <c>TORMissionManager</c>) is responsible for constructing the
    /// supplier once and passing the same instance to both
    /// <c>DefaultBattleMissionAgentSpawnLogic</c> and the controller;
    /// forgetting that handoff is how Phase F's (0, 0, 0) loading-screen
    /// hang slipped past code review (silent skip, nothing to log).
    ///
    /// Caught in <see cref="AmbushMissionController.AfterStart"/> for a
    /// contextual log line (phase state, which supplier is missing); the
    /// catch rethrows so the mission still fails to load rather than
    /// silently running broken.
    /// </summary>
    public class AmbushSupplierMissingException : Exception
    {
        public AmbushSupplierMissingException(string message) : base(message) { }
    }
}
