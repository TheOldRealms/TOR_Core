using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment
{
    public static class CareerAbilityChargeSupplier
    {
        public static float WitchHunterCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            
            if (chargeType != ChargeType.DamageDone) return 0;
            if (!affectingAgent.BelongsToMainParty()) return 0;

            if (affectingAgent.IsMainAgent || ( affectingAgent.IsHero && Hero.MainHero.HasCareerChoice("GuiltyByAssociationKeystone") ))
            {
                ExplainedNumber explainedNumber = new ExplainedNumber(0);
            
                if (mask == AttackTypeMask.Ranged || mask == AttackTypeMask.Melee && Hero.MainHero.HasCareerChoice("HuntTheWickedKeystone"))
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

                return explainedNumber.ResultNumber;
                
            }
            
            return 0;
        }

        public static float NecromancerCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (!affectingAgent.BelongsToMainParty()) return 0;
            if (mask == AttackTypeMask.Ranged) return 0;

            if (mask == AttackTypeMask.Melee && !affectingAgent.IsUndead())
            {
                return 0;
            }

            if (( chargeType != ChargeType.DamageDone && chargeType != ChargeType.Healed )) return 0;

            ExplainedNumber explainedNumber = new ExplainedNumber();

            explainedNumber.Add(chargeValue);

            if (mask == AttackTypeMask.Spell)
            {
                explainedNumber.AddFactor(-0.9f);
            }

            explainedNumber.Add(chargeValue);
            return explainedNumber.ResultNumber;
        }

        public static float WarriorPriestUlricCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (!affectingAgent.IsMainAgent) return 0;
            if (mask != AttackTypeMask.Melee)
            {
                return 0;
            }
            
            ExplainedNumber explainedNumber = new ExplainedNumber();
            explainedNumber.Add(chargeValue);

            if (Hero.MainHero.HasCareerChoice("FuryOfWarKeystone"))
            {
                explainedNumber.AddFactor(1);
            }
            
            return explainedNumber.ResultNumber;
        }
        
        public static float NecrarchCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (chargeType != ChargeType.DamageDone && chargeType != ChargeType.Healed) return 0;
            ExplainedNumber explainedNumber = new ExplainedNumber();
            
            if (!affectingAgent.IsHero && affectingAgent.IsUndead() && Hero.MainHero.HasCareerChoice("DiscipleOfAccursedKeystone"))
            {
                explainedNumber.Add(chargeValue);
                explainedNumber.AddFactor(-0.75f);
                return Mathf.Max(explainedNumber.ResultNumber,1);
            }
            
            if (Hero.MainHero.HasCareerChoice("DarkVisionKeystone"))
            {
                explainedNumber.AddFactor(0.25f);
            }

            if (affectingAgent.IsMainAgent)
            {
                if (Hero.MainHero.HasCareerChoice("DiscipleOfAccursedKeystone"))
                {
                    explainedNumber.AddFactor(-0.10f);
                }
                if (Hero.MainHero.HasCareerChoice("DarkVisionKeystone"))
                {
                    explainedNumber.AddFactor(-0.10f);
                }
                if (Hero.MainHero.HasCareerChoice("WitchSightKeystone"))
                {
                    explainedNumber.AddFactor(-0.10f);
                }
                if (Hero.MainHero.HasCareerChoice("UnhallowedSoulKeystone"))
                {
                    explainedNumber.AddFactor(-0.10f);
                }
                if (Hero.MainHero.HasCareerChoice("HungerForKnowledgeKeystone"))
                {
                    explainedNumber.AddFactor(-0.10f);
                }
                if (Hero.MainHero.HasCareerChoice("WellspringOfDharKeystone"))
                {
                    explainedNumber.AddFactor(-0.10f);
                }
                if (Hero.MainHero.HasCareerChoice("EverlingsSecretKeystone"))
                {
                    explainedNumber.AddFactor(-0.10f);
                }
                
            }

            if (!affectingAgent.IsHero || mask != AttackTypeMask.Spell) return explainedNumber.ResultNumber;
            explainedNumber.Add(chargeValue);
            if (!affectingAgent.IsMainAgent && !Hero.MainHero.HasCareerChoice("WellspringOfDharKeystone")) 
                return 0;
            
            return explainedNumber.ResultNumber;
        }
        
        public static float GrailDamselCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (chargeType != ChargeType.DamageDone && chargeType != ChargeType.Healed) return 0;

            if (mask != AttackTypeMask.Spell) return 0;

            if (!affectingAgent.IsHero) return 0;


            ExplainedNumber explainedNumber = new ExplainedNumber();

            if (affectingAgent.GetOriginMobileParty() != MobileParty.MainParty) return 0;


            if (!affectingAgent.IsMainAgent && !Hero.MainHero.HasCareerChoice("InspirationOfTheLadyKeystone")) return 0;

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
                    explainedNumber.AddFactor(-0.05f); // Originally only 10% of charge is taken into account, now it would be 5% 
                }
            }

            return explainedNumber.ResultNumber;
        }

        public static float MinorVampireCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (affectingAgent.GetOriginMobileParty() != MobileParty.MainParty) return 0;
            if (chargeType != ChargeType.DamageDone) return 0;

            ExplainedNumber explainedNumber = new ExplainedNumber();

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

            return explainedNumber.ResultNumber;
        }

        public static float BloodKnightCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (chargeType != ChargeType.NumberOfKills) return 0;

            if (!affectingAgent.IsHero) return 0;
            
            var explainedNumber = new ExplainedNumber();   //charge value is always 1, this is added with the calculated value  below
            
            var maxCharge = Hero.MainHero.GetCareer().MaxCharge;
            
            var malus = 0f;
            
            if (Hero.MainHero.HasCareerChoice("NightRiderKeystone"))
            {
                malus += 1;
            }
            
            if (Hero.MainHero.HasCareerChoice("BladeMasterKeystone"))
            {
                malus += 1;
            }
            
            if (Hero.MainHero.HasCareerChoice("DoomRiderKeystone"))
            {
                malus += 1;
            }
            
            if (Hero.MainHero.HasCareerChoice("AvatarOfDeathKeystone"))
            {
                malus += 1;
            }
            
            if (Hero.MainHero.HasCareerChoice("ControlledHungerKeystone"))
            {
                malus += 1;
            }
            
            if (Hero.MainHero.HasCareerChoice("DreadKnightKeystone"))
            {
                malus += 1;
            }
            
            if (Hero.MainHero.HasCareerChoice("PeerlessWarriorKeystone"))
            {
                malus += 1;
            }
            
            malus = Math.Min(5, malus);
            
            var change = maxCharge / ( 5 + malus );

           explainedNumber.Add(change);
            
            return explainedNumber.ResultNumber;
        }

        public static float WarriorPriestCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (mask != AttackTypeMask.Melee) return 0;
            if (chargeType == ChargeType.NumberOfKills) return 0;
            if (affectingAgent.Team == affectedAgent.Team) return 0;
            var explainedNumber = new ExplainedNumber();
            
            if ((chargeType != ChargeType.DamageTaken && affectedAgent.IsMainAgent) || (affectingAgent.IsMainAgent &&
                    Hero.MainHero.HasCareerChoice("BookOfSigmarKeyStone")))
            {
                if (affectingAgent.IsMainAgent && chargeType == ChargeType.DamageDone)
                {
                    if (collisionFlag == CareerHelper.ChargeCollisionFlag.HitShield) return 0;
                    explainedNumber.Add(chargeValue);
                }
                else
                {
                    var value = (float)chargeValue / Hero.MainHero.MaxHitPoints; //proportion of lost health 
                    value *= 3;
                    explainedNumber.Add(value * 300f); //scaled to maximum charge
                }


                if (collisionFlag == CareerHelper.ChargeCollisionFlag.HitShield) explainedNumber.Add(5);
            }

            return explainedNumber.ResultNumber;
        }

        public static float WaywatcherCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (mask != AttackTypeMask.Ranged) return 0;
            if (chargeType == ChargeType.NumberOfKills) return 0;
            if (collisionFlag == CareerHelper.ChargeCollisionFlag.HitShield) return 0;
            if (affectingAgent.Team == affectedAgent.Team) return 0;
            if (affectingAgent.IsEnemyOf(Agent.Main)) return 0;

            if (affectedAgent.Team == Agent.Main.Team) return 0;

            if (!affectingAgent.IsMainAgent && affectingAgent.BelongsToMainParty() &&
                !Hero.MainHero.HasCareerChoice("ForestStalkerKeystone")) return 0;

            
            chargeValue = Math.Min(150, chargeValue);
            
            
            var explainedNumber = new ExplainedNumber(chargeValue);
            
            if (affectingAgent != Agent.Main)
            {
                explainedNumber.AddFactor(-0.95f);
            }

            if (Hero.MainHero.HasCareerChoice("ProtectorOfTheWoodsKeystone"))
            {
                explainedNumber.AddFactor(0.25f);
            }
            
            
            if (collisionFlag == CareerHelper.ChargeCollisionFlag.HeadShot&& Hero.MainHero.HasCareerChoice("HawkeyedPassive2"))
            {
               explainedNumber.AddFactor(1f);
            }
            
            
            return explainedNumber.ResultNumber;
        }
        
        
        public static float SpellsingerCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (chargeType != ChargeType.DamageDone && chargeType != ChargeType.Healed) return 0;
            if (!affectingAgent.BelongsToMainParty()) return 0;
            if (mask == AttackTypeMask.Ranged) return 0;
            if (affectingAgent.IsHero && mask == AttackTypeMask.Melee) return 0;
            
            var isTreeSpirit = (affectingAgent.Character as CharacterObject).IsTreeSpirit();

            
            
            if (!affectingAgent.IsHero && !isTreeSpirit) return 0;
            
            if (mask == AttackTypeMask.Melee && isTreeSpirit)
            {
                if(!Hero.MainHero.HasCareerChoice("HeartOfTheTreeKeystone"))
                {
                    return 0;
                }
            }
            
            var explainedNumber = new ExplainedNumber(chargeValue);

            if (Hero.MainHero.HasCareerChoice("TreeSingingKeystone"))
            {
                explainedNumber.AddFactor(0.5f);
            }
            
            return explainedNumber.ResultNumber;
        }
        
        public static float GreyLordCareerCharge(Agent affectingAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, CareerHelper.ChargeCollisionFlag collisionFlag = CareerHelper.ChargeCollisionFlag.None)
        {
            if (chargeType != ChargeType.DamageDone && chargeType != ChargeType.Healed) return 0;
            if (!affectingAgent.BelongsToMainParty()) return 0;
            
            
            var explainedNumber = new ExplainedNumber(chargeValue);
            
            if(chargeType == ChargeType.Healed)
                explainedNumber.AddFactor(-0.25f);
            
            if((mask == AttackTypeMask.Spell))
            {
                if (affectingAgent.IsMainAgent)
                {
                    if (affectingAgent.GetComponent<AbilityComponent>().CareerAbility.IsOnCooldown())
                    {
                        return 0;
                    }
                }
            }
            
            if (affectingAgent.IsMainAgent)
            {
                switch (mask)
                {
                    case AttackTypeMask.Melee when Hero.MainHero.HasCareerChoice("CaelithsWisdomKeystone"):
                        return explainedNumber.ResultNumber * 3;
                    case AttackTypeMask.Melee:
                        return 0;
                    case AttackTypeMask.Ranged:
                        break;
                    case AttackTypeMask.Spell:
                        return explainedNumber.ResultNumber;
                        break;
                    case AttackTypeMask.All:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mask), mask, null);
                }
            }

            if (affectingAgent.IsHero && !affectingAgent.IsMainAgent)
            {
                switch (mask)
                {
                    case AttackTypeMask.Melee when Hero.MainHero.HasCareerChoice("ForbiddenScrollsOfSapheryKeystone"):
                        return explainedNumber.ResultNumber * 3;
                    case AttackTypeMask.Melee:
                        return 0;
                    case AttackTypeMask.Ranged when Hero.MainHero.HasCareerChoice("ForbiddenScrollsOfSapheryKeystone"):
                        return explainedNumber.ResultNumber;
                    case AttackTypeMask.Ranged:
                        return 0;
                    case AttackTypeMask.Spell:
                        return explainedNumber.ResultNumber;
                        break;
                    case AttackTypeMask.All:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mask), mask, null);
                }
            }


            return 0;
        }
    }
}