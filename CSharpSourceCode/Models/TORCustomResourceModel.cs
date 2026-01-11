using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.Models;

public class TORCustomResourceModel : GameModel
{
    //Sly : screen refresh recalculates the values for the UI elements on each tick so any common text is probably worth storing rather than fetching constantly
    private static TextObject ENCHANTEDEQUIPMENTTEXT = GameTexts.FindText("tor_generic_enchantedEquipment");
    private static TextObject UPKEEPTEXT = GameTexts.FindText("tor_generic_upkeep");


    public ExplainedNumber GetCultureSpecificCustomResourceChange(Hero hero, string resourceid)
    {
        if (hero.PartyBelongedTo == null) return new ExplainedNumber(); //does this imply that the player has no custom resource changes while a prisoner? If they are captured, do they stop paying for garrison upkeep?

        var number = new ExplainedNumber(0, true);
        if (hero.GetCultureSpecificCustomResource() == null) return number;

        var party = hero.PartyBelongedTo;
        var clan = hero.Clan;
        var culture = hero.Culture;

        //if resources are ever expanded to the AI, the foreach(clanmember) will need to be taken out of the Hero.MainHero conditional (if the intention is to have those equipment traits apply to any hero)

        if (hero == Hero.MainHero)
        {
            CareerHelper.ApplyBasicCareerPassives(hero, ref number, PassiveEffectType.CustomResourceGain, false);

            foreach (var clanmember in clan.Heroes)
            {
                var list = clanmember.CharacterObject.GetCharacterEquipment();
                foreach (var item in list)
                {
                    var traits = item.GetTraits();
                    foreach (var trait in traits)
                    {
                        if (trait.StatsTuple?.StatType == ItemTraitStatType.CustomResourceGain)
                        {
                            number.Add(trait.StatsTuple.Value, ENCHANTEDEQUIPMENTTEXT);
                        }
                    }
                }
            }

            if (hero.IsEnlisted())
            {
                ServeAsAHirelingHelpers.AddHirelingCustomResourceBenefits(hero, ref number);
            }

            var career = hero.GetCareer();

            if (career == TORCareers.KnightOldWorld)
            {
                var partyAttributes = ExtendedInfoManager.Instance.GetPartyInfoFor(party.StringId);

                var attributes = partyAttributes.TroopAttributes;

                var troops = party.MemberRoster.GetTroopRoster();

                foreach (var attribute in attributes.Where(attribute => attribute.Value.Contains("SigmarSeal3")))
                {
                    var troop = troops.FirstOrDefaultQ(x => x.Character.StringId == attribute.Key);

                    if (troop.Number > 0)
                    {
                        number.AddFactor(0.01f * troop.Number, GameTexts.FindText("TORKnightPuritySealName", "SigmarSeal3"));
                    }
                }
            }

            if (career == TORCareers.Runelord)
            {
                if (hero.HasCareerChoice("LegacyOfGrungniPassive2"))
                {
                    var choice = TORCareerChoices.GetChoice("LegacyOfGrungniPassive2");
                    var workshops = hero.OwnedWorkshops.WhereQ(X => X.WorkshopType.StringId.Contains("smithy") && X.Settlement.IsDwarfKarak());

                    number.Add(2 * workshops.CountQ(), choice.BelongsToGroup.Name);
                }
            }

            if (!hero.IsEnlisted() && party.Army != null && hero.PartyBelongedTo.Army.LeaderParty != party)
            {
                number.Add(5, GameTexts.FindText("tor_generic_armyMember"));
            }

            if (career == TORCareers.Spellsinger && party.HasBlessing("cult_of_isha"))
            {
                if (hero.HasCareerChoice("ArielsBlessingPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("ArielsBlessingPassive3");
                    number.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                }
            }

            if (career == TORCareers.Necromancer)
            {
                if (hero.HasCareerChoice("BookofWsoranPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("BookofWsoranPassive4");
                    var elements = party.MemberRoster.GetTroopRoster().WhereQ(x => x.Character.StringId.Contains("grave_guard")).ToList();
                    var bonus = 0f;
                    foreach (var element in elements)
                    {
                        bonus += element.Number;
                    }

                    number.Add((int)(bonus * choice.GetPassiveValue()), choice.BelongsToGroup.Name);
                }
            }

            if (clan?.Kingdom != null && (culture.StringId == TORConstants.Cultures.SYLVANIA || culture.StringId == TORConstants.Cultures.MOUSILLON) && (clan.Kingdom.Culture.StringId == TORConstants.Cultures.SYLVANIA || clan.Kingdom.Culture.StringId == TORConstants.Cultures.MOUSILLON) && !hero.IsEnlisted()) //Sly : restricts the hero to only gain dark energy from serving a culture that would generate it, eg. sylvanian player is a mercenary for tilea shouldn't receive tribute as if the tileans were performing dark magic rituals
            {
                var kingdom = clan.Kingdom;

                var kingdomSettlements = kingdom.Settlements;
                var bonus = 0;
                foreach (var settlement in kingdomSettlements)
                {
                    var value = 0;

                    if (settlement.IsVillage) continue;

                    if (settlement.IsCastle) value = 2;

                    else if (settlement.IsTown) value = 3;

                    if (settlement.OwnerClan == clan) value *= 2;

                    bonus += value;
                }

                number.Add(bonus, GameTexts.FindText("tor_custom_resource_vc_mo_darkTribute"));
            }

            if (clan != null && (culture.StringId == TORConstants.Cultures.GREENSKIN))
            {
                var playerSettlements = clan.Fiefs.WhereQ(x => x.Settlement.Owner == Hero.MainHero && x.Settlement.IsGreenskinCamp());

                var tribute = 0;
                foreach (var settlement in playerSettlements)
                {
                    var element = settlement.Settlement.Stash.FirstOrDefaultQ(x => x.EquipmentElement.Item.StringId == "tor_gs_loot_pile");
                    tribute = element.Amount / 10;
                }

                number.Add(tribute, TORTextHelper.GetTextObject("tor_greenskin_teef_tribute_text", "Teef Tribute"));

                if (hero.HasCareer(TORCareers.OrcBoss))
                {
                    if (hero.HasCareerChoice("MeanestanDaBaddestPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("MeanestanDaBaddestPassive4");
                        if (choice != null)
                        {
                            var companions = party.GetMemberHeroes().Where(x => x.HasAttribute("BigBoss")).Count();
                            number.Add(companions * choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                        }
                    }

                    var info = hero.PartyBelongedTo.GetPartyInfo();

                    var factor = Hero.MainHero.HasCareerChoice("LeafNuffinBehinPassive4") ? 2 : 1;

                    foreach (var attributePair in info.TroopAttributes)
                    {
                        var attributes = attributePair.Value;
                        if (!attributes.Contains("Extorsion"))
                            continue;

                        var element = Hero.MainHero.PartyBelongedTo.MemberRoster.GetTroopRoster().FirstOrDefault(x => x.Character.StringId == attributePair.Key);
                        if (element.Character != null)
                        {
                            number.Add(element.Character.Tier * factor * element.Number, TORTextHelper.GetTextObject("tor_greenskin_teef_extorsion_text", "Teef Extorsion"));
                        }
                    }
                }
            }

            if (culture.StringId == TORConstants.Cultures.ASRAI)
            {
                var forestText = GameTexts.FindText("tor_custom_resource_we_athelLorenHarmony");
                number.Add(0.001f, forestText); //purely to spite myself so that passive gains are listed at the top before the long list of penalties - later value is adjusted to accomodate #cursed
                var priorHarmonyGains = number.ResultNumber; //store accumulated prior gains before settlement negatives are applied - this is to have symbols which affect harmony gain to not have their effect mitigated by deductions
                var settlementGain = 0f;

                var weFiefs = Town.AllFiefs.WhereQ(x => x.StringId.Contains("_AL")).ToList(); //castles and towns
                List<Village> weVillages = []; //Village.All.WhereQ(x => x.StringId.Contains("_AL")).ToList();
                //Sly : I don't see a point in iterating them all when the number of asrai villages will be far smaller. 
                weFiefs.ForEach(x =>
                {
                    if (x.Settlement.BoundVillages != null)
                    {
                        foreach (var village in x.Settlement.BoundVillages) //No need to check the id because all WE villages should be correctly assigned to starting WE fortifications.
                        {
                            weVillages.Add(village);
                        }
                    }
                });

                //WE fiefs sit around 2 villages per, so ~2.5 harmony/held fief as base
                foreach (var fief in weFiefs)
                {
                    if (fief.Owner.Culture.StringId != TORConstants.Cultures.ASRAI)
                    {
                        number.Add(-5, GameTexts.FindText("tor_generic_isCaptured").SetTextVariable("SETTLEMENT", fief.Name));
                        continue;
                    }
                    settlementGain += 1.5f;
                }

                foreach (var village in weVillages)
                {
                    if (village.Owner.Culture.StringId != TORConstants.Cultures.ASRAI) //should this even apply to villages which are bound to a fief?
                    {
                        number.Add(-2, GameTexts.FindText("tor_generic_isCaptured").SetTextVariable("SETTLEMENT", village.Name));
                        continue;
                    }
                    if (village.VillageState == Village.VillageStates.Looted)
                    {
                        number.Add(-3, GameTexts.FindText("tor_generic_isLooted").SetTextVariable("SETTLEMENT", village.Name));
                        continue;
                    }
                    if (village.VillageState == Village.VillageStates.BeingRaided)
                    {
                        number.Add(-2, GameTexts.FindText("tor_generic_isRaided").SetTextVariable("SETTLEMENT", village.Name));
                        continue;
                    }
                    settlementGain += 0.5f;
                }

                if (settlementGain > 0)
                {
                    number.Add(settlementGain - 0.01f, forestText);//adjusted for prior non-zero amount - there's no way to get 0.01 from a combination of multipliers and the lowest 0.5 bonus from a village
                    var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
                    var list = settlementBehavior.GetUnlockedOakUpgradeCategory("WEGainUpgrade");
                    var harmonyFactor = 0.2f * list.Count; //20% per upgrade
                    if (harmonyFactor > 0) number.Add(harmonyFactor * settlementGain, GameTexts.FindText("tor_custom_resource_we_oakSettlementIncomeBonus"));
                }

                //apply symbol penalty to all gains
                priorHarmonyGains += settlementGain;
                if (priorHarmonyGains > 0)
                {
                    var symbolPenalty = ForestHarmonyHelper.HarmonySymbolPenalty();
                    number.Add(-symbolPenalty * priorHarmonyGains, GameTexts.FindText("tor_custom_resource_we_symbolPenalty"));
                }
            }

            if (career == TORCareers.BlackGrailKnight && hero.HasCareerChoice("BlackGrailVowPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("BlackGrailVowPassive4");
                var necroCompanions = party.GetMemberHeroes().Where(hero => hero.IsNecromancer() || hero.IsVampire()).Count();
                number.Add(necroCompanions * choice.GetPassiveValue(), choice.BelongsToGroup.Name);
            }

            if (career == TORCareers.GrailKnight && hero.HasCareerChoice("QuestingVowPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("QuestingVowPassive4");
                if (choice != null)
                {
                    var knightCompanions = party.GetMemberHeroes().Where(x => hero.IsBretonnianKnight()).Count();
                    number.Add(knightCompanions * choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                }
            }

            if (career == TORCareers.Necrarch && hero.HasCareerChoice("EverlingsSecretPassive3"))
            {
                var choice = TORCareerChoices.GetChoice("EverlingsSecretPassive3");
                if (choice != null)
                {
                    if (hero.GetExtendedInfo().MaxWindsOfMagic <= hero.GetCustomResourceValue("WindsOfMagic"))
                    {
                        number.Add(hero.GetExtendedInfo().WindsOfMagicRechargeRate * CampaignTime.HoursInDay, choice.BelongsToGroup.Name);
                    }
                }
            }

            if (culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                if (party.HasBlessing("cult_of_lady"))
                {
                    var obj = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_lady");
                    if (obj != null)
                    {
                        number.Add(15, GameTexts.FindText("tor_religion_blessing_name", "cult_of_lady"));
                    }
                }

                if (hero.IsClanLeader)
                {
                    var clanPartyBonus = Math.Max((clan.WarPartyComponents.Count - 1) * 2, 0);
                    if (clanPartyBonus > 0) number.Add(clanPartyBonus, GameTexts.FindText("tor_custom_resource_bre_clanParties"));
                }

                var chivalryLevel = hero.GetChivalryLevel();
                if (chivalryLevel == ChivalryLevel.Honourable)
                {
                    number.Add(5, ChivalryHelper.GetChivalryRankText(chivalryLevel));
                }

                if (chivalryLevel == ChivalryLevel.Chivalrous)
                {
                    number.Add(15, ChivalryHelper.GetChivalryRankText(chivalryLevel));
                }
            }
        }

        //upkeep after to manipulate gains and deductions separately
        var upkeep = GetCalculatedCustomResourceUpkeep(hero, resourceid);
        if (upkeep.ResultNumber < 0)
        {
            number.AddFromExplainedNumber(upkeep, UPKEEPTEXT);
        }

        return number;
    }

    //Sly : do we want this capable of holding descriptions allowing a detailed breakdown when holding alt?
    //this would probably cause issues for a dwarf player with the size of the UI element, but it may make it more evident for players how different sources of bonuses are contributing
    public ExplainedNumber GetCalculatedCustomResourceUpkeep(Hero hero, string resourceID = "")
    {
        if (resourceID == "")
        {
            resourceID = hero.GetCultureSpecificCustomResource().StringId;
        }


        var upkeep = new ExplainedNumber(0, true, UPKEEPTEXT);

        foreach (var element in hero.PartyBelongedTo.MemberRoster.GetTroopRoster())
        {
            if (element.Character.HasCustomResourceUpkeepRequirement())
            {
                var resource = element.Character.GetCustomResourceRequiredForUpkeep();

                if (resource.Item1.StringId != resourceID) continue;
                var unitUpkeep = new ExplainedNumber(resource.Item2 * element.Number);
                if (hero == Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(hero, ref unitUpkeep, PassiveEffectType.CustomResourceUpkeepModifier, true, element.Character);

                    if (hero.Culture.StringId == TORConstants.Cultures.ASRAI)
                    {

                        if (hero.HasAttribute("WETreekinSymbol") && !element.Character.IsElf() && element.Character.Culture.StringId == TORConstants.Cultures.ASRAI)
                        {
                            unitUpkeep.AddFactor(-0.5f, ForestHarmonyHelper.TreeSymbolText("WETreekinSymbol"));
                        }

                        if (hero.HasAttribute("WEOrionSymbol") && !element.Character.IsElf() && element.Character.Culture.StringId == TORConstants.Cultures.ASRAI)
                        {
                            unitUpkeep.AddFactor(1f, ForestHarmonyHelper.TreeSymbolText("WEOrionSymbol"));
                        }
                    }

                    if (hero.Culture.StringId == TORConstants.Cultures.SYLVANIA || hero.Culture.StringId == TORConstants.Cultures.MOUSILLON)
                    {
                        if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.Army != null && hero.PartyBelongedTo.Army.LeaderParty != MobileParty.MainParty)
                        {
                            unitUpkeep.AddFactor(-0.5f, GameTexts.FindText("tor_generic_armyMember"));
                        }
                        else if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.BesiegedSettlement != null)
                        {
                            unitUpkeep.AddFactor(-0.5f, GameTexts.FindText("tor_generic_siegeCampBonus"));
                        }
                    }
                }

                upkeep.Add(-unitUpkeep.ResultNumber, UPKEEPTEXT);

            }
        }
        if (!hero.IsClanLeader) return upkeep;
        
        foreach (var settlement in hero.Clan.Settlements)
        {
            if (!settlement.IsCastle && !settlement.IsTown) continue;
            if (settlement.Town.GarrisonParty == null) continue;
            var garrison = settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster();
            foreach (var elem in garrison)
            {
                if (elem.Character.HasCustomResourceUpkeepRequirement())
                {
                    var resource = elem.Character.GetCustomResourceRequiredForUpkeep();

                    if (resource == null) continue;

                    if (resource.Item1.StringId != resourceID) continue;
                    var garrisonFactor = 0.25f; //base reduction bonus 
                    var garrisonUnitUpkeep = new ExplainedNumber(resource.Item2 * elem.Number * garrisonFactor);

                    if (hero == Hero.MainHero)
                    {
                        CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref garrisonUnitUpkeep, PassiveEffectType.CustomResourceUpkeepModifier, true, elem.Character);
                    }

                    upkeep.Add(-garrisonUnitUpkeep.ResultNumber, GameTexts.FindText("tor_generic_garrisonUpkeep"));
                }
            }
        }

        return upkeep;
    }

    public float GetFactorForGeneralizedCosts(CustomResource resource)
    {

        switch (resource.StringId)
        {
            case "Prestige":
            case "CouncilFavor":
            case "OathGold":
                return 1;
            case "Chivalry":
                return 2;
            case "ForestHarmony":
            case "DarkEnergy":
                return 3;
        }

        return 1;
    }


}