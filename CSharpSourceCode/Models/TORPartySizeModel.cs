using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Invasions;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartySizeModel : DefaultPartySizeLimitModel
    {
        public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
        {
            var num = base.GetPartyMemberSizeLimit(party, includeDescriptions);
            if (party.MapFaction != null && party.Culture != null && party.Culture.Name.Contains("Vampire"))
            {
                if (party.LeaderHero != null && party.LeaderHero.IsHumanPlayerCharacter)
                {
                    num.Add(20, new TextObject("Friend of undead"));
                }
                else if (party.Id.Contains("caravan"))
                {
                    num.Add(50, new TextObject("Caravan of death"));
                }
                else if (party.IsSettlement)
                {
                    if (party.Settlement.IsVillage)
                    {
                        num.Add(50, new TextObject("Settlement of Vampire Counts"));
                    }
                    else
                    {
                        num.Add(300, new TextObject("Settlement of Vampire Counts"));
                    }
                }
                else if (party.IsMobile)
                {
                    num.Add(100, new TextObject("Vampire lord"));
                }
            }

            if (party != null && party.LeaderHero != null && party.LeaderHero == Hero.MainHero)
            {
                AddCareerPassivesForPartySize(Hero.MainHero, ref num);
            }

            if(party != null && party.LeaderHero != null && party.MobileParty != null && party.MobileParty.PartyComponent is IInvasionParty)
            {
                num.Add(1500, new TextObject("Invasion Force Bonus"));
            }
            
            return num;
        }



        private void AddCareerPassivesForPartySize(Hero playerHero, ref ExplainedNumber number)
        {
            if (playerHero == null) return;
            if (playerHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(playerHero, ref number, PassiveEffectType.PartySize);
            }
        }
    }
}
