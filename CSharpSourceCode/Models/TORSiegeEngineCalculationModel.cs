using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORSiegeEngineCalculationModel : DefaultSiegeEngineCalculationModel
    {

        public static readonly float BaseCannonReloadSpeed = 20f;

        public static TORSiegeEngineCalculationModel Current => Campaign.Current?.Models?.GetSiegeEngineCalculationModel();

        public float CalculateCannonReloadSpeed(float cannonReloadSpeed, Agent user , Agent reloader)
        {
            var explainedNumber = new ExplainedNumber(cannonReloadSpeed);
            if (user == null) return cannonReloadSpeed;
            else
            {
                if(user.HasAttribute("CrewII")) // mid tier Cannoneer troops
                {
                    explainedNumber.AddFactor(-0.15f);
                }
                else if(user.HasAttribute("CrewIII"))   //Elite Cannoneer units can reload very fast
                {
                    explainedNumber.AddFactor(-0.30f);
                }
            }
            if (reloader == null) return cannonReloadSpeed;
            if(reloader.HasAttribute("CrewII")) // mid tier Cannoneer troops
            {
                explainedNumber.AddFactor(-0.20f);
            }
            else if(reloader.HasAttribute("CrewIII"))   //Elite Cannoneer units can reload very fast
            {
                explainedNumber.AddFactor(-0.35f);
            }

            return explainedNumber.ResultNumber;
        }
    }
}