using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CharacterDevelopment
{
    public class TORPerkHandlerCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkPicked);
        }

        /// <remarks>
        /// For newly created heroes, the hero developer is initialized and perks are picked before OnHeroCreated is dispatched so the extended info manager hasn't yet had time to create an entry for the hero.
        /// Any perks that interact with ExtendedHeroInfo must be manually adjusted via ExtendedInfoManager.OnHeroCreated OR make use of the TORCampaignEvents.OnHeroExtendedInfoCreated event in order to set their initial value properly.
        /// </remarks>
        private void OnPerkPicked(Hero hero, PerkObject perk)
        {
            if (hero != null && perk != null) //!hero.GetPerkValue(perk) -> this does not get triggered for any of the perks, since all of them do have a value of 0!
                                              //No, it's because the perk value is set to 1, then the event is dispatched, so GetPerkValue will always return true at this point. GetPerkValue returns false if the perk value is 0.
            {
                if (perk == TORPerks.GunPowder.FiringDrills)
                {
                    if (hero.GetAttributeValue(TORAttributes.Discipline) < Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute)
                    {
                        hero.HeroDeveloper.AddAttribute(TORAttributes.Discipline, 1, false);
                    }
                    else hero.HeroDeveloper.UnspentAttributePoints += 1;
                }
                if (perk == TORPerks.Faith.DivineMission)
                {
                    if (hero.HeroDeveloper.GetFocus(DefaultSkills.Medicine) < Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill)
                    {
                        hero.HeroDeveloper.AddFocus(DefaultSkills.Medicine, 1, false);
                    }
                    else hero.HeroDeveloper.UnspentFocusPoints += 1;
                }
                if (perk == TORPerks.Faith.ForeSight) hero.HeroDeveloper.UnspentAttributePoints += 1;

                var info = hero.GetExtendedInfo();
                if (info != null)
                {
                    if (hero.IsSpellCaster() || hero.HasAttribute(CharacterAttributes.RUNESMITH))
                    {
                        if (perk == TORPerks.Spellcraft.EntrySpells)
                        {
                            if (info.SpellCastingLevel < SpellCastingLevel.Entry)
                                hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                        }
                        if (perk == TORPerks.Spellcraft.AdeptSpells)
                        {
                            if (info.SpellCastingLevel < SpellCastingLevel.Adept)
                                hero.SetSpellCastingLevel(SpellCastingLevel.Adept);
                        }
                        if (perk == TORPerks.Spellcraft.MasterSpells)
                        {
                            if (info.SpellCastingLevel < SpellCastingLevel.Master)
                                hero.SetSpellCastingLevel(SpellCastingLevel.Master);
                        }
                    }

                    if (hero.IsPriest())
                    {
                        var prayers = CareerHelper.GetPriestPrayerList(hero);

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