using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Formations
{
    public static class TORFormationAssignments
    {
        private enum TroopFormationRole
        {
            Infantry,
            HeavyInfantry,
            ShieldInfantry,
            ShockInfantry,
            RangedInfantry,
            Cavalry,
            RangedCavalry,
            Monster,
            ArtilleryOperator,
            Spellcaster
        }

        private sealed class CultureFormationPreset
        {
            public string CultureKey { get; }
            public TroopFormationRole[][] FormationRoles { get; }

            public CultureFormationPreset(string cultureKey, TroopFormationRole[][] formationRoles)
            {
                CultureKey = cultureKey;
                FormationRoles = formationRoles;
            }
        }

        private static readonly Dictionary<string, CultureFormationPreset> _cultureFormations = new()
        {
            [TORConstants.Cultures.GREENSKIN] = new(
                "greenskin",
                [
                    [TroopFormationRole.Infantry],
                    [TroopFormationRole.ShieldInfantry],
                    [TroopFormationRole.ShockInfantry],
                    [TroopFormationRole.RangedInfantry],
                    [TroopFormationRole.Cavalry],
                    [TroopFormationRole.RangedCavalry],
                    [TroopFormationRole.Monster],
                    [TroopFormationRole.ArtilleryOperator, TroopFormationRole.Spellcaster]
                ])
        };

        private static CultureFormationPreset PlayerCultureFormations => _cultureFormations[Hero.MainHero.Culture.StringId];

        public static bool HasPlayerCultureFormations => _cultureFormations.ContainsKey(Hero.MainHero.Culture.StringId);

        public static string FormationCultureKey => PlayerCultureFormations.CultureKey;

        public static bool AppliesToPlayerTeam(BattleSideEnum battleSide)
        {
            var playerTeam = Mission.Current.PlayerTeam;
            return HasPlayerCultureFormations && playerTeam != null && playerTeam.Side == battleSide && playerTeam.IsPlayerGeneral;
        }

        public static string GetFormationIcon(int formationIndex)
        {
            return $"{FormationCultureKey}_formation_{formationIndex + 1}";
        }

        public static FormationClass GetFormation(CharacterObject character)
        {
            if (Campaign.Current.PlayerFormationPreferences.TryGetValue(character, out var formation))
            {
                return formation;
            }

            var attributes = character.GetAttributes();
            var hasShield = false;
            var hasRangedWeapon = false;
            if (!character.IsHero)
            {
                foreach (var equipment in character.BattleEquipments)
                {
                    for (var slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
                    {
                        var weapons = equipment[slot].Item?.Weapons;
                        if (weapons == null)
                        {
                            continue;
                        }

                        foreach (var weapon in weapons)
                        {
                            if (weapon.IsShield)
                            {
                                hasShield = true;
                            }
                            if (weapon.IsRangedWeapon &&
                                WeaponComponentData.GetItemTypeFromWeaponClass(weapon.WeaponClass) != ItemObject.ItemTypeEnum.Thrown)
                            {
                                hasRangedWeapon = true;
                            }
                        }
                    }
                }
            }

            var formationRoles = PlayerCultureFormations.FormationRoles;
            // the latter slots are prioritized if a troop qualifies for several roles
            for (var i = formationRoles.Length - 1; i > 0; i--)
            {
                foreach (var role in formationRoles[i])
                {
                    if (MatchesTroopFormationRole(character, role, attributes, hasShield, hasRangedWeapon))
                    {
                        return (FormationClass)i;
                    }
                }
            }

            return FormationClass.Infantry;
        }

        private static bool MatchesTroopFormationRole(CharacterObject character, TroopFormationRole role,
            List<string> attributes, bool hasShield, bool hasRangedWeapon)
        {
            if (character.IsHero)
            {
                return role == TroopFormationRole.Spellcaster && character.HeroObject.IsSpellCaster();
            }

            var isMeleeInfantry = !character.IsMounted && !hasRangedWeapon;
            var isHeavyInfantry = isMeleeInfantry && character.Tier >= 5;

            return role switch
            {
                TroopFormationRole.Infantry => isMeleeInfantry,
                TroopFormationRole.HeavyInfantry => isHeavyInfantry,
                TroopFormationRole.ShieldInfantry => isHeavyInfantry && hasShield,
                TroopFormationRole.ShockInfantry => isHeavyInfantry && !hasShield,
                TroopFormationRole.RangedInfantry => !character.IsMounted && hasRangedWeapon,
                TroopFormationRole.Cavalry => character.IsMounted && !hasRangedWeapon,
                TroopFormationRole.RangedCavalry => character.IsMounted && hasRangedWeapon,
                TroopFormationRole.Monster => attributes.Contains(TORConstants.CharacterAttributes.MONSTROUS),
                TroopFormationRole.ArtilleryOperator =>
                    attributes.Contains(TORConstants.CharacterAttributes.CREW_1) ||
                    attributes.Contains(TORConstants.CharacterAttributes.CREW_2) ||
                    attributes.Contains(TORConstants.CharacterAttributes.CREW_3),
                TroopFormationRole.Spellcaster => attributes.Contains(TORConstants.CharacterAttributes.SPELLCASTER),
                _ => false
            };
        }

        public static bool TryGetFormation(IAgentOriginBase origin, Team team, out FormationClass formation)
        {
            if (team == Mission.Current.PlayerTeam && origin.Troop is CharacterObject character &&
                (!character.IsHero || character.HeroObject != Hero.MainHero && character.HeroObject.PartyBelongedTo == MobileParty.MainParty))
            {
                formation = GetFormation(character);
                return true;
            }

            formation = FormationClass.Unset;
            return false;
        }
    }
}
