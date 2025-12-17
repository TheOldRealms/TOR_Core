using Helpers;
using SandBox;
using System.Collections.Generic;
using System.Linq;
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
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPartySpeedCalculatingModel : DefaultPartySpeedCalculatingModel
    {
        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
        {
            var result = base.CalculateFinalSpeed(mobileParty, finalSpeed);

            if (!mobileParty.IsLordParty) return result;


            var leaderHero = mobileParty?.LeaderHero;
            if (leaderHero == null) return result;



            if (mobileParty != MobileParty.MainParty)
            {
                if (leaderHero.IsVampire())
                {
                    result.AddFactor(0.1f, new TextObject("Vampire bonus")); //Sly : looks relatively close to other cultures with vampires having a slight edge during the day, and a larger bonus at night
                    if (Campaign.Current.IsNight)
                    {
                        result.Add(0.25f, new TextObject("Vampire nighttime bonus"));
                    }
                }
            }

            if (mobileParty != MobileParty.MainParty) return result;


            TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
            if (mobileParty.HasBlessing("cult_of_taal"))
            {
                if (faceTerrainType == TerrainType.Forest)
                {
                    result.AddFactor(0.1f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_taal"));
                }
            }

            AddCareerPassivesForPartySpeed(mobileParty, ref result);

            var heroes = mobileParty.GetMemberHeroes();
            foreach (var trait in heroes.SelectQ(hero => hero.CharacterObject.GetCharacterEquipment()).
                            SelectMany(equipment => equipment.SelectQ(item => item.GetTraits()).
                                SelectMany(traits => traits.WhereQ(trait => trait != null && trait.StatsTuple?.StatType == ItemTraitStatType.PartySpeed))))
            {
                result.AddFactor(trait.StatsTuple.Value / 100f, GameTexts.FindText("tor_generic_enchantedEquipment"));
            }

            if (leaderHero.HasCareer(TORCareers.KnightOldWorld))
            {
                var taal = ReligionObject.All.FirstOrDefaultQ(x => x.StringId == "cult_of_taal");
                if (Hero.MainHero.GetDevotionLevelForReligion(taal) >= DevotionLevel.Fanatic)
                {
                    var info = ExtendedInfoManager.Instance.GetPartyInfoFor(mobileParty.StringId);
                    if (info != null)
                    {
                        var troopAttributes = info.TroopAttributes;

                        var bonus = 0f;
                        foreach (var troop in mobileParty.MemberRoster.GetTroopRoster())
                        {
                            if (troopAttributes.TryGetValue(troop.Character.StringId, out List<string> elementAttributes))
                            {
                                if (elementAttributes.Contains("TaalSeal3"))
                                {
                                    bonus += 0.02f * troop.Number;
                                }
                            }
                        }

                        result.Add(bonus, new TextObject("Taal Seal"));
                    }
                }
            }


            if (leaderHero.GetCustomResourceValue("DarkEnergy") == 0 && leaderHero.GetCalculatedCustomResourceUpkeep("DarkEnergy") <= -100)
            {
                result.AddFactor(-0.9f, new TextObject("Burden of Dark Energy Costs is too high!"));
            }

            if (leaderHero.IsVampire())//player vamp
            {
                if (Campaign.Current.IsNight || faceTerrainType == TerrainType.Forest || leaderHero.HasCareerChoice("NewBloodPassive4") || leaderHero.HasCareerChoice("ControlledHungerPassive1"))
                {
                    result.AddFactor(0.50f, new TextObject("Vampire Nighttime bonus"));
                }
                else
                {
                    result.AddFactor(-0.25f, new TextObject("Suffering from sun light"));
                }
            }
            /*This requires editing the base speed calculation because it implements nighttime hours which are very restrictive and causes a mismatch between our night bonus and their night penalty. Not worth pursuing atm until more willing to understand and modify the speed calculation in more detail.
            //if (leaderHero.IsVampire()) //is this about vampire, or undead in general? would a necro player be supposed to take the penalty?
            //{
            //    int timeOfDay = CampaignTime.Now.GetHourOfDay;
            //    var isNight = timeOfDay >= 22 || timeOfDay < 6;

            //    if (!isNight && (leaderHero != Hero.MainHero || (faceTerrainType != TerrainType.Forest && !leaderHero.HasCareerChoice("NewBloodPassive4") && !leaderHero.HasCareerChoice("ControlledHungerPassive1"))))
            //    {
            //        result.AddFactor(-0.25f, new TextObject("Suffering from sun light"));
            //    }
            //    else
            //    {
            //        result.AddFactor(0.5f, new TextObject("Vampire Nighttime bonus"));
            //    }
            }*/

            var positionEvent = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(mobileParty.Position.ToVec2());

            if (positionEvent == MapWeatherModel.WeatherEvent.Snowy || positionEvent == MapWeatherModel.WeatherEvent.Blizzard) //Sly : order_without_power mentioned that TerrainType.Snow is unused and that snow is entirely within the weather model having rain in a location during the winter, at a latitude that supports snow.
            {
                CareerChoiceObject choice = null;
                //Sly : probably fixed ulric perk not applying, but snow never spawned to test - this is most likely due to an issue with weather rendering/modeling as some users reported receiving snow movement penalties in southern athel loren despite not standing on any snow; someone even managed it mid-summer so the weather model is doing... things.
                if (leaderHero.HasCareerChoice("FrostsBitePassive3"))
                {
                    choice = TORCareerChoices.GetChoice("FrostsBitePassive3");
                }
                else if (leaderHero.HasCareerChoice("PathfinderPassive3"))
                {
                    choice = TORCareerChoices.GetChoice("PathfinderPassive3");
                }

                if (choice == null) return result;
                var snowText = new TextObject("{=vLjgcdgB}Snow");

                var snowEffect = result.GetLines().FirstOrDefaultQ(item => snowText.Value.Contains(item.name));
                if (snowEffect.name != null)
                {
                    result.Add(-snowEffect.number, choice.BelongsToGroup.Name);
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