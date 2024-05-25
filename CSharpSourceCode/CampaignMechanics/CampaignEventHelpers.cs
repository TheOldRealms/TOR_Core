using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics
{
    public static class CampaignEventHelpers
    {
        public static float CalculateCombatStrength()
        {
            float combatstrength = 0;
            if (Campaign.Current != null)
            {
                var playerEvent = Campaign.Current.MainParty.MapEvent;
                
                if(playerEvent==null) return 0;

                playerEvent.GetStrengthsRelativeToParty(playerEvent.PlayerSide, out float playerStrength, out float enemyStrength);

                if (enemyStrength > 0)
                {
                    combatstrength = playerStrength / enemyStrength;
                }
            }

            return combatstrength;
        }
    }
}