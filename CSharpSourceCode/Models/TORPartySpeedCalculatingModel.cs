using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartySpeedCalculatingModel : DefaultPartySpeedCalculatingModel
    {
        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
        {
            var result = base.CalculateFinalSpeed(mobileParty, finalSpeed);
            if(mobileParty == MobileParty.MainParty)
                AddCareerPassivesForTroopMorale(mobileParty, ref finalSpeed);
            
            if(mobileParty != null && mobileParty.Party.Culture.StringId == "khuzait")
            {
                result.Add(0.5f, new TextObject("Vampire bonus"));
                if (Campaign.Current.IsNight)
                {
                    result.Add(0.25f, new TextObject("Vampire nighttime bonus"));
                }
            }
            
            if (mobileParty.HasBlessing("cult_of_taal"))
            {
                TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
                if (faceTerrainType == TerrainType.Forest)
                {
                    result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_taal"));
                }
            }

            
            return result;
        }
        
        private void AddCareerPassivesForTroopMorale(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if(party.LeaderHero==null)return;
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.PartyMovementSpeed);
            }
        }
    }
}
