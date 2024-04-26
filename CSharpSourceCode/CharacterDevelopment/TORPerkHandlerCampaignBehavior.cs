using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem;
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
            if (hero != null && perk != null ) //!hero.GetPerkValue(perk) -> this does not get triggered for any of the perks, since all of them do have a value of 0!
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
                    
                    if (Hero.MainHero.HasAnyCareer())
                    {
                       
                        var prayers = CareerHelper.GetBattlePrayerList(Hero.MainHero.GetCareer());

                        var rank = 0;

                        if (perk == TORPerks.Faith.NovicePrayers)
                        {
                            rank = 2;
                        }
                        
                        else if (perk == TORPerks.Faith.AdeptPrayers)
                        {
                            rank = 3;
                        }
                        
                        else if (perk == TORPerks.Faith.GrandPrayers)
                        {
                            rank = 4;
                        }
                        
                        var prayersForRank = prayers.Where(X => X.Rank == rank);
                        foreach (var prayer in prayersForRank)
                        {
                            hero.AddAbility(prayer.PrayerID);
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
