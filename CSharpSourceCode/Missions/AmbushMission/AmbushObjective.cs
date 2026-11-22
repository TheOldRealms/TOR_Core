using System;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Mutable MissionObjective for the ambush HUD. Vanilla's own
    /// <c>GenericMissionObjective</c> is internal, so we can't reach its
    /// Name/Description setters from the mod. We only need live
    /// Description + Progress; Name stays constant per objective
    /// instance. Rebuild the Description TextObject when phase/state
    /// transitions make the text stale; <c>MissionObjective.Tick</c>
    /// compares cached vs current reference and fires <c>OnUpdated</c>
    /// so the vanilla VM refreshes without extra wiring.
    /// </summary>
    public class AmbushObjective : MissionObjective
    {
        private readonly string _uniqueId;
        private readonly TextObject _name;
        private TextObject _description;
        private Func<MissionObjectiveProgressInfo> _progressCallback;

        public override string UniqueId => _uniqueId;
        public override TextObject Name => _name;
        public override TextObject Description => _description;

        public AmbushObjective(Mission mission, string id, TextObject name, TextObject description)
            : base(mission)
        {
            _uniqueId = id;
            _name = name;
            _description = description;
        }

        /// <summary>
        /// Replace the description TextObject. The reference change is
        /// what <c>MissionObjective.CheckNameUpdates</c> uses to fire
        /// <c>OnUpdated</c>, so pass a freshly built TextObject rather
        /// than mutating the existing one with SetTextVariable.
        /// </summary>
        public void SetDescription(TextObject description) => _description = description;

        public void SetProgressCallback(Func<MissionObjectiveProgressInfo> callback)
            => _progressCallback = callback;

        public override MissionObjectiveProgressInfo GetCurrentProgress()
            => _progressCallback?.Invoke() ?? default;
    }
}
