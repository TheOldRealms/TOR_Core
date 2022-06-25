using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem.Spells;
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

        public static int GetPlaceableArtilleryCount(this Hero hero)
        {
            int count = 0;
            if (hero.CanPlaceArtillery())
            {
                var engineering = hero.GetSkillValue(DefaultSkills.Engineering);
                count = (int)Math.Truncate((decimal)engineering / 50);
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

        public static string GetInfoKey(this Hero hero)
        {
            return hero.StringId;
        }
    }
}
