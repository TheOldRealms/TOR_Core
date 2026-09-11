using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models
{
    public class TORPartyTroopUpgradeModel : DefaultPartyTroopUpgradeModel
    {
        public override ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
        {
            if (characterObject.IsUndead()) return new ExplainedNumber(0);

            var explainedNumber = base.GetGoldCostForUpgrade(party, characterObject, upgradeTarget);
            var applyIronbreakerDiscountLast = party.LeaderHero?.HasCareerChoice("IronPricePassive3") == true &&
                characterObject.HasAttribute(CharacterAttributes.IRONBREAKER);

            if (party.LeaderHero != null && party.LeaderHero == Hero.MainHero && !applyIronbreakerDiscountLast)
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.TroopUpgradeCost, true, characterObject);
            }

            if (characterObject.Culture.StringId == TORConstants.Cultures.DAWI)
            {
                if (party == PartyBase.MainParty)
                {
                    if (characterObject.HasAttribute(CharacterAttributes.DWARF_GUN) || upgradeTarget.HasAttribute(CharacterAttributes.DWARF_GUN))
                    {
                        if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_ENGINEERS_2))
                        {
                            explainedNumber.AddFactor(-0.25f);
                        }
                        else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_ENGINEERS_1))
                        {
                            explainedNumber.AddFactor(-0.15f);
                        }
                    }

                    if (characterObject.HasAttribute(CharacterAttributes.DWARF_WARRIOR))
                    {
                        if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_WARRIORS_3))
                        {
                            explainedNumber.AddFactor(-0.30f);
                        }
                        else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_WARRIORS_2))
                        {
                            explainedNumber.AddFactor(-0.20f);
                        }
                        else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_WARRIORS_1))
                        {
                            explainedNumber.AddFactor(-0.10f);
                        }
                    }

                    if (characterObject.HasAttribute(CharacterAttributes.IRONBREAKER))
                    {
                        explainedNumber.AddFactor(3f);
                        if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_RUNESMITH_3))
                        {
                            explainedNumber.AddFactor(-0.20f);
                        }
                        else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_RUNESMITH_2))
                        {
                            explainedNumber.AddFactor(-0.10f);
                        }
                    }

                }
            }

            if (party.LeaderHero != null && party.LeaderHero == Hero.MainHero && applyIronbreakerDiscountLast)
            {
                var costBeforeCareerPerks = explainedNumber.ResultNumber;
                var careerAdjustedCost = new ExplainedNumber(costBeforeCareerPerks, explainedNumber.IncludeDescriptions);
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref careerAdjustedCost, PassiveEffectType.TroopUpgradeCost, true, characterObject);
                // Return the adjusted final cost; adding its delta to the old base would apply the surcharges again.
                if (careerAdjustedCost.ResultNumber != costBeforeCareerPerks)
                {
                    explainedNumber = careerAdjustedCost;
                }
            }

            return explainedNumber;
        }

        public override bool CanPartyUpgradeTroopToTarget(PartyBase upgradingParty, CharacterObject upgradeableCharacter, CharacterObject upgradeTarget)
        {
            var baseValue = base.CanPartyUpgradeTroopToTarget(upgradingParty, upgradeableCharacter, upgradeTarget);
            if (baseValue == false) return baseValue;
            else
            {
                //check party has enough resources for upgrade if it needs a custom resource



                return baseValue;
            }
        }
    }
}