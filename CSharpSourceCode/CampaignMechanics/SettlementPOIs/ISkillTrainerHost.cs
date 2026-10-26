using System.Collections.Generic;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// POI capability: hosts one or more companion-skill trainers.
    /// <see cref="SkillTrainerBehavior"/> discovers hosts on session launch
    /// and merges each one's configs into its runtime registry so the
    /// standard training dialog lines attach to the host's hub.
    ///
    /// <para>Constraints the host should respect (behavior does not enforce):
    /// one trainer teaches one skill (one <see cref="SkillTrainerConfig"/>
    /// per NPC), and ideally no two trainers at the same
    /// <see cref="SkillTrainerConfig.HubState"/> teach the same skill.
    /// Multi-trainer locations place each NPC separately via
    /// <see cref="SettlementPOI.PlaceResident"/> inside their
    /// <see cref="SettlementPOI.EnsurePlaced"/> override.</para>
    /// </summary>
    public interface ISkillTrainerHost
    {
        IEnumerable<SkillTrainerConfig> GetSkillTrainers();
    }
}
