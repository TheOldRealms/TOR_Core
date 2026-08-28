using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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
                    if (characterObject.HasAttribute("DwarfGun") || upgradeTarget.HasAttribute("DwarfGun"))
                    {
                        if (Hero.MainHero.HasAttribute("GuildEngineersII"))
                        {
                            explainedNumber.AddFactor(-0.25f);
                        }
                        else if (Hero.MainHero.HasAttribute("GuildEngineersI"))
                        {
                            explainedNumber.AddFactor(-0.15f);
                        }
                    }

                    if (characterObject.HasAttribute("DwarfWarrior"))
                    {
                        if (Hero.MainHero.HasAttribute("GuildWarriorsIII"))
                        {
                            explainedNumber.AddFactor(-0.30f);
                        }
                        else if (Hero.MainHero.HasAttribute("GuildWarriorsII"))
                        {
                            explainedNumber.AddFactor(-0.20f);
                        }
                        else if (Hero.MainHero.HasAttribute("GuildWarriorsI"))
                        {
                            explainedNumber.AddFactor(-0.10f);
                        }
                    }

                    if (characterObject.HasAttribute("Ironbreaker"))
                    {
                        explainedNumber.AddFactor(3f);
                        if (Hero.MainHero.HasAttribute("GuildRuneSmithsIII"))
                        {
                            explainedNumber.AddFactor(-0.20f);
                        }
                        else if (Hero.MainHero.HasAttribute("GuildRuneSmithsII"))
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