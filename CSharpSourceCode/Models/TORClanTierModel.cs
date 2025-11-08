using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    class TORClanTierModel : DefaultClanTierModel
    {
        public override int GetPartyLimitForTier(Clan clan, int clanTierToCheck)
        {
            // Patch AI clans to have unlimited parties so that new lords aren't
            // spawned without a party.
            return Clan.PlayerClan.Equals(clan)
                ? base.GetPartyLimitForTier(clan, clanTierToCheck)
                : 9999;
        }
        
        
        public override int GetCompanionLimit(Clan clan)
        {
            int companionLimit = base.GetCompanionLimit(clan);

            if (clan != Clan.PlayerClan) return companionLimit;

            if (Hero.MainHero.HasAnyCareer())
            {
                var explainedNumber = new ExplainedNumber(); 
                CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref explainedNumber ,PassiveEffectType.CompanionLimit, false);
                companionLimit += (int)explainedNumber.ResultNumber;
            }

            return companionLimit;

        }
    }
}
