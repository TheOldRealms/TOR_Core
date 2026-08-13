using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;

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
                    result.AddFactor(0.1f, TORTextHelper.GetTextObject("tor_vampire_bonus", "Vampire bonus")); //Sly : looks relatively close to other cultures with vampires having a slight edge during the day, and a larger bonus at night
                    if (Campaign.Current.IsNight)
                    {
                        result.Add(0.25f, TORTextHelper.GetTextObject("tor_vampire_nighttime_bonus", "Vampire nighttime bonus"));
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
            var partySpeedTraits = heroes.SelectQ(hero => hero.CharacterObject.GetCharacterEquipment())
                .SelectMany(equipment => equipment.SelectQ(item => item.GetTraits())
                    .SelectMany(traits => traits.WhereQ(trait => trait != null && trait.StatsTuple?.StatType == ItemTraitStatType.PartySpeed)));

            var maxPartySpeedTrait = partySpeedTraits.OrderByDescending(t => t.StatsTuple.Value).FirstOrDefault();
            if (maxPartySpeedTrait != null)
            {
                result.AddFactor(maxPartySpeedTrait.StatsTuple.Value / 100f, GameTexts.FindText("tor_generic_enchantedEquipment"));
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

                        result.Add(bonus, TORTextHelper.GetTextObject("tor_taal_seal", "Taal Seal"));
                    }
                }
            }


            if (leaderHero.GetCustomResourceValue("DarkEnergy") == 0 && leaderHero.GetCalculatedCustomResourceUpkeep("DarkEnergy") <= -100)
            {
                result.AddFactor(-0.9f, TORTextHelper.GetTextObject("tor_dark_energy_burden", "Burden of Dark Energy Costs is too high!"));
            }

            if (leaderHero.IsVampire())//player vamp
            {
                if (Campaign.Current.IsNight)
                {
                    //night is always beneficial
                    result.AddFactor(0.50f, TORTextHelper.GetTextObject("tor_vampire_nighttime_bonus", "Vampire Nighttime bonus"));
                }
                else if (faceTerrainType == TerrainType.Forest || leaderHero.HasCareerChoice("NewBloodPassive4") || leaderHero.HasCareerChoice("ControlledHungerPassive1"))
                {
                    //daytime penalties avoided in some circumstances
                }
                else
                {
                    //daytime penalty is otherwise the default
                    result.AddFactor(-0.25f, TORTextHelper.GetTextObject("tor_suffering_sunlight", "Suffering from sun light"));
                }
            }
            

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
                var snowText = TORTextHelper.GetTextObject("tor_snow", "Snow");

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