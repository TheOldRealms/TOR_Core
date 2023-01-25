using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Extensions
{
    public static class HeroExtensions
    {
        public static bool CanRaiseDead(this Hero hero)
        {
            return hero.IsHumanPlayerCharacter && hero.IsNecromancer();
        }

        /// <summary>
        /// Returns raise dead chance, where, for example, 0.1 is a 10% chance.
        /// </summary>
        /// <param name="hero"></param>
        /// <returns></returns>
        public static float GetRaiseDeadChance(this Hero hero)
        {
            return hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence) * 0.07f;
        }

        public static HeroExtendedInfo GetExtendedInfo(this Hero hero)
        {
            return ExtendedInfoManager.Instance.GetHeroInfoFor(hero.GetInfoKey());
        }

        public static int GetEffectiveWindsCostForSpell(this Hero hero, Spell spell)
        {
            return hero.GetEffectiveWindsCostForSpell(spell.Template);
        }

        public static int GetEffectiveWindsCostForSpell(this Hero hero, AbilityTemplate spell)
        {
            int result = spell.WindsOfMagicCost;
            if (Game.Current.GameType is Campaign)
            {
                var model = Campaign.Current.Models.GetAbilityModel();
                if (model != null)
                {
                    result = model.GetEffectiveWindsCost(hero.CharacterObject, spell);
                }
            }
            return result;
        }

        public static int GetPlaceableArtilleryCount(this Hero hero)
        {
            int count = 0;
            if (hero.CanPlaceArtillery())
            {
                var engineering = hero.GetSkillValue(DefaultSkills.Engineering);
                count = (int)Math.Truncate((decimal)engineering / 50);
                if (hero != Hero.MainHero && count == 0) count = 1; //Ensure AI lords can place at least 1 piece.
            }
            return count;
        }

        public static bool CanPlaceArtillery(this Hero hero)
        {
            return hero.HasAttribute("CanPlaceArtillery");
        }

        public static void AddAbility(this Hero hero, string ability)
        {
            var info = hero.GetExtendedInfo();
            if (info != null && !info.AllAbilites.Contains(ability))
            {
                info.AcquiredAbilities.Add(ability);
            }
        }

        public static void AddAttribute(this Hero hero, string attribute)
        {
            var info = hero.GetExtendedInfo();
            if (info != null && !info.AllAttributes.Contains(attribute))
            {
                info.AcquiredAttributes.Add(attribute);
            }
        }

        public static bool HasAttribute(this Hero hero, string attribute)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().AllAttributes.Contains(attribute);
            }
            else return false;
        }

        public static bool HasAbility(this Hero hero, string ability)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().AllAbilites.Contains(ability);
            }
            else return false;
        }

        public static void SetSpellCastingLevel(this Hero hero, SpellCastingLevel level)
        {
            if (hero.GetExtendedInfo() != null)
            {
                hero.GetExtendedInfo().SpellCastingLevel = level;
            }
        }

        public static void AddKnownLore(this Hero hero, string loreID)
        {
            if (hero.GetExtendedInfo() != null)
            {
                hero.GetExtendedInfo().AddKnownLore(loreID);
            }
        }

        public static bool HasKnownLore(this Hero hero, string loreID)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().HasKnownLore(loreID);
            }
            else return false;
        }

        public static bool IsSpellCaster(this Hero hero)
        {
            return hero.HasAttribute("SpellCaster");
        }

        public static bool IsAbilityUser(this Hero hero)
        {
            return hero.HasAttribute("AbilityUser");
        }

        public static bool IsNecromancer(this Hero hero)
        {
            return hero.HasAttribute("Necromancer");
        }

        public static bool IsUndead(this Hero hero)
        {
            return hero.HasAttribute("Undead");
        }

        public static bool IsVampire(this Hero hero)
        {
            return hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("vampire");
        }

        public static bool IsSpellTrainer(this Hero hero)
        {
            return hero.Occupation == Occupation.Special && hero.Name.Contains("Magister");
        }

        public static bool IsMasterEngineer(this Hero hero)
        {
            if(hero!=null)
                return hero.Occupation == Occupation.Special&& hero.Name.Contains("Master Engineer");
            return false;
        }

        public static bool HasCareerChoice(this Hero hero, CareerChoiceObject choice)
        {
            bool result = false;
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().CareerChoices.Contains(choice.StringId);
            }
            return result;
        }

        public static bool HasCareer(this Hero hero, CareerObject career)
        {
            bool result = false;
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().CareerID == career.StringId;
            }
            return result;
        }

        public static CareerObject GetCareer(this Hero hero)
        {
            CareerObject result = null;
            if (hero != null && hero.GetExtendedInfo() != null && !string.IsNullOrEmpty(hero.GetExtendedInfo().CareerID))
            {
                result = TORCareers.All.FirstOrDefault(x=>x.StringId == hero.GetExtendedInfo().CareerID);
            }
            return result;
        }

        public static string GetInfoKey(this Hero hero)
        {
            return hero.StringId;
        }
    }
}
