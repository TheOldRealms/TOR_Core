using System.Linq;
using Helpers;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPartySpeedCalculatingModel : DefaultPartySpeedCalculatingModel
    {
        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
        {
            var result = base.CalculateFinalSpeed(mobileParty, finalSpeed);
            if (mobileParty == MobileParty.MainParty)
                AddCareerPassivesForPartySpeed(mobileParty, ref result);

            if (mobileParty != null && mobileParty != MobileParty.MainParty && mobileParty.IsLordParty && mobileParty.LeaderHero != null)
            {
                if (mobileParty.LeaderHero.IsVampire())
                {
                    result.AddFactor(0.5f, new TextObject("Vampire bonus"));
                    if (Campaign.Current.IsNight)
                    {
                        result.Add(0.25f, new TextObject("Vampire nighttime bonus"));
                    }
                }
                
            }

            if (mobileParty.Party != null && mobileParty == MobileParty.MainParty)
            {
                TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
                if (MobileParty.MainParty.LeaderHero == Hero.MainHero && MobileParty.MainParty.LeaderHero.IsVampire())
                {
                    if (Campaign.Current.IsNight)
                    {
                        result.AddFactor(0.25f, new TextObject("Vampire Nighttime bonus"));
                    }

                    float daytime = CampaignTime.Hours(Campaign.CurrentTime).CurrentHourInDay;
                    var isNight = daytime > 18 || daytime < 6;

                    if (!isNight && faceTerrainType != TerrainType.Forest && !MobileParty.MainParty.LeaderHero.HasCareerChoice("NewBloodPassive4") && !MobileParty.MainParty.LeaderHero.HasCareerChoice("ControlledHungerPassive1"))
                    {
                        result.AddFactor(-0.2f, new TextObject("Suffering from sun light"));
                    }
                }
                
                if (mobileParty.HasBlessing("cult_of_taal"))
                {

                    if (faceTerrainType == TerrainType.Forest)
                    {
                        result.AddFactor(0.1f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_taal"));
                    }
                }

                if (MobileParty.MainParty.LeaderHero == Hero.MainHero)
                {
                    if (Hero.MainHero.GetCustomResourceValue("DarkEnergy")==0 && Hero.MainHero.GetCalculatedCustomResourceUpkeep("DarkEnergy") <=-100)
                    {
                        result.AddFactor(-0.9f,new TextObject("Burden of Dark Energy Costs is too high!") );
                    }

                    var positionEvent = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(mobileParty.Position2D);

                    if (positionEvent == MapWeatherModel.WeatherEvent.Snowy || positionEvent == MapWeatherModel.WeatherEvent.Blizzard)
                    {
                    
                            CareerChoiceObject choice = null;
                            if (Hero.MainHero.HasCareerChoice("FrostsBitePassive3"))
                            {
                                choice = TORCareerChoices.GetChoice("FrostBitePassive3");
                            }
                            else if(Hero.MainHero.HasCareerChoice("PathfinderPassive3"))
                            { 
                                choice = TORCareerChoices.GetChoice("PathfinderPassive3");
                            }

                            if (choice == null) return result;
                            var snowText = new TextObject("{=vLjgcdgB}Snow");
                            
                            var snowEffect = result.GetLines().FirstOrDefaultQ(item => snowText.Value.Contains(item.name));
                            if (snowEffect.name !=null)
                            {
                                result.Add(-snowEffect.number, choice.BelongsToGroup.Name);
                            }
                    }
                   
                   
                }
            }

            

            return result;
        }

        private void AddCareerPassivesForPartySpeed(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (party.LeaderHero == null) return;
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.PartyMovementSpeed, false);
            }
        }
    }
}
