using NLog;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Utilities;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.Models
{
    public class TORCharacterStatsModel : DefaultCharacterStatsModel
    {
        public override int MaxCharacterTier => 9;

        public override ExplainedNumber MaxHitpoints(CharacterObject character, bool includeDescriptions = false)
        {
            var number = base.MaxHitpoints(character, includeDescriptions);
            number = CalculateHitPoints(ref number, character);
            return number;
        }

        private ExplainedNumber CalculateHitPoints(ref ExplainedNumber number, CharacterObject character)
        {
            if (character.IsMinotaur())
            {
                number.Add(350f, TORTextHelper.GetTextObject("tor_stats_minotaur_bonus_text", "Minotaur bonus"));
            }
            if (character.IsTroll())
            {
                number.Add(450f, TORTextHelper.GetTextObject("tor_stats_troll_bonus_text", "Troll bonus"));
            }
            if (character.IsTreeman())
            {
                number.Add(1000f, TORTextHelper.GetTextObject("tor_stats_large_monster_text", "Large Monster"));
            }
            else if (character.IsTreeSpirit())
            {
                number.Add(100f, TORTextHelper.GetTextObject("tor_stats_dryad_bonus_text", "Dryad bonus"));
            }
            if (character.IsHero)
            {
                return CalculateHeroHealth(ref number, character.HeroObject);
            }
            else
            {
                return CalculateTroopHealth(ref number, character);
            }
        }

        private ExplainedNumber CalculateTroopHealth(ref ExplainedNumber number, CharacterObject character)
        {
            switch (character.Tier)
            {
                case 0:
                    number.Add(-15);
                    break;
                case 1:
                case 2:
                case 3:
                    break;
                case 4:
                    number.Add(20);
                    break;
                default:
                    number.Add(character.Tier * 10);
                    break;
            }
            if (character.IsUndead() && !character.IsHero && !character.HasAttribute("NecromancerChampion"))
            {
                number.Add(-25);
            }

            if (character.IsDwarf())
            {
                number.Add(20);
            }
            if (character.IsGoblin())
            {
                number.Add(-20);
            }
            if (character.IsOrc())
            {
                number.Add(40);
            }

            if (character.HasAttribute("NecromancerChampion"))
            {
                if (Mission.Current == null) return number;
                var playerMainAgent = Mission.Current.MainAgent;
                var value = playerMainAgent?.GetComponent<AbilityComponent>()?.CareerAbility?.Template?.ScaleVariable1;
                if (value == null)
                {
                    TORCommon.Log("Necromancer champion can't find player necromancer's ability scaling. Player may have died or one was cheated in. Champion being set to 1 hp and being left to die.", LogLevel.Info);
                    if (playerMainAgent == null) TORCommon.Log("Player agent null.", LogLevel.Info);
                    else if (playerMainAgent.GetComponent<AbilityComponent>() == null) TORCommon.Log("Component null.", LogLevel.Info);
                    else if (playerMainAgent.GetComponent<AbilityComponent>().CareerAbility == null) TORCommon.Log("Career ability null.", LogLevel.Info);
                    else if (playerMainAgent.GetComponent<AbilityComponent>().CareerAbility.Template == null) TORCommon.Log("Template null.", LogLevel.Info);
                    value = number.ResultNumber - 1;
                }
                number.Add((int)value);
            }

            return number;
        }

        private ExplainedNumber CalculateHeroHealth(ref ExplainedNumber number, Hero hero)
        {
            var info = hero.GetExtendedInfo();
            if (info != null)
            {
                if (info.AcquiredAttributes.Contains("Tier1"))
                {
                    number.Add(100, TORTextHelper.GetTextObject("tor_stats_tier1_text", "Tier1"));
                }
                else if (info.AcquiredAttributes.Contains("Tier2"))
                {
                    number.Add(150, TORTextHelper.GetTextObject("tor_stats_tier2_text", "Tier2"));
                }
                else if (info.AcquiredAttributes.Contains("Tier3"))
                {
                    number.Add(200, TORTextHelper.GetTextObject("tor_stats_tier3_text", "Tier3"));
                }
                else if (info.AcquiredAttributes.Contains("Tier4"))
                {
                    number.Add(300, TORTextHelper.GetTextObject("tor_stats_tier4_text", "Tier4"));
                }
                if (hero.IsVampire() && !hero.IsHumanPlayerCharacter)
                {
                    number.Add(100, TORTextHelper.GetTextObject("tor_stats_vampire_body_text", "Vampire body"));
                }

                if (hero.HasAttribute("Everchosen"))
                {
                    number.Add(2000);
                }

                if (hero.IsDwarf())
                {
                    number.Add(20);
                }
                if (hero.IsGoblin())
                {
                    number.Add(-20);
                }
                if (hero.IsOrc())
                {
                    number.Add(40);
                }

                if (hero.HasAttribute("Tough"))
                {
                    number.Add(100);
                }

                if (hero.HasAnyCareer())
                {
                    CareerHelper.ApplyBasicCareerPassives(hero, ref number, PassiveEffectType.Health, false);
                }

                if (hero.IsPlayerCompanion)
                {
                    if (Hero.MainHero.HasCareerChoice("GuiltyByAssociationPassive3"))
                    {
                        var choice = TORCareerChoices.GetChoice("GuiltyByAssociationPassive3");
                        number.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                    }

                    if (Hero.MainHero.HasCareerChoice("CommanderPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("CommanderPassive4");
                        number.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                    }

                    if (Hero.MainHero.HasCareerChoice("EnvoyOfTheLadyPassive2"))
                    {
                        if (hero.IsBretonnianKnight())
                        {
                            var choice = TORCareerChoices.GetChoice("EnvoyOfTheLadyPassive2");
                            number.AddFactor(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                        }
                    }

                    if (Hero.MainHero.HasCareerChoice("HolyCrusaderPassive2") &&
                        hero.PartyBelongedTo == MobileParty.MainParty &&
                        hero.IsBretonnianKnight())
                    {
                        var choice = TORCareerChoices.GetChoice("HolyCrusaderPassive2");
                        var knightCompanions = MobileParty.MainParty.GetMemberHeroes().Where(knight => knight.IsBretonnianKnight());
                        var extraHealth = knightCompanions.Count() * choice.GetPassiveValue();
                        number.Add(extraHealth, choice.BelongsToGroup.Name);
                    }

                    if (Hero.MainHero.HasCareerChoice("ForHearthAndHomePassive2"))
                    {
                        var equipment = hero.CharacterObject.GetCharacterEquipment();
                        var choice = TORCareerChoices.GetChoice("ForHearthAndHomePassive2");
                        var extraHealth = equipment.WhereQ(item => item.HasAnyTrait()).Sum(_ => (int)choice.GetPassiveValue());

                        number.Add(extraHealth, choice.BelongsToGroup.Name);
                    }

                    // BestofDaBestPassive4: Orc Big Bosses gain 100 health
                    if (Hero.MainHero.HasCareerChoice("BestofDaBestPassive4"))
                    {
                        if (hero.HasAttribute("BigBoss"))
                        {
                            var choice = TORCareerChoices.GetChoice("BestofDaBestPassive4");
                            number.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                        }
                    }

                    // Orc Shaman: +70 HP for Shaman Boss companion
                    if (Hero.MainHero.HasCareerChoice("GorkAnMorkAreWatchinPassive4"))
                    {
                        if (hero.HasAttribute("ShamanBoss"))
                        {
                            var choice = TORCareerChoices.GetChoice("GorkAnMorkAreWatchinPassive4");
                            number.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                        }
                    }

                }

                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI && hero.PartyBelongedTo != null && (hero.PartyBelongedTo.IsMainParty || hero == Hero.MainHero))
                {

                    if (!Hero.MainHero.HasAttribute("WEWandererSymbol"))
                    {
                        var level = hero.PartyBelongedTo.LeaderHero.GetForestHarmonyLevel();
                        switch (level)
                        {
                            case ForestHarmonyLevel.Harmony: break;
                            case ForestHarmonyLevel.Unbound:
                                number.AddFactor(ForestHarmonyHelper.HealthDebuffUnBound, GameTexts.FindText("tor_forest_harmony_level", ForestHarmonyLevel.Unbound.ToString()));
                                break;
                            case ForestHarmonyLevel.Bound:
                                number.AddFactor(ForestHarmonyHelper.HealthDebuffBound, GameTexts.FindText("tor_forest_harmony_level", ForestHarmonyLevel.Bound.ToString()));
                                break;
                        }
                    }

                    var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
                    var list = settlementBehavior.GetUnlockedOakUpgradeCategory("WEHealthUpgrade");
                    foreach (var attribute in list)
                    {
                        number.AddFactor(0.1f, new TextObject("Oak of Ages"));
                    }


                    if (Hero.MainHero.HasAttribute("WEWardancerSymbol"))
                    {
                        number.AddFactor(0.25f, ForestHarmonyHelper.TreeSymbolText("WEWardancerSymbol"));
                    }

                    if (hero == Hero.MainHero && Hero.MainHero.HasAttribute("WEDurthuSymbol"))
                    {
                        number.AddFactor(0.10f);
                    }

                }


                if (hero.HasAttribute("GiftOfNurgle")) number.Add(20, new TextObject("Gift of Nurgle"));
            }
            if (hero.GetPerkValue(TORPerks.Faith.Devotee))
            {
                number.Add(TORPerks.Faith.Devotee.PrimaryBonus * hero.GetAttributeValue(TORAttributes.Discipline), new TextObject("Perks"));
            }

            var healthFromEquipment = hero.GetAggregatedStatEffectFromEquipment(ItemTraitStatType.HealthMax);
            if (healthFromEquipment > 0)
            {
                number.Add(healthFromEquipment, GameTexts.FindText("tor_generic_enchantedEquipment"));
            }

            var model = Campaign.Current.Models.CampaignTimeModel;
            if ((bool)(model?.CampaignStartTime.IsNow))
            {
                hero.HitPoints = (int)number.ResultNumber;
            }
            return number;
        }
    }
}