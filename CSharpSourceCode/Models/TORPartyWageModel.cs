using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartyWageModel : DefaultPartyWageModel
    {

        public override int GetCharacterWage(CharacterObject character)
        {
            if (character.IsUndead()) return 0;

            switch (character.Tier)
            {
                case 0:
                    return 1;
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                    return 5;
                case 4:
                    return 8;
                case 5:
                    return 12;
                case 6:
                    return 17;
                case 7:
                    return 23;
                case 8:
                    return 30;
                default:
                    return 40;
            }
        }

        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, bool includeDescriptions = false)
        { 
            var value = base.GetTotalWage(mobileParty, includeDescriptions);
            
            for (int index = 0; index < mobileParty.MemberRoster.Count; ++index)
            {
                TroopRosterElement elementCopyAtIndex = mobileParty.MemberRoster.GetElementCopyAtIndex(index);
                if (mobileParty.IsMainParty)
                {
                    if (mobileParty.LeaderHero.HasAnyCareer())
                    {
                        var careerID = mobileParty.LeaderHero.GetCareer().StringId;
                        value = AddCareerSpecifWagePerks(value, mobileParty.LeaderHero, elementCopyAtIndex);
                    }
                }
                
                
            }

            return value;
        }

        private ExplainedNumber AddCareerSpecifWagePerks(ExplainedNumber resultValue, Hero hero, TroopRosterElement unit)
        {
            var choices = hero.GetAllCareerChoices();

            if (choices.Contains("SigmarsProclaimerPassive2"))
            {
                if (unit.Character.IsSoldier)
                {
                    var choice = TORCareerChoices.GetChoice("SigmarsProclaimerPassive2");
                    var includeRegularTroops = choices.Contains("ArchLectorPassive4");
                    var value = CalculateSigmarsProclaimerPerk(unit, includeRegularTroops, choice);
                    resultValue.Add(value, choice.BelongsToGroup.Name);
                }
            }
            
            if (choices.Contains("DuelistPassive3"))
            {
                if (!unit.Character.IsMounted)
                {
                    TextObject text;
                    var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, "DuelistPassive3", out text);
                    resultValue.Add(value,text);
                }
            }
            
            if (choices.Contains("CommanderPassive1"))
            {
                if (unit.Character.Tier>4&&!unit.Character.IsHero)
                {
                    TextObject text;
                    var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, "CommanderPassive1", out text);
                    resultValue.Add(value,text);
                }
            }
            
            if (choices.Contains("LordlyPassive3"))
            {
                if (unit.Character.IsVampire()&&unit.Character != Hero.MainHero.CharacterObject)
                {
                    TextObject text;
                    var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, "LordlyPassive3", out text);
                    resultValue.Add(value,text);
                }
            }
            
            if (choices.Contains("InspirationOfTheLadyPassive3"))
            {
                if (unit.Character.IsKnightUnit())
                {
                    TextObject text;
                    var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, "LordlyPassive3", out text);
                    resultValue.Add(value,text);
                }
            }
            
            if (choices.Contains("AvatarOfDeathPassive2"))
            {
                if (unit.Character.IsVampire()&&unit.Character != Hero.MainHero.CharacterObject)
                {
                    TextObject text;
                    var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, "AvatarOfDeathPassive2", out text);
                    resultValue.Add(value,text);
                }
            }
            
            if (choices.Contains("MonsterSlayerPassive4"))
            {
                if (unit.Character != Hero.MainHero.CharacterObject&&!unit.Character.IsKnightUnit()&&unit.Character.Culture.StringId=="vlandia")
                {
                    TextObject text;
                    var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, "MonsterSlayerPassive4", out text);
                    resultValue.Add(value,text);
                }
            }
            
            if (choices.Contains("MasterHorsemanPassive4"))
            {
                if (unit.Character != Hero.MainHero.CharacterObject&&unit.Character.IsKnightUnit())
                {
                    TextObject text;
                    var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, "MasterHorsemanPassive4", out text);
                    resultValue.Add(value,text);
                }
          
            }


            return resultValue;
        }



        private float CalculateSigmarsProclaimerPerk(TroopRosterElement unit, bool includeRegularTroops, CareerChoiceObject choice)
        {
            if (!unit.Character.UnitBelongsToCult("cult_of_sigmar"))
            {
                if (!includeRegularTroops)
                {
                    return 0f;
                }
            }
            if (choice?.Passive == null) return 0;
            var effectMagnitude = choice.Passive.EffectMagnitude;
            if (choice.Passive.InterpretAsPercentage) effectMagnitude /= 100;
            var value = -(unit.Character.TroopWage*unit.Number) * (effectMagnitude);
            return value;
        }
        

    }
}
