using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPartyMoraleModel : DefaultPartyMoraleModel
    {
        public override ExplainedNumber GetEffectivePartyMorale(MobileParty mobileParty, bool includeDescription = false)
        {
            var result = base.GetEffectivePartyMorale(mobileParty, includeDescription);

            if (!mobileParty.IsLordParty) return result;


            if (mobileParty.HasPerk(TORPerks.Spellcraft.StoryTeller))
            {
                result.Add(TORPerks.Spellcraft.StoryTeller.SecondaryBonus, TORPerks.Spellcraft.StoryTeller.Name);
            }


            if (!mobileParty.IsMainParty || mobileParty.LeaderHero != Hero.MainHero) return result;


            AddCareerPassivesForTroopMorale(mobileParty, ref result);

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                var chivalryLevel = mobileParty.LeaderHero.GetChivalryLevel();
                var value = 0f;
                if (chivalryLevel == ChivalryLevel.Unknightly)
                {
                    value = -0.75f;
                }
                if (chivalryLevel == ChivalryLevel.Uninspiring)
                {
                    value = -0.5f;
                }
                if (chivalryLevel == ChivalryLevel.Sincere)
                {
                    value = -0.2f;
                }
                if (chivalryLevel == ChivalryLevel.Noteworthy)
                {
                    value = 0f;
                }
                if (chivalryLevel == ChivalryLevel.PureHearted)
                {
                    value = 0.1f;
                }
                if (chivalryLevel == ChivalryLevel.Honourable)
                {
                    value = 0.2f;
                }
                if (chivalryLevel == ChivalryLevel.Chivalrous)
                {
                    value = 0.2f;
                }

                result.AddFactor(value, ChivalryHelper.GetChivalryRankText(chivalryLevel));
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                if (Hero.MainHero.HasAttribute("Waaagh0"))
                {
                    result.Add(-40f, TORTextHelper.GetTextObject("tor_greenskin_internal_fightin_text", "Internal Fightin'"));
                    if (Hero.MainHero.PartyBelongedTo != null &&
                        Hero.MainHero.PartyBelongedTo.Party.NumberOfAllMembers < 50)
                    {
                        result.Add(20f, TORTextHelper.GetTextObject("tor_greenskin_internal_fightin_bonus_low_member_text", "Small Mob Pansies"));
                    }
                }
                else if (Hero.MainHero.HasAttribute("Waaagh1"))
                {
                    result.Add(-20f, TORTextHelper.GetTextObject("tor_greenskin_petty_squabblin_text", "Petty Squabblin'"));
                }
                // Waaagh2 and Waaagh3 ('Ere We Go / WAAAGH) have no morale changes

                if (Hero.MainHero.HasCareer(TORCareers.OrcBoss))
                {
                    var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(mobileParty.StringId);
                    if (partyExtendedInfo != null)
                    {
                        var totalExtorsionMalus = 0f;
                        foreach (var attributePair in partyExtendedInfo.TroopAttributes)
                        {
                            var attributes = attributePair.Value;
                            if (attributes?.Contains("Extorsion") ?? false)
                            {
                                var troopId = attributePair.Key;
                                var troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopId);
                                if (troop != null)
                                {
                                    var element = mobileParty.MemberRoster.GetTroopRoster().FirstOrDefault(x => x.Character == troop);
                                    if (element.Character != null)
                                    {
                                        totalExtorsionMalus += element.Number * 0.5f;
                                    }
                                }
                            }
                        }

                        if (totalExtorsionMalus > 0)
                        {
                            result.Add(-totalExtorsionMalus, TORTextHelper.GetTextObject("tor_greenskin_teef_extorsion_text", "Teef Extorsion"));
                        }
                    }
                }
            }

            return result;
        }

        private void AddCareerPassivesForTroopMorale(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (party.LeaderHero == null) return;
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.TroopMorale);
            }
        }
    }
}