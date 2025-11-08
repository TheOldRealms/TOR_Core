using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPartySizeModel : DefaultPartySizeLimitModel
    {
        public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
        {
            var num = base.GetPartyMemberSizeLimit(party, includeDescriptions);
            if (party?.MapFaction != null && party.Culture != null && party.Culture.StringId==TORConstants.Cultures.SYLVANIA)
            {
                if (party.LeaderHero != null && party.LeaderHero.IsHumanPlayerCharacter)
                {
                    num.Add(20, new TextObject("Friend of undead"));
                }
                else if (party.MobileParty.IsCaravan)
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

            if (party == null || party.LeaderHero ==null)
                return num;

            if (party.LeaderHero != null && party.LeaderHero == Hero.MainHero)
            {
                AddCareerPassivesForPartySize(Hero.MainHero, ref num);
            }
            
            if (party.LeaderHero?.Culture?.StringId == TORConstants.Cultures.ASRAI)
            {
                num.AddFactor(-0.25f, new TextObject("Woodelf party size cultural penalty"));

                if (party.LeaderHero == Hero.MainHero)
                {
                    var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
                    var list = settlementBehavior.GetUnlockedOakUpgradeCategory("WEPartySizeUpgrade");
                    foreach (var attribute in list)
                    {
                        num.AddFactor(0.1f);
                    }

                    if (Hero.MainHero.HasAttribute("WEKithbandSymbol"))
                    {
                        num.AddFactor(0.5f, ForestHarmonyHelper.TreeSymbolText("WEKithbandSymbol"));
                    }

                    if (Hero.MainHero.HasAttribute("WEDurthuSymbol"))
                    {
                        num.AddFactor(-0.25f, ForestHarmonyHelper.TreeSymbolText("WEDurthuSymbol"));
                    }
                }
            }

            if (party.LeaderHero != null && party.LeaderHero == Hero.MainHero &&
                Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                if (Hero.MainHero.HasAttribute("Wargh3"))
                {
                    num.Add(60, new TextObject("'Ere We Go!"));
                }
                else if (Hero.MainHero.HasAttribute("Wargh4"))
                {
                    num.Add(120, new TextObject("WAAAGH!!!!"));
                }
            }

            return num;
        }



        private void AddCareerPassivesForPartySize(Hero playerHero, ref ExplainedNumber number)
        {
            if (playerHero == null) return;
            if (playerHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(playerHero, ref number, PassiveEffectType.PartySize,false);
            }


            if (playerHero.HasCareer(TORCareers.Spellsinger))
            {
                if (Hero.MainHero.HasCareerChoice("TreeSingingPassive4"))
                {
                    var info = playerHero.GetExtendedInfo();
                    var abilities = info.AllAbilities;
                    
                    number.Add(3*abilities.Count);
                }
            }

            if (playerHero.HasCareer(TORCareers.Warden))
            {
                if (playerHero.HasCareerChoice("WardenOfAtylwythPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("WardenOfAtylwythPassive3");
                    foreach (var hero in playerHero.PartyBelongedTo.GetMemberHeroes())
                    {
                        if (hero == playerHero)
                        {
                            continue;
                        }
                        if(!hero.CharacterObject.IsElf())
                            continue;

                        if (hero.Culture.StringId != TORConstants.Cultures.ASRAI)
                        {
                            continue;
                        }

                        if (hero.HasAttribute("GladeCaptain"))
                        {
                            number.Add(choice.Passive.EffectMagnitude,choice.Description);
                        }
                    }
                }
            }
        }
    }
}
