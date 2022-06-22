using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics
{
    public class TORIssuesCampaignBehaviour : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.CanHaveQuestsOrIssuesEvent.AddNonSerializedListener(this, CheckIssue);
        }

        private void CheckIssue(Hero hero, ref bool result)
        {
            result = false;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
