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
        //public override int MaxCharacterTier => 9;

        public override ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
        {
            if (characterObject.IsUndead()) return new ExplainedNumber(0);

            var explainedNumber = base.GetGoldCostForUpgrade(party, characterObject, upgradeTarget);

            if (party.LeaderHero != null && party.LeaderHero == Hero.MainHero)
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