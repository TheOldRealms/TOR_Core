using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models;

public class TORCustomResourceModel : GameModel
{
            public ExplainedNumber GetCultureSpecificCustomResourceChange(Hero hero)
        {
            if (hero.PartyBelongedTo == null) return new ExplainedNumber();

            var number = new ExplainedNumber(0,true);
            if (hero.GetCultureSpecificCustomResource() != null)
            {
                var upkeep =  GetCalculatedCustomResourceUpkeep(hero, hero.GetCultureSpecificCustomResource().StringId);
                
                if (upkeep.ResultNumber < 0)
                {
                    foreach (var line in upkeep.GetLines())
                    {
                        number.Add((int)line.number, new TextObject(line.name));
                    }
                }

                if (hero == Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref number,PassiveEffectType.CustomResourceGain, false);
                    
                    if (hero.IsEnlisted())
                    {
                        ServeAsAHirelingHelpers.AddHirelingCustomResourceBenefits(hero,ref number);
                    }

                    if (hero.Culture.StringId == TORConstants.Cultures.ASRAI)
                    {
                        foreach (var attribute in  OakOfAgesMenuLogic.CustomResourceGainUpgrades)
                        {
                            if(Hero.MainHero.HasAttribute(attribute))
                            {
                                number.Add(10f);
                            }
                        }
                    }
                }
                
                

                if (hero.HasCareer(TORCareers.BlackGrailKnight)&& hero.HasCareerChoice("BlackGrailVowPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("BlackGrailVowPassive4");
                    if (hero.PartyBelongedTo != null)
                    {
                        var heroes = hero.PartyBelongedTo.GetMemberHeroes();
                        heroes.Remove(Hero.MainHero);

                        foreach (var companion in heroes)
                        {
                            if (companion.IsVampire() || companion.IsNecromancer())
                            {
                                number.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                            }
                        }
                    }
                }

                if (hero.HasCareer(TORCareers.GrailKnight)&&hero.HasCareerChoice("QuestingVowPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("QuestingVowPassive4");
                    var heroes = hero.PartyBelongedTo.GetMemberHeroes();
                    heroes.Remove(Hero.MainHero);
                    foreach (var companion in heroes)
                    {
                        if (companion.IsBretonnianKnight())
                        {
                            number.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                        }
                    }
                }

                if (hero.HasCareer(TORCareers.Necrarch) && hero.HasCareerChoice("EverlingsSecretPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("EverlingsSecretPassive3");
                    if (choice!=null)
                    {
                        if (hero.GetExtendedInfo().MaxWindsOfMagic <= hero.GetCustomResourceValue("WindsOfMagic"))
                        {
                            number.Add(hero.GetExtendedInfo().WindsOfMagicRechargeRate * CampaignTime.HoursInDay, choice.BelongsToGroup.Name);
                        }
                    }
                    
                }

                if (hero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    if (hero.PartyBelongedTo != null)
                    {
                        if (hero.PartyBelongedTo.HasBlessing("cult_of_lady"))
                        {
                            var obj = ReligionObject.All.FirstOrDefault( x=>x.StringId=="cult_of_lady");
                            if (obj != null)
                            {
                                
                                number.Add(15,new TextObject("Blessing of the Lady"));
                            }
                            
                        }
                    }

                    if (hero.IsClanLeader)
                    {
                        foreach (var clanmember in hero.Clan.Heroes)
                        {
                            if(clanmember==hero) continue;

                            if (clanmember.IsAlive&&clanmember.IsPartyLeader)
                            {
                                number.Add(2,new TextObject("Clan members with Party"));
                            }
                        }
                    }

                    if (hero.GetChivalryLevel() == ChivalryLevel.Honourable)
                    {
                        number.Add(5,new TextObject(ChivalryLevel.Honourable.ToString()));
                    }
                    
                    if (hero.GetChivalryLevel() == ChivalryLevel.Chivalrous)
                    {
                        number.Add(15,new TextObject(ChivalryLevel.Honourable.ToString()));
                    }
                    
                }

                if (hero.Culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    number.Add(15, new TextObject("Growth"));
                }
            } 
            return number;
        }

        public ExplainedNumber GetCalculatedCustomResourceUpkeep(Hero hero, string resourceID="")
        {
            if (resourceID == "")
            {
                resourceID = hero.GetCultureSpecificCustomResource().StringId;
            }
            var upkeep = new ExplainedNumber(0,true,new TextObject("Upkeep"));
            foreach (var element in hero.PartyBelongedTo.MemberRoster.GetTroopRoster())
            {
                if (element.Character.HasCustomResourceUpkeepRequirement())
                {
                    var resource = element.Character.GetCustomResourceRequiredForUpkeep();
                
                    if(resource.Item1.StringId !=resourceID) continue;
                    var unitUpkeet = new ExplainedNumber(resource.Item2*element.Number);
                    if (hero == Hero.MainHero)
                    {
                        CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref unitUpkeet,PassiveEffectType.CustomResourceUpkeepModifier, true, element.Character);

                        if (hero.Culture.StringId == TORConstants.Cultures.ASRAI)
                        {
                            foreach (var attribute in OakOfAgesMenuLogic.CustomResourceGainUpgrades.Where(attribute => Hero.MainHero.HasAttribute(attribute)))
                            {
                                unitUpkeet.AddFactor(-0.1f);
                            }
                        }
                    }
                    
                    upkeep.Add(-unitUpkeet.ResultNumber,new TextObject("Upkeep"));
                    
                }
            }

            foreach (var settlement in hero.Clan.Settlements)
            {
                if (!settlement.IsCastle && !settlement.IsTown)
                {
                    continue;
                }
                if(settlement.Town.GarrisonParty==null) continue;
                var garrison = settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster();
                foreach (var elem in garrison)
                {
                    if (elem.Character.HasCustomResourceUpgradeRequirement())
                    {
                        var resource = elem.Character.GetCustomResourceRequiredForUpkeep();
                        
                        if(resource==null) continue;
                
                        if(resource.Item1.StringId !=resourceID) continue;
                        var garrisonFactor = 0.25f; //base reduction bonus 
                        var garrisonUnitUpkeep = new ExplainedNumber(resource.Item2*elem.Number*garrisonFactor);
                        if (hero == Hero.MainHero)
                        {
                            CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref garrisonUnitUpkeep,PassiveEffectType.CustomResourceUpkeepModifier, true, elem.Character); 
                        }
                    
                        upkeep.Add(-garrisonUnitUpkeep.ResultNumber,new TextObject("Garrison Upkeep"));
                    }
                }
            }
            
            return upkeep;
        }
        
        
}