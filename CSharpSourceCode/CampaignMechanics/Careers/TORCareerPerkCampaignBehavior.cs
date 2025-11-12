using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORCareerPerkCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyCareerTickEvents);
            CampaignEvents.ItemsLooted.AddNonSerializedListener(this, RaidingPartyEvent);
            CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, PlayerRecruitmentEvent);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, PostBattleEvents);
            TORCampaignEvents.Instance.ItemDuplicated += OnItemDuplicated;
            CampaignEvents.OnEquipmentSmeltedByHeroEvent.AddNonSerializedListener(this, SmeltingPartyEvent);
            CampaignEvents.OnNewItemCraftedEvent.AddNonSerializedListener(this, OnSmithedItem);
            CampaignEvents.OnItemsRefinedEvent.AddNonSerializedListener(this, RefinedItemEvent);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyPartyEvent);
        }

        private void HourlyPartyEvent(MobileParty party)
        {
            if (!party.IsMainParty) return;

            if (party.CurrentSettlement != null) return;
            if (Hero.MainHero.HasCareerChoice("ForgefireBurningPassive3"))
            {
                var campaignBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();

                foreach (var hero in party.GetMemberHeroes())
                {
                    var stamina = campaignBehavior.GetHeroCraftingStamina(hero);
                    var max = campaignBehavior.GetMaxHeroCraftingStamina(hero);
                    if (stamina >= max)
                        return;
                    var value = Math.Min(max, stamina + 4);
                    campaignBehavior.SetHeroCraftingStamina(hero, value);
                }
            }
        }

        private void RefinedItemEvent(Hero hero, TaleWorlds.Core.Crafting.RefiningFormula arg2)
        {
            if (Hero.MainHero.HasCareer(TORCareers.Runelord))
            {
                var value = 0;
                switch (arg2.Output)
                {
                    case CraftingMaterials.Iron1:
                    case CraftingMaterials.Iron2:
                        value = 10;
                        break;
                    case CraftingMaterials.Iron3:
                    case CraftingMaterials.Iron4:
                        value = 20;
                        break;
                    case CraftingMaterials.Iron5:
                    case CraftingMaterials.Iron6:
                        value = 30;
                        break;
                }

                RuneSmithForgeFirePerk(hero, value);
            }
        }

        private void OnSmithedItem(ItemObject item, ItemModifier modifier, bool crafted)
        {
            if (Hero.MainHero.HasCareer(TORCareers.Runelord))
            {
                var value = item.Value / 10;
                RuneSmithForgeFirePerk(Hero.MainHero, value);
            }
        }

        private void SmeltingPartyEvent(Hero hero, EquipmentElement equipmentElement)
        {
            if (Hero.MainHero.HasCareer(TORCareers.Runelord))
            {
                var value = equipmentElement.Item.Value / 10;
                RuneSmithForgeFirePerk(hero, value);
            }
        }

        private void RuneSmithForgeFirePerk(Hero hero, int value)
        {
            if (Hero.MainHero.HasCareerChoice("ForgefireBurningPassive4")) hero.AddSkillXp(TORSkills.Faith, value);
        }

        private void OnItemDuplicated(object sender, ItemDuplicatedEventArgs e)
        {
            if (Hero.MainHero.HasCareer(TORCareers.Runelord) && Hero.MainHero.HasCareerChoice("TeachingsOfThungniPassive2"))
            {
                if (!e.NewItem.IsCraftedByPlayer) return;

                var list = ItemTrait.All.Where(x => e.Traits.Contains(x.ItemTraitStringId)).ToList();
                var value = 0;
                foreach (var trait in list)
                {
                    switch (trait.IngredientItem)
                    {
                        case TorTradeGoodType.ArcaneScroll:
                        case TorTradeGoodType.GemStone:
                        case TorTradeGoodType.BlessedWater:
                        case TorTradeGoodType.AmberCrystal:
                        case TorTradeGoodType.WarpstoneDust:
                            value += 3;
                            break;
                        case TorTradeGoodType.DragonBlood:
                            value += 10;
                            break;
                    }

                    value *= trait.IngredientAmount;
                }

                Hero.MainHero.AddSkillXp(DefaultSkills.Crafting, value * 100);
                Hero.MainHero.AddSkillXp(TORSkills.SpellCraft, value * 100);
            }
        }


        private void PostBattleEvents(MapEvent mapEvent)
        {
            CheckWarriorPriestPerks(mapEvent);

            if (Hero.MainHero.HasCareerChoice("SecretOfFellfangPassive3"))
            {
                var choice = TORCareerChoices.GetChoice("SecretOfFellfangPassive3");
                var abilityModel = Campaign.Current.Models.GetAbilityModel();
                var maximum = abilityModel.GetMaximumWindsOfMagic(Hero.MainHero.CharacterObject);

                var postBattleBonus = maximum * choice.GetPassiveValue();

                Hero.MainHero.AddWindsOfMagic(postBattleBonus);
            }
        }

        private void CheckWarriorPriestPerks(MapEvent mapEvent)
        {
            if (Hero.MainHero.HasCareerChoice("BookOfSigmarPassive3"))
            {
                var playerParty = MobileParty.MainParty;
                if (playerParty == null) return; //PlayerBattleEndEvent shouldn't trigger on a loss so this should never be null
                var heroes = playerParty.GetMemberHeroes();
                foreach (var hero in heroes.Where(hero => !hero.IsHealthFull()))
                {
                    var choice = TORCareerChoices.GetChoice("BookOfSigmarPassive3");

                    hero.Heal((int)choice.GetPassiveValue());
                }
            }
        }

        private void PlayerRecruitmentEvent(CharacterObject characterObject, int amount)
        {
            if (Hero.MainHero.HasAnyCareer())
            {
                var party = Hero.MainHero.PartyBelongedTo;
                var choices = Hero.MainHero.GetAllCareerChoices();

                if (choices.Contains("PaymasterPassive2"))
                {
                    var choice = TORCareerChoices.GetChoice("PaymasterPassive2");
                    if (choice != null)
                        AddExtraTroopsWithChanceIfPossible(characterObject, amount, party, choice.GetPassiveValue());
                }

                if (choices.Contains("InspirationOfTheLadyPassive1"))
                {
                    var choice = TORCareerChoices.GetChoice("InspirationOfTheLadyPassive1");
                    if (choice != null)
                        AddExtraTroopsWithChanceIfPossible(characterObject, amount, party, choice.GetPassiveValue());
                }

                if (choices.Contains("MonsterSlayerPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("MonsterSlayerPassive3");
                    if (choice != null)
                        AddExtraTroopsWithChanceIfPossible(characterObject, amount, party, choice.GetPassiveValue());
                }

                if (choices.Contains("UnbreakableArmyPassive2"))
                {
                    var choice = TORCareerChoices.GetChoice("UnbreakableArmyPassive2");
                    if (choice != null)
                        AddExtraTroopsWithChanceIfPossible(characterObject, amount, party, choice.GetPassiveValue());
                }
            }
        }

        private void AddExtraTroopsWithChanceIfPossible(CharacterObject troop, int originalAmount, MobileParty party, float chance)
        {
            for (var i = 0; i < originalAmount; i++)
            {
                if (party.Party.PartySizeLimit - party.Party.NumberOfAllMembers + 1 < 0) break;

                if (MBRandom.RandomFloatRanged(0, 1) < chance) party.AddElementToMemberRoster(troop, 1);
            }
        }

        private void RaidingPartyEvent(MobileParty mobileParty, ItemRoster itemRoster)
        {
            if (mobileParty != MobileParty.MainParty && mobileParty.MapEvent.IsRaid) return;

            if (mobileParty == null || mobileParty.LeaderHero != Hero.MainHero) return;
            if (!MobileParty.MainParty.LeaderHero.HasAnyCareer()) return;

            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (choices.Contains("BladeMasterPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("BladeMasterPassive4");
                if (choice == null) return;
                var memberList = mobileParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
                    if (member.Character.IsVampire() && !member.Character.IsHero)
                    {
                        mobileParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                    }
                    else if (member.Character.IsHero && member.Character.IsVampire() && member.Character.IsHero)
                    {
                        var character = member.Character.HeroObject;
                        var skills = new List<SkillObject>
                        {
                            DefaultSkills.OneHanded,
                            DefaultSkills.TwoHanded,
                            DefaultSkills.Polearm,
                            DefaultSkills.Riding,
                            DefaultSkills.Tactics
                        };
                        var targetSkill = skills.GetRandomElement();
                        character.AddSkillXp(targetSkill, choice.GetPassiveValue());
                    }
                }
            }
        }

        private void DailyCareerTickEvents()
        {
            var mainParty = MobileParty.MainParty;
            if (!mainParty.LeaderHero.HasAnyCareer()) return;
            var choices = mainParty.LeaderHero.GetAllCareerChoices();

            if (choices.Contains("SurvivalistPassive4") || choices.Contains("ForestStalkerPassive1")) LaunchHuntingEvent(mainParty);

            if (choices.Contains("PeerlessWarriorPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("PeerlessWarriorPassive4");
                if (choice != null)
                {
                    var skills = new List<SkillObject> { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm };
                    var targetSkill = skills.GetRandomElement();
                    Hero.MainHero.AddSkillXp(targetSkill, choice.GetPassiveValue());
                }
            }

            if (choices.Contains("ErrantryWarPassive4"))
            {
                var memberList = mainParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
                    if (!member.Character.IsRanged && !member.Character.IsHero)
                    {
                        var choice = TORCareerChoices.GetChoice("ErrantryWarPassive4");
                        if (choice != null)
                            mainParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                    }
                }
            }

            if (choices.Contains("JustCausePassive2"))
            {
                var memberList = mainParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
                    if (!member.Character.IsKnightUnit())
                    {
                        var choice = TORCareerChoices.GetChoice("JustCausePassive2");
                        if (choice != null)
                            mainParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                    }
                }
            }

            if (choices.Contains("HailOfArrowsPassive2"))
            {
                var memberList = mainParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
                    if (!member.Character.IsKnightUnit())
                    {
                        var choice = TORCareerChoices.GetChoice("HailOfArrowsPassive2");
                        if (choice != null)
                            mainParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                    }
                }
            }

            if (choices.Contains("CurseOfMousillonPassive4"))
            {
                var heroes = mainParty.GetMemberHeroes();
                var chance = 0.0f + heroes.Where(hero => hero.HasAttribute("IllFated")).Sum(hero => 0.1f);

                if (chance <= 0.0f) return;

                var moralebonus = mainParty.Morale / 200;

                chance += moralebonus;

                var memberList = mainParty.MemberRoster.GetTroopRoster();

                var bretonnes = memberList.FindAll(x => !x.Character.IsEliteTroop() && x.Character.Culture.StringId == TORConstants.Cultures.BRETONNIA);

                for (var index = 0; index < bretonnes.Count; index++)
                {
                    var member = bretonnes[index];
                    for (var i = 0; i < member.Number; i++)
                    {
                        var randomFloat = MBRandom.RandomFloat;

                        if (randomFloat >= chance) continue;

                        var mousillonEquivalent = TORRecruitmentHelpers.GetMousillonEquivalent(member.Character);

                        if (mousillonEquivalent == null) continue;

                        mainParty.AddElementToMemberRoster(mousillonEquivalent, 1);
                        mainParty.AddElementToMemberRoster(member.Character, -1);
                    }
                }
            }
        }

        private static void LaunchHuntingEvent(MobileParty mobileParty)
        {
            if (mobileParty.CurrentSettlement != null || mobileParty.Army != null) return;

            var faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
            var scoutingValue = mobileParty.LeaderHero.GetSkillValue(DefaultSkills.Scouting);
            var wieldedWeaponValue = 0;
            var weaponSkill = DefaultSkills.Bow;
            var weapons = mobileParty.LeaderHero.CharacterObject.GetCharacterEquipment(EquipmentIndex.WeaponItemBeginSlot, EquipmentIndex.Weapon3)
                .Where(x => x.WeaponComponent.PrimaryWeapon.IsRangedWeapon || x.PrimaryWeapon.RelevantSkill == DefaultSkills.Polearm);
            if (weapons.IsEmpty()) return;
            var wielded = weapons.ToList().MaxBy(x => mobileParty.LeaderHero.GetSkillValue(x.RelevantSkill));
            if (wielded == null) return;

            weaponSkill = wielded.PrimaryWeapon.RelevantSkill;
            wieldedWeaponValue = mobileParty.LeaderHero.GetSkillValue(wielded.PrimaryWeapon.RelevantSkill);

            var PreyChance = (float)scoutingValue / 300 * 0.9f;
            if (faceTerrainType == TerrainType.Forest)
                PreyChance += 0.1f;

            var huntSucess = (scoutingValue + wieldedWeaponValue) / 600f;
            if (mobileParty.HasBlessing("cult_of_taal"))
                huntSucess *= 1.1f;

            var PreySize = MBRandom.RandomInt(1, 3);
            if (MBRandom.RandomFloatRanged(0, 1) >= PreyChance)
            {
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("tor_hunt_perk_result", "CompletelyFailed").ToString(),
                    Colors.Yellow));
                return;
            }

            mobileParty.LeaderHero.AddSkillXp(DefaultSkills.Scouting, 50f * PreySize);
            var preySizeAnimalText = "";
            switch (PreySize)
            {
                case 1:
                    preySizeAnimalText = new TextObject("{=tor_hunt_perk_animal_large_str}large Animal").ToString();
                    break;
                case 2:
                    preySizeAnimalText = new TextObject("{=tor_hunt_perk_animal_medium_str}medium Animal").ToString();
                    break;
                case 3:
                    preySizeAnimalText = new TextObject("{=tor_hunt_perk_animal_small_str}small Animal").ToString();
                    break;
            }

            MBTextManager.SetTextVariable("PERK_HUNT_ANIMAL_SIZE", preySizeAnimalText);


            if (MBRandom.RandomFloatRanged(0, 1) >= huntSucess)
            {
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("tor_hunt_perk_result", "Failed").ToString(), Colors.Yellow));
                return;
            }

            var preyText = PreySize > 1
                ? $"({PreySize} {DefaultItems.Meat.Name}, {PreySize}{DefaultItems.Hides.Name})"
                : $"{PreySize} {DefaultItems.Meat.Name}";
            MBTextManager.SetTextVariable("PERK_HUNT_PREY", preyText);

            mobileParty.LeaderHero.AddSkillXp(weaponSkill, 50f * PreySize);
            mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Meat, PreySize));
            if (PreySize > 1)
                mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Hides, PreySize));

            InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("tor_hunt_perk_result", "Success").ToString(), Colors.Yellow));
        }

        ~TORCareerPerkCampaignBehavior()
        {
            TORCampaignEvents.Instance.ItemDuplicated -= OnItemDuplicated;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}