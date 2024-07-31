using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Careers
{
    public class CareerDialogOptionsCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, RegisterDialogs);
        }

        public void RegisterDialogs(CampaignGameStarter obj)
        {
            var grailDamselEnvoyOfTheLadyPerkDialog = new GrailDamselEnvoyOfTheLadyPerkDialog(obj);
            CareerButtonDialogs.OnSessionLaunched(obj);
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}