using TaleWorlds.CampaignSystem;
﻿using System.Collections.Generic;
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

        public static float GetCannonCrewReloadBonus(List<string> attributes)
        {
            if (attributes.Contains(CharacterAttributes.CREW_3)) return 0.35f;
            if (attributes.Contains(CharacterAttributes.CREW_2)) return 0.20f;
            return 0f;
        }

        public float CalculateCannonReloadSpeed(float cannonReloadSpeed, Agent user , Agent reloader)
        {
            if (user == null || reloader == null) return cannonReloadSpeed;

            var explainedNumber = new ExplainedNumber(cannonReloadSpeed);
            foreach (var crewMember in new[] { user, reloader })
            {
                var reloadBonus = GetCannonCrewReloadBonus(crewMember.GetAttributes());
                if (reloadBonus > 0f)
                {
                    explainedNumber.AddFactor(-reloadBonus);
                }
            }

            return explainedNumber.ResultNumber;
        }
    }
}