﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class TORWanderersCampaignBehavior : CampaignBehaviorBase
    {
        public override void SyncData(IDataStore dataStore) { }

        public override void RegisterEvents()
        {
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnAfterSettlementEntered);
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, CheckPlayerCurrentSettlement);
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, AddDailySkillXpToCompanions);
            CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
        }

        private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail detail, ref bool result)
        {
            if((hero.IsLord || hero.IsPlayerCompanion || hero.IsWanderer) && detail != KillCharacterAction.KillCharacterActionDetail.Executed)
            {
                result = false;
            }
        }

        private void AddDailySkillXpToCompanions(Hero hero)
        {
            if(hero.IsPartyLeader && !hero.IsPrisoner && hero.PartyBelongedTo != null && hero.CompanionsInParty.Count() > 0 && hero.GetPerkValue(TORPerks.SpellCraft.StoryTeller))
            {
                foreach(var companion in hero.CompanionsInParty)
                {
                    var skills = MBObjectManager.Instance.GetObjectTypeList<SkillObject>();
                    var randomskill = skills.TakeRandom(1).FirstOrDefault();
                    var amount = TORPerks.SpellCraft.StoryTeller.PrimaryBonus;
                    companion.AddSkillXp(randomskill, amount);
                }
            }
        }

        private void CheckPlayerCurrentSettlement()
        {
            var playerSettlement = MobileParty.MainParty.CurrentSettlement;
            if (playerSettlement != null && playerSettlement.IsTown)
            {
                ReplaceEnemyWanderersIfExist(playerSettlement);
            }
        }

        private void OnAfterSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (!settlement.IsTown || mobileParty == null || !mobileParty.IsMainParty)
            {
                return;
            }

            //check for unsuitable wanderers
            ReplaceEnemyWanderersIfExist(settlement);
        }

        private void ReplaceEnemyWanderersIfExist(Settlement settlement)
        {
            for (int i = 0; i < settlement.HeroesWithoutParty.Count; i++)
            {
                var wanderer = settlement.HeroesWithoutParty[i];
                if (wanderer != null && wanderer.Occupation == Occupation.Wanderer && wanderer.Culture != settlement.Culture)
                {
                    //look for empty suitable settlement to move unsuitable wanderer
                    var suitableTown = (from x in Town.AllTowns
                                        where x.Settlement.Culture == wanderer.Culture
                                        orderby x.Settlement.HeroesWithoutParty.Count ascending
                                        select x).FirstOrDefault()
                        ?.Settlement;
                    if (suitableTown != null)
                    {
                        EnterSettlementAction.ApplyForCharacterOnly(wanderer, suitableTown);
                    }
                    else
                    {
                        LeaveSettlementAction.ApplyForCharacterOnly(wanderer);
                        wanderer.ChangeState(Hero.CharacterStates.NotSpawned);
                    }
                }
            }

            if (settlement.HeroesWithoutParty.Where(h => h.Occupation == Occupation.Wanderer).Count() == 0)
            {
                //create suitable wanderer
                CharacterObject template = settlement.Culture.NotableAndWandererTemplates.Where(h => h.Occupation == Occupation.Wanderer).GetRandomElementInefficiently();
                if (template != null)
                {
                    Hero newWanderer = HeroCreator.CreateSpecialHero(template, settlement, null, null, 26 + MBRandom.RandomInt(27));
                    AdjustEquipmentImp(newWanderer.BattleEquipment);
                    AdjustEquipmentImp(newWanderer.CivilianEquipment);
                    newWanderer.ChangeState(Hero.CharacterStates.Active);
                    EnterSettlementAction.ApplyForCharacterOnly(newWanderer, settlement);
                }
            }
        }

        private void AdjustEquipmentImp(Equipment equipment)
        {
            ItemModifier @object = MBObjectManager.Instance.GetObject<ItemModifier>("companion_armor");
            ItemModifier object2 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_weapon");
            ItemModifier object3 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_horse");
            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
            {
                EquipmentElement equipmentElement = equipment[equipmentIndex];
                if (equipmentElement.Item != null)
                {
                    if (equipmentElement.Item.ArmorComponent != null)
                    {
                        equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, @object, null, false);
                    }
                    else if (equipmentElement.Item.HorseComponent != null)
                    {
                        equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, object3, null, false);
                    }
                    else if (equipmentElement.Item.WeaponComponent != null)
                    {
                        equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, object2, null, false);
                    }
                }
            }
        }
    }
}
