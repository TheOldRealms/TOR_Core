using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models
{
    public class TORCharacterStatsModel : DefaultCharacterStatsModel
    {
        /// <summary>
        /// Which health rules a hero is subject to. Troops are the fourth category and never
        /// reach here - they branch out at <see cref="CalculateHitPoints"/>.
        /// </summary>
        private enum HeroKind
        {
            Player,
            Companion,
            Lord
        }

        public override int MaxCharacterTier => 9;

        public override ExplainedNumber MaxHitpoints(CharacterObject character, bool includeDescriptions = false)
        {
            var number = base.MaxHitpoints(character, includeDescriptions);
            CalculateHitPoints(ref number, character);
            return number;
        }

        private void CalculateHitPoints(ref ExplainedNumber number, CharacterObject character)
        {
            AddRaceHealth(ref number, character);
            if (character.IsHero)
            {
                CalculateHeroHealth(ref number, character.HeroObject);
            }
            else
            {
                CalculateTroopHealth(ref number, character);
            }
        }

        /// <summary>
        /// Race bonuses apply to every category alike - troop, player, companion and lord.
        /// </summary>
        private void AddRaceHealth(ref ExplainedNumber number, CharacterObject character)
        {
            if (character.IsMinotaur())
            {
                number.Add(350f, TORTextHelper.GetTextObject("tor_stats_minotaur_bonus_text", "Minotaur bonus"));
            }
            if (character.IsTroll())
            {
                number.Add(450f, TORTextHelper.GetTextObject("tor_stats_troll_bonus_text", "Troll bonus"));
            }
            // Treemen carry the TreeSpirit attribute as well as the large_humanoid_monster race
            // (tor_troopdefinitions.xml / tor_extendedunitproperties.xml), so this stays an else:
            // they take the monster bonus instead of the dryad one, not both.
            if (character.IsTreeman())
            {
                number.Add(1000f, TORTextHelper.GetTextObject("tor_stats_large_monster_text", "Large Monster"));
            }
            else if (character.IsTreeSpirit())
            {
                number.Add(100f, TORTextHelper.GetTextObject("tor_stats_dryad_bonus_text", "Dryad bonus"));
            }
            if (character.IsDwarf())
            {
                number.Add(20, TORTextHelper.GetTextObject("tor_stats_dwarf_bonus_text", "Dwarf bonus"));
            }
            if (character.IsGoblin())
            {
                number.Add(-20, TORTextHelper.GetTextObject("tor_stats_goblin_bonus_text", "Goblin frailty"));
            }
            if (character.IsOrc())
            {
                number.Add(40, TORTextHelper.GetTextObject("tor_stats_orc_bonus_text", "Orc bonus"));
            }
        }

        private void CalculateTroopHealth(ref ExplainedNumber number, CharacterObject character)
        {
            var tierText = TORTextHelper.GetTextObject("tor_stats_troop_tier_text", "Troop tier");
            switch (character.Tier)
            {
                case 0:
                    number.Add(-15, tierText);
                    break;
                case 1:
                case 2:
                case 3:
                    break;
                case 4:
                    number.Add(20, tierText);
                    break;
                default:
                    number.Add(character.Tier * 10, tierText);
                    break;
            }
            if (character.IsUndead() && !character.HasAttribute(CharacterAttributes.NECROMANCER_CHAMPION))
            {
                number.Add(-25, TORTextHelper.GetTextObject("tor_stats_undead_body_text", "Undead body"));
            }

            // Must stay last: the champion's health is derived from the running total.
            if (character.HasAttribute(CharacterAttributes.NECROMANCER_CHAMPION))
            {
                if (Mission.Current == null) return;
                var playerMainAgent = Mission.Current.MainAgent;
                var value = playerMainAgent?.GetComponent<AbilityComponent>()?.CareerAbility?.Template?.ScaleVariable1;
                if (value == null)
                {
                    TORCommon.Log("Necromancer champion can't find player necromancer's ability scaling. Player may have died or one was cheated in. Champion being set to 1 hp and being left to die.", LogLevel.Info);
                    if (playerMainAgent == null) TORCommon.Log("Player agent null.", LogLevel.Info);
                    else if (playerMainAgent.GetComponent<AbilityComponent>() == null) TORCommon.Log("Component null.", LogLevel.Info);
                    else if (playerMainAgent.GetComponent<AbilityComponent>().CareerAbility == null) TORCommon.Log("Career ability null.", LogLevel.Info);
                    else if (playerMainAgent.GetComponent<AbilityComponent>().CareerAbility.Template == null) TORCommon.Log("Template null.", LogLevel.Info);
                    number = new ExplainedNumber(1f);
                    return;
                }
                number.Add((int)value, TORTextHelper.GetTextObject("tor_stats_necromancer_champion_text", "Necromantic empowerment"));
            }
        }

        private void CalculateHeroHealth(ref ExplainedNumber number, Hero hero)
        {
            var info = hero.GetExtendedInfo();
            if (info != null)
            {
                AddCommonHeroHealth(ref number, hero, info);

                switch (GetHeroKind(hero))
                {
                    case HeroKind.Player:
                        // Nothing player-exclusive outside AddAsraiHealth today.
                        break;
                    case HeroKind.Companion:
                        AddCompanionCareerHealth(ref number, hero);
                        break;
                    case HeroKind.Lord:
                        // AI heroes receive the common block only.
                        break;
                }

                // Keyed on party membership rather than hero kind: clan lords riding with the
                // player are not IsPlayerCompanion but are still subject to forest harmony.
                AddAsraiHealth(ref number, hero);
            }
            AddUniversalHeroHealth(ref number, hero);
        }

        private static HeroKind GetHeroKind(Hero hero)
        {
            if (hero == Hero.MainHero) return HeroKind.Player;
            if (hero.IsPlayerCompanion) return HeroKind.Companion;
            return HeroKind.Lord;
        }

        /// <summary>
        /// Applies to every hero that has extended info, whatever their kind.
        /// </summary>
        private void AddCommonHeroHealth(ref ExplainedNumber number, Hero hero, HeroExtendedInfo info)
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

            if (hero.HasAttribute(CharacterAttributes.EVERCHOSEN))
            {
                number.Add(2000, TORTextHelper.GetTextObject("tor_stats_everchosen_text", "Everchosen"));
            }

            if (hero.IsOrion())
            {
                number.Add(3000, TORTextHelper.GetTextObject("tor_stats_orion_text", "Orion"));
            }

            if (hero.HasAttribute(CharacterAttributes.TOUGH))
            {
                number.Add(100, TORTextHelper.GetTextObject("tor_stats_tough_text", "Tough"));
            }

            if (hero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(hero, ref number, PassiveEffectType.Health, false);
            }

            if (hero.HasAttribute(CharacterAttributes.GIFT_OF_NURGLE))
            {
                number.Add(20, TORTextHelper.GetTextObject("tor_stats_gift_of_nurgle_text", "Gift of Nurgle"));
            }
        }

        /// <summary>
        /// The player's own career choices, spent on their companions.
        /// </summary>
        private void AddCompanionCareerHealth(ref ExplainedNumber number, Hero hero)
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
                var knightCount = MobileParty.MainParty.GetMemberHeroes().CountQ(knight => knight.IsBretonnianKnight());
                number.Add(knightCount * choice.GetPassiveValue(), choice.BelongsToGroup.Name);
            }

            if (Hero.MainHero.HasCareerChoice("ForHearthAndHomePassive2"))
            {
                var equipment = hero.CharacterObject.GetCharacterEquipment();
                var choice = TORCareerChoices.GetChoice("ForHearthAndHomePassive2");
                var traitedItemCount = equipment.CountQ(item => item.HasAnyTrait());
                number.Add(traitedItemCount * (int)choice.GetPassiveValue(), choice.BelongsToGroup.Name);
            }

            // BestofDaBestPassive4: Orc Big Bosses gain 100 health
            if (Hero.MainHero.HasCareerChoice("BestofDaBestPassive4"))
            {
                if (hero.HasAttribute(CharacterAttributes.BIG_BOSS))
                {
                    var choice = TORCareerChoices.GetChoice("BestofDaBestPassive4");
                    number.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                }
            }

            // Orc Shaman: +70 HP for Shaman Boss companion
            if (Hero.MainHero.HasCareerChoice("GorkAnMorkAreWatchinPassive4"))
            {
                if (hero.HasAttribute(CharacterAttributes.SHAMAN_BOSS))
                {
                    var choice = TORCareerChoices.GetChoice("GorkAnMorkAreWatchinPassive4");
                    number.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                }
            }
        }

        /// <summary>
        /// Wood elf forest harmony, Oak of Ages upgrades and tree symbols. Reaches the player and
        /// anyone travelling in their party.
        /// </summary>
        private void AddAsraiHealth(ref ExplainedNumber number, Hero hero)
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI && hero.PartyBelongedTo != null && (hero.PartyBelongedTo.IsMainParty || hero == Hero.MainHero))
            {

                if (!Hero.MainHero.HasAttribute(CharacterAttributes.WE_WANDERER_SYMBOL))
                {
                    // Forest harmony is a player-owned resource everywhere else it is read
                    // (ForestHarmonyHelper.GetForestHarmonyInfo), and the main party's leader is
                    // always the player - reading it directly also removes a null LeaderHero path.
                    var level = Hero.MainHero.GetForestHarmonyLevel();
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

                // 10% per unlocked upgrade, as one tooltip line rather than one per upgrade.
                var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
                var oakUpgrades = settlementBehavior?.GetUnlockedOakUpgradeCategory("WEHealthUpgrade");
                if (oakUpgrades != null && oakUpgrades.Count > 0)
                {
                    number.AddFactor(0.1f * oakUpgrades.Count, TORTextHelper.GetTextObject("tor_stats_oak_of_ages_text", "Oak of Ages"));
                }

                if (Hero.MainHero.HasAttribute(CharacterAttributes.WE_WARDANCER_SYMBOL))
                {
                    number.AddFactor(0.25f, ForestHarmonyHelper.TreeSymbolText("WEWardancerSymbol"));
                }

                if (hero == Hero.MainHero && Hero.MainHero.HasAttribute(CharacterAttributes.WE_DURTHU_SYMBOL))
                {
                    number.AddFactor(0.10f, ForestHarmonyHelper.TreeSymbolText("WEDurthuSymbol"));
                }
            }
        }

        /// <summary>
        /// Runs for every hero, including those without extended info.
        /// </summary>
        private void AddUniversalHeroHealth(ref ExplainedNumber number, Hero hero)
        {
            if (hero.GetPerkValue(TORPerks.Faith.Devotee))
            {
                number.Add(TORPerks.Faith.Devotee.PrimaryBonus * hero.GetAttributeValue(TORAttributes.Discipline), TORTextHelper.GetTextObject("tor_stats_perks_text", "Perks"));
            }

            var healthFromEquipment = hero.GetAggregatedStatEffectFromEquipment(ItemTraitStatType.HealthMax);
            if (healthFromEquipment > 0)
            {
                number.Add(healthFromEquipment, GameTexts.FindText("tor_generic_enchantedEquipment"));
            }

            var model = Campaign.Current.Models.CampaignTimeModel;
            if (model != null && model.CampaignStartTime.IsNow)
            {
                hero.HitPoints = (int)number.ResultNumber;
            }
        }
    }
}
