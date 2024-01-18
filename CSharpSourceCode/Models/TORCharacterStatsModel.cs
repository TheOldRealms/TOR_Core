﻿using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.Models
{
    public class TORCharacterStatsModel : DefaultCharacterStatsModel
    {
        public override int MaxCharacterTier => 9;

        public override ExplainedNumber MaxHitpoints(CharacterObject character, bool includeDescriptions = false)
        {
            var number = base.MaxHitpoints(character, includeDescriptions);
            number = CalculateHitPoints(number, character);
            return number;
        }

        private ExplainedNumber CalculateHitPoints(ExplainedNumber number, CharacterObject character)
        {
            if (character.Race == FaceGen.GetRaceOrDefault("large_humanoid_monster"))
            {
                number.Add(1000f, new TextObject("Large Monster"));
            }
            if (character.IsHero)
            {
                return CalculateHeroHealth(number, character.HeroObject);
            }
            else
            {
                return CalculateTroopHealth(number, character);
            }
        }

        private ExplainedNumber CalculateTroopHealth(ExplainedNumber number, CharacterObject character)
        {
            switch (character.Tier)
            {
                case 0:
                    number.Add(-15);
                    break;
                case 1:
                case 2:
                case 3:
                    break;
                case 4:
                    number.Add(20);
                    break;
                default:
                    number.Add(character.Tier * 10);
                    break;
            }
            if (character.IsUndead()&&!character.IsHero && !character.HasAttribute("NecromancerChampion"))
            {
                number.Add(-50);
            }

            if (character.HasAttribute("NecromancerChampion"))
            {
                var playerMainAgent = Mission.Current.Agents.FirstOrDefault(x => x.IsHero && x.GetHero() == Hero.MainHero);
                var value = playerMainAgent.GetComponent<AbilityComponent>().CareerAbility.Template.ScaleVariable1;
                TORCommon.Say("value :"+(int)value);
                number.Add((int)value);
            }
            
            return number;
        }

        private ExplainedNumber CalculateHeroHealth(ExplainedNumber number, Hero hero)
        {
            var info = hero.GetExtendedInfo();
            if (info != null)
            {
                if (info.AcquiredAttributes.Contains("Tier1"))
                {
                    number.Add(100, new TextObject("Tier1"));
                }
                else if (info.AcquiredAttributes.Contains("Tier2"))
                {
                    number.Add(150, new TextObject("Tier2"));
                }
                else if (info.AcquiredAttributes.Contains("Tier3"))
                {
                    number.Add(200, new TextObject("Tier3"));
                }
                else if (info.AcquiredAttributes.Contains("Tier4"))
                {
                    number.Add(300, new TextObject("Tier4"));
                }
                if (hero.IsVampire()&&!hero.IsHumanPlayerCharacter)
                {
                    number.Add(100, new TextObject("Vampire body"));
                }

                if (hero.HasAnyCareer())
                {
                    CareerHelper.ApplyBasicCareerPassives(hero, ref number, PassiveEffectType.Health, false);
                }

                if (hero.PartyBelongedTo!=null&& hero.IsPlayerCompanion)
                {
                    if (Hero.MainHero.HasCareerChoice("GuiltyByAssociationPassive3"))
                    {
                        var choice = TORCareerChoices.GetChoice("GuiltyByAssociationPassive3");
                        number.Add(choice.GetPassiveValue());
                    }
                    
                    if (Hero.MainHero.HasCareerChoice("CommanderPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("CommanderPassive4");
                        number.Add(choice.GetPassiveValue());
                    }
                    
                    if (Hero.MainHero.HasCareerChoice("EnvoyOfTheLadyPassive2"))
                    {
                        if (hero.IsBretonnianKnight())
                        {
                            var choice = TORCareerChoices.GetChoice("EnvoyOfTheLadyPassive2");
                            number.AddFactor(choice.GetPassiveValue());
                        }
                    }
                    if (Hero.MainHero.HasCareerChoice("HolyCrusaderPassive2"))
                    {
                        if (hero.IsBretonnianKnight())
                        {
                            var choice = TORCareerChoices.GetChoice("HolyCrusaderPassive2");
                            var heroes =Hero.MainHero.PartyBelongedTo.GetMemberHeroes();
                            heroes.Remove(Hero.MainHero);
                            var extraHealth= heroes.Where(knight => hero.IsBretonnianKnight()).Sum(knight=> 15);
                            number.Add(extraHealth,choice.BelongsToGroup.Name);
                        }
                    }
                    
                    
                    
                }

                if (hero.HasAttribute("GiftOfNurgle")) number.Add(20, new TextObject("Gift of Nurgle"));
            }
            if (hero.GetPerkValue(TORPerks.Faith.Devotee))
            {
                number.Add(TORPerks.Faith.Devotee.PrimaryBonus * hero.GetAttributeValue(TORAttributes.Discipline), new TextObject("Perks"));
            }
            
       
            if (Campaign.Current.CampaignStartTime.IsNow)
            {
                hero.HitPoints = (int)number.ResultNumber;
            }
            return number;
        }


        
    }
}
