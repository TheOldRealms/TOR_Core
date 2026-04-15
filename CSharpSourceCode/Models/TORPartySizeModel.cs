using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.RaidingParties;
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
            //Sly : PartyBase.Culture is actually PartyBase.MapFaction.Culture with no null protection on MapFaction in native

            var num = base.GetPartyMemberSizeLimit(party, includeDescriptions);
            if (party?.MapFaction != null && party.Culture != null && party.Culture.StringId == TORConstants.Cultures.SYLVANIA)
            {
                if (party.LeaderHero != null && party.LeaderHero.IsHumanPlayerCharacter)
                {
                    num.Add(20, TORTextHelper.GetTextObject("tor_party_size_desc.FriendOfUndead", "Friend of undead"));
                }
                else if (party.MobileParty.IsCaravan)
                {
                    num.Add(50, TORTextHelper.GetTextObject("tor_party_size_desc.CaravanOfDeath", "Caravan of death"));
                }
                else if (party.IsSettlement)
                {
                    if (party.Settlement.IsVillage)
                    {
                        num.Add(50, TORTextHelper.GetTextObject("tor_party_size_desc.SettlementVampireCounts", "Settlement of Vampire Counts"));
                    }
                    else
                    {
                        num.Add(300, TORTextHelper.GetTextObject("tor_party_size_desc.SettlementVampireCounts", "Settlement of Vampire Counts"));
                    }
                }
                else if (party.IsMobile)
                {
                    num.Add(100, TORTextHelper.GetTextObject("tor_party_size_desc.VampireLord", "Vampire lord"));
                }
            }

            if (party?.MapFaction != null && party.Culture?.StringId == TORConstants.Cultures.GREENSKIN &&
                party.IsMobile &&
                party.LeaderHero != Hero.MainHero)
            {
                num.AddFactor(0.35f, TORTextHelper.GetTextObject("tor_party_size_desc.Greenskins", "Greenskins"));
            }

            //Sly : prevents the parties from taking Overmanned speed penalties, they'll now move as fast as parties of foot bandits.
            if (party.MobileParty.IsRaidingParty())
            {
                num.Add(80);//Sly : raiding components are AI parties only, description left blank.
            }

            if (party == null || party.LeaderHero == null)
                return num;


            if (party.LeaderHero == Hero.MainHero)
            {
                AddCareerPassivesForPartySize(Hero.MainHero, party, ref num);
            }

            if (party.MapFaction != null && party.Culture?.StringId == TORConstants.Cultures.DAWI)
            {
                num.AddFactor(-0.25f, TORTextHelper.GetTextObject("tor_party_size_desc.DwarfPenalty", "Dwarf cultural penalty"));
            }

            if (party.MapFaction != null && party.Culture?.StringId == TORConstants.Cultures.ASRAI)
            {
                num.AddFactor(-0.25f, TORTextHelper.GetTextObject("tor_party_size_desc.WoodelfPenalty", "Woodelf cultural penalty"));

                if (party.LeaderHero == Hero.MainHero)
                {
                    var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
                    var list = settlementBehavior.GetUnlockedOakUpgradeCategory("WEPartySizeUpgrade");
                    var oakPartyBonus = 0f;
                    foreach (var attribute in list)
                    {
                        oakPartyBonus += 10f;
                    }
                    if (oakPartyBonus > 0) num.Add(oakPartyBonus, TORTextHelper.GetTextObject("tor_party_size_desc.OakOfAgesOutposts", "Oak of Ages outposts"));

                    if (Hero.MainHero.HasAttribute("WEKithbandSymbol"))
                    {
                        num.AddFactor(0.25f, ForestHarmonyHelper.TreeSymbolText("WEKithbandSymbol"));
                    }

                    if (Hero.MainHero.HasAttribute("WEDurthuSymbol"))
                    {
                        num.AddFactor(-0.2f, ForestHarmonyHelper.TreeSymbolText("WEDurthuSymbol"));
                    }
                }
            }

            if (party.LeaderHero == Hero.MainHero &&
                Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                if (Hero.MainHero.HasAttribute("Waaagh2"))
                {
                    num.Add(60, TORTextHelper.GetTextObject("tor_party_size_desc.EreWeGo", "'Ere We Go!"));
                }
                else if (Hero.MainHero.HasAttribute("Waaagh3"))
                {
                    num.Add(120, TORTextHelper.GetTextObject("tor_party_size_desc.Waaagh", "WAAAGH!!!!"));
                }
            }

            //Sly : Prevents the subtractions for unit weights from applying to the multipliers and causing their values to fluctuate with troops changes providing predictability in party size.
            var compositeNum = new ExplainedNumber(0, includeDescriptions, null);
            compositeNum.AddFromExplainedNumber(num, GameTexts.FindText("str_base_size"));

            ApplyMonstrousUnitWeight(party, ref compositeNum);//Player clan check immediately inside

            return compositeNum;
        }

        public override TroopRoster FindAppropriateInitialRosterForMobileParty(MobileParty party, PartyTemplateObject partyTemplate)
        {
            if (!party.IsRaidingParty()) return base.FindAppropriateInitialRosterForMobileParty(party, partyTemplate);

            //scales the party size to the approximately approriate amount based on the field that stores the intended amount during component initialization
            var raidingComponent = party.PartyComponent as RaidingPartyComponent;
            var targetPartySize = raidingComponent._partySize;

            TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
            if (targetPartySize <= 0)
            {
                TORCommon.Log("TORPartySizeModel : party attempted to be spawned with no troops in it. Adding one troop to it.", NLog.LogLevel.Info);
                var defaultCharObj = Campaign.Current.ObjectManager.GetFirstObject<CharacterObject>();
                troopRoster.AddToCounts(defaultCharObj, 1);
                return troopRoster;
            }
            
            float templateMaxCount = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MaxValue);
            float initialPartySizeRatio = targetPartySize / templateMaxCount;
            //reverse iteration as the troops are added to the party in the order of the Stacks and so depleted troops during subtraction here will be removed from the last indexes first and avoid index errors
            for (int i = partyTemplate.Stacks.Count - 1; i > -1 ; i--)
            {
                int maxValue = partyTemplate.Stacks[i].MaxValue;
                int num = (int)(maxValue * initialPartySizeRatio);

                CharacterObject character = partyTemplate.Stacks[i].Character;
                troopRoster.AddToCounts(character, num);
            }

            return troopRoster;
        }

        private void AddCareerPassivesForPartySize(Hero playerHero, PartyBase party, ref ExplainedNumber number)
        {
            if (playerHero == null) return;
            if (playerHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(playerHero, ref number, PassiveEffectType.PartySize, false);
                AddUnitPartyWeightBonus(playerHero, party, ref number);
            }

            if (playerHero.HasCareer(TORCareers.Spellsinger))
            {
                if (Hero.MainHero.HasCareerChoice("TreeSingingPassive4"))
                {
                    var info = playerHero.GetExtendedInfo();
                    var abilities = info.AllAbilities;

                    number.Add(3 * abilities.Count);
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
                        if (!hero.CharacterObject.IsElf())
                            continue;

                        if (hero.Culture.StringId != TORConstants.Cultures.ASRAI)
                        {
                            continue;
                        }

                        if (hero.HasAttribute("GladeCaptain"))
                        {
                            number.Add(choice.Passive.EffectMagnitude, choice.Description);
                        }
                    }
                }
            }
        }

        private void AddUnitPartyWeightBonus(Hero playerHero, PartyBase party, ref ExplainedNumber number)
        {
            if (party?.MemberRoster == null) return;

            var choices = playerHero.GetAllCareerChoices();
            foreach (var choiceId in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceId);
                if (choice?.Passive == null) continue;
                if (choice.Passive.PassiveEffectType != PassiveEffectType.UnitPartyWeight) continue;

                float reduction = MathF.Clamp(choice.Passive.EffectMagnitude, 0f, 0.9f);
                if (reduction <= 0f) continue;

                int matchingUnitCount = 0;
                foreach (var element in party.MemberRoster.GetTroopRoster())
                {
                    if (element.Character != null && choice.Passive.IsValidCharacterObject(element.Character))
                    {
                        matchingUnitCount += element.Number;
                    }
                }

                if (matchingUnitCount > 0)
                {
                    // Reduction 0.2 means 20% weight reduction, freeing 0.2 slots per matching unit
                    // Multiple perks stack additively: 0.2 + 0.2 = 0.4 (40% total reduction)
                    float bonusSlots = matchingUnitCount * reduction;
                    number.Add(bonusSlots, new TextObject(choice.BelongsToGroup.Name.ToString()));
                }
            }
        }

        private void ApplyMonstrousUnitWeight(PartyBase party, ref ExplainedNumber number)
        {
            if (party?.MemberRoster == null) return;
            if (party.LeaderHero?.Clan != Clan.PlayerClan) return;

            int treemenCount = 0;
            int minotaurCount = 0;
            int trollCount = 0;
            int goblinCount = 0;

            foreach (var element in party.MemberRoster.GetTroopRoster())
            {
                if (element.Character == null) continue;

                if (element.Character.StringId.Contains("treeman"))
                {
                    treemenCount += element.Number;
                }
                else if (element.Character.IsMinotaur())
                {
                    minotaurCount += element.Number;
                }
                else if (element.Character.IsTroll())
                {
                    trollCount += element.Number;
                }
                else if (element.Character.IsGoblin())
                {
                    goblinCount += element.Number;
                }
            }

            // Treemen weight: 10 slots with WEDurthuSymbol, 25 slots otherwise
            bool hasDurthuSymbol = party.LeaderHero.HasAttribute("WEDurthuSymbol");
            int treemenWeight = hasDurthuSymbol ? 10 : 24;

            if (treemenCount > 0)
            {
                number.Add(-(treemenWeight) * treemenCount, TORTextHelper.GetTextObject("tor_party_size_desc.TreemenWeight", "Treemen weight"));
            }

            // Minotaurs take 8 slots each (7 extra beyond the 1 they already occupy)
            if (minotaurCount > 0)
            {
                number.Add(-7 * minotaurCount, TORTextHelper.GetTextObject("tor_party_size_desc.MinotaurWeight", "Minotaur weight"));
            }

            // Trolls take 8 slots each (7 extra beyond the 1 they already occupy)
            if (trollCount > 0)
            {
                number.Add(-7 * trollCount, TORTextHelper.GetTextObject("tor_party_size_desc.TrollWeight", "Troll weight"));
            }

            // Dryads and Elves both cost 1 slot (no weight penalty)

            // Goblins count as 0.5 units (half a slot each)
            if (goblinCount > 0)
            {
                number.Add(0.5f * goblinCount, TORTextHelper.GetTextObject("tor_party_size_desc.GoblinWeight", "Goblin weight"));
            }
        }

        public static void RecalculateMainPartySize()
        {
            var _ = PartyBase.MainParty.PartySizeLimit;//recalculate to force a cache refresh of the underlying PartyBase's PartySizeLimit - the value is of no consequence.
        }
    }
}