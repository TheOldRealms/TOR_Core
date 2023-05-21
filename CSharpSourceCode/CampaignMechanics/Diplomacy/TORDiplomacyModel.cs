using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    public class TORDiplomacyModel:DefaultDiplomacyModel
    {
        public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingClan, out TextObject warReason)
        {
            float scoreOfDeclaringWar = base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out warReason);
            // Do your magic religion stuff here, but potentially biasing it towards Religious decisions quite heavily.
            return scoreOfDeclaringWar;
        }
    }
}
