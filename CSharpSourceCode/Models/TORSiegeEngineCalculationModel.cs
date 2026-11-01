using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models
{
    public class TORSiegeEngineCalculationModel : DefaultSiegeEngineCalculationModel
    {

        public virtual float BaseCannonReloadSpeed => 20f;

        public static TORSiegeEngineCalculationModel Current => Campaign.Current?.Models?.GetSiegeEngineCalculationModel();

        public float CalculateCannonReloadSpeed(float cannonReloadSpeed, Agent user , Agent reloader)
        {
            if (user == null || reloader == null) return cannonReloadSpeed;

            var explainedNumber = new ExplainedNumber(cannonReloadSpeed);
            foreach (var crewMember in new[] { user, reloader })
            {
                if (crewMember.HasAttribute(CharacterAttributes.CREW_3))
                {
                    explainedNumber.AddFactor(-0.35f);
                }
                else if (crewMember.HasAttribute(CharacterAttributes.CREW_2))
                {
                    explainedNumber.AddFactor(-0.20f);
                }
            }

            return explainedNumber.ResultNumber;
        }
    }
}