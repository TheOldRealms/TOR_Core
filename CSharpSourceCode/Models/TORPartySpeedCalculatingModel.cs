using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
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
                AddCareerPassivesForPartySpeed(mobileParty, ref finalSpeed);
            
            if(mobileParty != null &&mobileParty!=MobileParty.MainParty&& mobileParty.Party.Culture.StringId == "khuzait")
            {
               
                result.AddFactor(0.5f, new TextObject("Vampire bonus"));
                if (Campaign.Current.IsNight)
                {
                    //finalSpeed.AddFactor(0.25f, new TextObject("{=fAxjyMt5}Vampire nighttime bonus"));
                    result.Add(0.25f, new TextObject("Vampire nighttime bonus"));
                }
            }

            if (mobileParty.Party != null && mobileParty == MobileParty.MainParty)
            {
                TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
                if (MobileParty.MainParty.LeaderHero == Hero.MainHero && MobileParty.MainParty.LeaderHero.IsVampire())
                {
                    if (Campaign.Current.IsNight)
                    { 
                        //finalSpeed.AddFactor(0.25f, new TextObject("{=fAxjyMt5}Vampire Nighttime Bonus"));
                       result.AddFactor(0.25f, new TextObject("Vampire Nighttime bonus"));
                    }

                    if (Campaign.Current.IsDay&&faceTerrainType != TerrainType.Forest && !MobileParty.MainParty.LeaderHero.HasCareerChoice("NewBloodPassive4") && !MobileParty.MainParty.LeaderHero.HasCareerChoice("ControlledHungerPassive1"))
                    {
                        result.AddFactor(-0.2f, new TextObject("Suffering from sun light"));
                    }
                }

                if (MobileParty.MainParty.LeaderHero == Hero.MainHero)
                {
                    var choices = Hero.MainHero.GetAllCareerChoices();

                    if (faceTerrainType == TerrainType.Forest&&choices.Contains("SurvivalistPassive3"))
                    {
                        var choice = TORCareerChoices.GetChoice("SurvivalistPassive3");
                        if (choice != null)
                        {
                            result.AddFactor(choice.GetPassiveValue(), new TextObject(choice.BelongsToGroup.ToString()));
                        }
                        
                    }
                 
                   
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
        
        private void AddCareerPassivesForPartySpeed(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if(party.LeaderHero==null)return;
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.PartyMovementSpeed);
            }
        }
    }
}
