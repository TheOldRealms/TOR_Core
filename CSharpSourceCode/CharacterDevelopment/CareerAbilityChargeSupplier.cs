using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment
{
    public static class CareerAbilityChargeSupplier
    {
        public static ExplainedNumber WitchHunterCareerCharge( Agent affectingAgent,Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(0);
            if (chargeType != ChargeType.DamageDone) return explainedNumber;

            if (affectingAgent.IsHero && affectingAgent.IsMainAgent)
            {
                explainedNumber.Add(chargeValue);
            }
            
            
            
            if ( mask == AttackTypeMask.Ranged || mask == AttackTypeMask.Melee && Hero.MainHero.HasCareerChoice("HuntTheWickedKeystone") )
            {
                if (mask == AttackTypeMask.Ranged)
                {
                    explainedNumber.AddFactor(-0.5f);
                }

                if (collisionFlag == CareerHelper.ChargeCollisionFlag.HeadShot)
                {
                    explainedNumber.AddFactor(1f);
                }

                explainedNumber.Add(chargeValue);
            }

            return explainedNumber;
        }
        
        public static ExplainedNumber NecromancerCareerCharge(Agent affectingAgent,Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            ExplainedNumber explainedNumber =new ExplainedNumber();
            if( mask == AttackTypeMask.Melee&&!affectingAgent.IsUndead())
            {
                return explainedNumber;
            }
            
            if (chargeType != ChargeType.DamageDone) return explainedNumber; 
            
            explainedNumber.Add(chargeValue);
            
            
            if (mask == AttackTypeMask.Spell)
            {
                explainedNumber.AddFactor(-0.9f);
            }
                        
            explainedNumber.Add(chargeValue);
            return explainedNumber;
        }
        
        public static ExplainedNumber GrailDamselCareerCharge(Agent affectingAgent,Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            ExplainedNumber explainedNumber =new ExplainedNumber();
            
            if (chargeType != ChargeType.DamageDone) return explainedNumber;    //heal also 

            if (mask != AttackTypeMask.Spell) return explainedNumber;

            if (!affectingAgent.IsHero) return explainedNumber;

            if (affectingAgent.GetOriginMobileParty() != MobileParty.MainParty) return explainedNumber;
           

            if (!affectingAgent.IsMainAgent && !Hero.MainHero.HasCareerChoice("InspirationOfTheLadyKeystone")) return explainedNumber;
            
            explainedNumber.Add(chargeValue);
                       
            explainedNumber.AddFactor(-0.9f);

            var choices = Hero.MainHero.GetAllCareerChoices();
                            
            if (choices.Contains("VividVisionsKeystone"))
            {
                var choice = TORCareerChoices.GetChoice("VividVisionsKeystone");
                if (choice != null)
                {
                    var value = choice.GetPassiveValue();
                    explainedNumber.AddFactor(value);
                }
            }
            if (choices.Contains("InspirationOfTheLadyKeystone"))
            {
                var choice = TORCareerChoices.GetChoice("InspirationOfTheLadyKeystone");
                if (choice != null)
                {
                    explainedNumber.AddFactor(-0.05f);           // Originally only 10% of charge is taken into account, now it would be 5% 
                }
            }
            return explainedNumber;




        }

        public static ExplainedNumber MinorVampireCareerCharge(Agent affectingAgent,Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            ExplainedNumber explainedNumber =new ExplainedNumber();
            if (chargeType != ChargeType.DamageDone) return explainedNumber; 
            
            explainedNumber.Add(chargeValue);

            if (mask == AttackTypeMask.Spell && Hero.MainHero.GetAllCareerChoices().Contains("ArkayneKeystone"))
            {
                explainedNumber.AddFactor(-0.9f);
            }
                            
            if (Hero.MainHero.GetAllCareerChoices().Contains("NewBloodKeystone"))
            {
                var choice = TORCareerChoices.GetChoice("NewBloodKeystone");
                if (choice != null)
                {
                    var value = choice.GetPassiveValue();
                    explainedNumber.AddFactor(value);
                }
            }

            if (Hero.MainHero.GetAllCareerChoices().Contains("MartialleKeystone"))
            {
                var choice = TORCareerChoices.GetChoice("MartialleKeystone");
                if (choice != null)
                {
                    var value = choice.GetPassiveValue();
                    explainedNumber.AddFactor(value);
                }
            }

            return explainedNumber;
        }

        public static ExplainedNumber BloodKnightCareerCharge(Agent affectingAgent,Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber();
            if (chargeType != ChargeType.NumberOfKills) return explainedNumber;

            if (!affectingAgent.IsHero) return explainedNumber;
            if (affectingAgent.IsMainAgent || affectingAgent.GetOriginMobileParty().IsMainParty&&Hero.MainHero.HasCareerChoice("NightRiderKeystone"))
            {
                explainedNumber.Add(chargeValue);
            }
            
            
            if (Hero.MainHero.HasCareerChoice("DreadKnightKeystone"))
            {
                var choice = TORCareerChoices.GetChoice("DreadKnightKeystone");
                if (choice != null)
                {
                    var value = choice.GetPassiveValue();
                    explainedNumber.AddFactor(value);
                }
            }
            
            return explainedNumber;
        }
        
        public static ExplainedNumber WarriorPriestCareerCharge(Agent affectingAgent,Agent affectedAgent,ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            ExplainedNumber explainedNumber =new ExplainedNumber();
            if (chargeType != ChargeType.DamageTaken) return explainedNumber;
            
            explainedNumber.Add(chargeValue);
            
            explainedNumber.Add((float) chargeValue / Hero.MainHero.MaxHitPoints);      //proportion of lost health 
                        
            if (collisionFlag == CareerHelper.ChargeCollisionFlag.HitShield)
            {
                explainedNumber.AddFactor(-0.85f);
            }

            return explainedNumber;
        }
        
        
    }
}