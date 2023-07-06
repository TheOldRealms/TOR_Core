﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment
{
    public class TORPerkHandlerCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkPicked);
        }

        private void OnPerkPicked(Hero hero, PerkObject perk)
        {
            var info = hero.GetExtendedInfo();
            if (hero != null && perk != null && !hero.GetPerkValue(perk))
            {
                if(perk == TORPerks.GunPowder.FiringDrills)
                {
                    hero.HeroDeveloper.AddAttribute(TORAttributes.Discipline, 1, false);
                }
                if(perk == TORPerks.Faith.DivineMission)
                {
                    if (hero.HeroDeveloper.CanAddFocusToSkill(DefaultSkills.Medicine))
                    {
                        hero.HeroDeveloper.AddFocus(DefaultSkills.Medicine, 1);
                    }
                    else hero.HeroDeveloper.UnspentFocusPoints += 1;
                }
                if (perk == TORPerks.Faith.ForeSight) hero.HeroDeveloper.UnspentAttributePoints += 1;
                if (info != null)
                {
                    if (hero.IsSpellCaster())
                    {
                        if (perk == TORPerks.SpellCraft.EntrySpells)
                        {
                            if (info.SpellCastingLevel < SpellCastingLevel.Entry)
                                hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                        }
                        if (perk == TORPerks.SpellCraft.AdeptSpells)
                        {
                            if (info.SpellCastingLevel < SpellCastingLevel.Adept)
                                hero.SetSpellCastingLevel(SpellCastingLevel.Adept);
                        }
                        if (perk == TORPerks.SpellCraft.MasterSpells)
                        {
                            if (info.SpellCastingLevel < SpellCastingLevel.Master)
                                hero.SetSpellCastingLevel(SpellCastingLevel.Master);
                        }
                    }
                    if (hero.HasAttribute("Priest"))
                    {
                        if (perk == TORPerks.Faith.NovicePrayers)
                        {
                            if (!hero.HasAbility("HealingHand")) hero.AddAbility("HealingHand");
                        }
                        if (perk == TORPerks.Faith.AdeptPrayers)
                        {
                            if (!hero.HasAbility("AmourOfRighteousness")) hero.AddAbility("AmourOfRighteousness");
                            if (!hero.HasAbility("Vanquish")) hero.AddAbility("Vanquish");
                        }
                        if (perk == TORPerks.Faith.GrandPrayers)
                        {
                            if (!hero.HasAbility("CometOfSigmar")) hero.AddAbility("CometOfSigmar");
                        }
                    }
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}
