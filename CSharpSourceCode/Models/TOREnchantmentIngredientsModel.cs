using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting;

public class TOREnchantmentIngredientsModel : GameModel
{

    public int GetCustomResourceValueForIngredient(TorTradeGoodType ingredient)
    {
        switch (ingredient)
        {
            case TorTradeGoodType.ArcaneScroll:
            case TorTradeGoodType.BlessedWater:
            case TorTradeGoodType.AmberCrystal:
            case TorTradeGoodType.WarpstoneDust:
            case TorTradeGoodType.GemStone:
                return 10;
            case TorTradeGoodType.DragonBlood:
                return 50;
            case TorTradeGoodType.Invalid:
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
        }
    }

    public float GetPercentOfIngredientForRecycleItems(TorTradeGoodType ingredient, ItemObject item)
    {
        var factor = 0.5f;
        if (item.IsCraftedByPlayer)
        {
            factor = 0.33f;
        }

        return factor;
    }



    public int CalculateResultAmount(float dropscore, TorTradeGoodType ingredient, float percentageOfLoot = 1)
    {
        float careerBonus = 1f;

        // Orc Shaman enchantment loot bonus
        if (Hero.MainHero.HasCareerChoice("BonesAnFirepitzPassive3"))
        {
            var choice = TORCareerChoices.GetChoice("BonesAnFirepitzPassive3");
            if (choice != null)
            {
                careerBonus += choice.GetPassiveValue(); // 0.25 for 25%
            }
        }

        return (int)(dropscore * GetDropAmplitude(ingredient) * RandomMultiplier(ingredient) * (percentageOfLoot / 100) * careerBonus);
    }


    /// <summary>
    /// Returns a drop factor for a defeated unit in a battle. The Resulting factor is later used for calculating the real dropChance in CalculateResultAmount()
    /// </summary>
    /// <param name="character">the unit or character in question</param>
    /// <param name="ingredient">the enchantment ingredient. If ever - never calculate here a drop chance for non enchantment ingredients - use a seperate model for it</param>
    /// <param name="mapEvent">optional - used as an additonal parameter for locations and such</param>
    /// <returns></returns>
    public float GetIngredientDropFactorForCharacter(CharacterObject character, TorTradeGoodType ingredient, MapEvent mapEvent = null)
    {
        float result = 0f;

        switch (ingredient)
        {
            //amber
            case TorTradeGoodType.AmberCrystal:
                {

                    var inAthelLoren = false;
                    if (mapEvent?.GetLeaderParty(BattleSideEnum.Defender)?.MobileParty?.InAthelLoren() == true)
                    {
                        inAthelLoren = true;
                    }


                    if (character.StringId.Contains("treeman"))
                    {
                        result += 5f;
                    }
                    if (character.IsTreeSpirit())//Sly : treemen are TreeSpirits so this grants 7.5 for them
                    {
                        result += 2.5f;
                    }
                    if (character.IsBeastman() || character.IsElf())
                    {
                        result++;
                    }

                    if (inAthelLoren)
                    {
                        result *= 2;
                    }
                    break;
                }
            case TorTradeGoodType.ArcaneScroll:

                //arcane scrolls
                if (character.Culture.StringId == TORConstants.Cultures.EMPIRE && character.IsHero && character.HeroObject.IsSpellCaster())
                {
                    result += 10;
                }

                if (character.IsCultist())
                {
                    result++;
                }

                if (character.IsVampire())
                {
                    result++;
                }

                if (character.IsHero && character.IsVampire() && !character.IsBloodDragon())
                    result += 5;

                if (character.IsVampire() && character.HasAttribute("Necrarch"))
                {
                    result += 10;
                }
                break;
            case TorTradeGoodType.BlessedWater:
                var inLaurelorn = false;
                if (mapEvent?.GetLeaderParty(BattleSideEnum.Defender)?.MobileParty?.InLaurelorn() == true)
                {
                    inLaurelorn = true;
                }
                if (character.IsReligiousUnit() && (character.Culture.StringId == TORConstants.Cultures.BRETONNIA || character.Culture.StringId == TORConstants.Cultures.EMPIRE))
                {
                    result++;
                }

                if (character.StringId == "tor_br_quest_knight")
                {
                    result += 7.5f;
                }
                if (character.IsHero && character.HeroObject.IsPriest())
                {
                    result += 5;
                }

                if (character.Culture.StringId == TORConstants.Cultures.EONIR)
                {
                    result += 0.5f;
                }

                if (inLaurelorn)
                {
                    result += 1;
                }
                break;
            case TorTradeGoodType.WarpstoneDust:
                var inSylvania = false;

                if (mapEvent != null)
                {
                    inSylvania = TORCommon.FindSettlementsAroundPosition(mapEvent.Position.ToVec2(), 150f,
                        x => x.StringId.Contains("SY")).Count > 0;
                }


                if (character.IsVampire())
                {
                    result += 2.5f;
                }

                if (character.Culture.StringId == TORConstants.Cultures.SYLVANIA ||
                    character.Culture.StringId == TORConstants.Cultures.MOUSILLON)
                {
                    result++;
                }
                if (character.IsCultist())
                {
                    result++;
                }

                if (inSylvania)
                {
                    result *= 2;
                }
                break;
            case TorTradeGoodType.DragonBlood:
                if (character.IsHero && character.HeroObject.IsLord)
                {
                    result += 5;
                }
                if (character.IsVampire() && character.StringId == "tor_bd_blooddragon_kastelan")
                {
                    result++;
                }
                break;
            case TorTradeGoodType.GemStone:
                if (character.Culture.StringId == TORConstants.Cultures.DAWI || character.Culture.StringId == TORConstants.Cultures.GREENSKIN)
                {
                    result += 1;

                    if (character.Tier > 4)
                    {
                        result++;
                    }
                }
                if (character.StringId.Contains("iron") && character.Culture.StringId == TORConstants.Cultures.DAWI)
                {
                    result += 2;  //Ironbreaker bonus
                }

                break;
            case TorTradeGoodType.Invalid:
                return 0f;
        }


        return result;

    }


    /// <summary>
    /// used as a decimator, in order to ensure that certain resources get rarer. For later: Could be on the long run coupled to game difficulty
    /// </summary>
    /// <param name="ingredient"> TOR enchantment ingredient</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"> The given ingredient is not an enchantment resource</exception>
    private float GetDropAmplitude(TorTradeGoodType ingredient)
    {
        float amplitude = 0.1f;

        switch (ingredient)
        {
            case TorTradeGoodType.ArcaneScroll:
                break;
            case TorTradeGoodType.BlessedWater:
                break;
            case TorTradeGoodType.DragonBlood:
                amplitude = 0.05f;
                break;
            case TorTradeGoodType.AmberCrystal:
                amplitude = 0.05f / 3f;
                break;
            case TorTradeGoodType.WarpstoneDust:
                amplitude = 0.05f / 3f; // this and amber will need further adjustments
                break;
            case TorTradeGoodType.GemStone:
                amplitude = 0.05f;
                break;
            case TorTradeGoodType.Invalid:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
        }

        return amplitude;
    }

    /// <summary>
    /// Random multiplier, that affects the result outcome with a random factor that could drastically increase or decreate the won amount of the resource. , should be not lower than 0.
    /// Could be later affected by certain luck properties or skills.
    /// </summary>
    /// <param name="ingredient"></param>
    /// <returns></returns>
    private float RandomMultiplier(TorTradeGoodType ingredient)
    {
        var randomMin = 0.2f;
        var randomMax = 2f;

        switch (ingredient)
        {
            case TorTradeGoodType.ArcaneScroll:
                randomMax = 1.5f;
                break;
            case TorTradeGoodType.BlessedWater:
                break;
            case TorTradeGoodType.DragonBlood:
                randomMax = 2;
                break;
            case TorTradeGoodType.AmberCrystal:
                randomMax = 1.5f;
                break;
            case TorTradeGoodType.WarpstoneDust:
                randomMax = 3f;
                break;
            case TorTradeGoodType.GemStone:
                randomMax = 1f;
                break;
            case TorTradeGoodType.Invalid:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
        }
        return MBRandom.RandomFloatRanged(randomMin, randomMax);
    }

}