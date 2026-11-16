using System.Collections.Generic;
using TOR_Core.CampaignMechanics.CompanionRecruitment;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// POI capability: offers one or more wanderer templates for recruitment
    /// via a <see cref="CompanionRecruitmentBehavior"/>-registered dialog line.
    /// Discovery runs at session launch; each yielded
    /// <see cref="CompanionRecruitmentConfig"/> contributes an "ask for a
    /// recruit" player line attached to the host's hub state, scoped to the
    /// host NPC by template id.
    /// </summary>
    public interface ICompanionRecruitmentHost
    {
        IEnumerable<CompanionRecruitmentConfig> GetCompanionRecruits();
    }
}
