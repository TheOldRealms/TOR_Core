using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Helpers;
using SandBox.View;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Enables levitation idle, gliding, and battle animations on campaign map party icons
    /// when an unmounted party leader has a levitation off-hand focus or staff equipped.
    /// </summary>
    [HarmonyPatch]
    // Uncategorized patches run in OnSubModuleLoad, before action_types.xml exists.
    // Preparing MobilePartyVisual / ActionIndexCache at that point freezes every action index at -1.
    [HarmonyPatchCategory("LatePatches")]
    public static class CampaignMapLevitationPatches
    {
        private const string LEVITATION_ITEM_USAGE_PREFIX = "tor_levitate_";
        private const float LEVITATE_LOCOMOTION_DURATION = 5.63f;

        private sealed class LevitateMapState
        {
            public bool HasMainHand1h;
            public bool InBattle;
        }

        private static readonly ConditionalWeakTable<MobilePartyVisual, LevitateMapState> _levitatingVisuals = new();

        private static bool _actionsInitialized;
        private static ActionIndexCache _actMapIdle;
        private static ActionIndexCache _actMapMove;
        private static ActionIndexCache _act1hMapIdle;
        private static ActionIndexCache _act1hMapMove;
        private static ActionIndexCache _actMapBattle;

        private static void EnsureActionsInitialized()
        {
            if (_actionsInitialized)
            {
                return;
            }

            _actMapIdle = ActionIndexCache.Create("act_tor_levitate_map_idle");
            _actMapMove = ActionIndexCache.Create("act_tor_levitate_map_move");
            _act1hMapIdle = ActionIndexCache.Create("act_tor_levitate_1h_map_idle");
            _act1hMapMove = ActionIndexCache.Create("act_tor_levitate_1h_map_move");
            _actMapBattle = ActionIndexCache.Create("act_tor_levitate_map_battle");
            _actionsInitialized = true;
        }

        private static ActionIndexCache GetDesiredLocomotionAction(bool hasMainHand1h, bool isMoving)
        {
            if (hasMainHand1h)
            {
                return isMoving ? _act1hMapMove : _act1hMapIdle;
            }

            return isMoving ? _actMapMove : _actMapIdle;
        }

        private static bool IsLevitateOffhandItem(ItemObject item)
        {
            if (item == null || !item.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand))
            {
                return false;
            }

            var weapons = item.WeaponComponent?.Weapons;
            if (weapons == null)
            {
                return false;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                string itemUsage = weapons[i]?.ItemUsage;
                if (!string.IsNullOrEmpty(itemUsage) &&
                    itemUsage.StartsWith(LEVITATION_ITEM_USAGE_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetLevitateEquipment(CharacterObject leader, out int offhandSlot, out int mainHand1hSlot)
        {
            offhandSlot = -1;
            mainHand1hSlot = -1;

            if (leader == null || leader.HasMount() || leader.Equipment == null)
            {
                return false;
            }

            for (int i = (int)EquipmentIndex.WeaponItemBeginSlot; i < (int)EquipmentIndex.NumPrimaryWeaponSlots; i++)
            {
                ItemObject item = leader.Equipment[i].Item;
                if (item == null)
                {
                    continue;
                }

                if (offhandSlot < 0 && IsLevitateOffhandItem(item))
                {
                    offhandSlot = i;
                    continue;
                }

                if (mainHand1hSlot < 0 && !item.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand))
                {
                    WeaponComponentData primary = item.WeaponComponent?.PrimaryWeapon;
                    if (primary != null && primary.IsMeleeWeapon && primary.IsOneHanded)
                    {
                        mainHand1hSlot = i;
                    }
                }
            }

            return offhandSlot >= 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SandBoxViewHelpers.MobilePartyVisualHelper), nameof(SandBoxViewHelpers.MobilePartyVisualHelper.GetMeleeWeaponToWield))]
        public static void AvoidWieldingLevitateOffhandAsMainHand(PartyBase party, ref int wieldedItemIndex)
        {
            CharacterObject leader = PartyBaseHelper.GetVisualPartyLeader(party);
            if (TryGetLevitateEquipment(leader, out _, out int mainHand1hSlot))
            {
                wieldedItemIndex = mainHand1hSlot;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SandBoxViewHelpers.MobilePartyVisualHelper), nameof(SandBoxViewHelpers.MobilePartyVisualHelper.GetHumanAgentPartyVisual))]
        public static bool SpawnLevitateOffhandOnPartyIcon(
            Scene mapScene,
            MatrixFrame frame,
            PartyBase party,
            uint contourColor,
            ActionIndexCache leaderAction,
            ref bool clearBannerEntityCache,
            ref (string, GameEntity) cachedBannerEntity,
            ref float animationDuration,
            ref AgentVisuals __result)
        {
            CharacterObject leader = PartyBaseHelper.GetVisualPartyLeader(party);
            if (!TryGetLevitateEquipment(leader, out int offhandSlot, out int mainHand1hSlot))
            {
                return true;
            }

            // Skip campaign_banner_small in the off-hand so the character visibly holds their levitate staff/focus.
            uint color1 = party.MapFaction?.Color ?? unchecked((uint)-3357781);
            uint color2 = party.MapFaction?.Color2 ?? unchecked((uint)-3357781);
            Equipment equipment = leader.Equipment.Clone(false);
            Monster monster = FaceGen.GetBaseMonsterFromRace(leader.Race);
            MBActionSet actionSet = MBGlobals.GetActionSetWithSuffix(monster, leader.IsFemale, "_map");

            AgentVisualsData data = new AgentVisualsData()
                .UseMorphAnims(true)
                .Equipment(equipment)
                .BodyProperties(leader.GetBodyProperties(leader.Equipment, -1))
                .SkeletonType(leader.IsFemale ? SkeletonType.Female : SkeletonType.Male)
                .Scale(0.3f)
                .Frame(frame)
                .ActionSet(actionSet)
                .Scene(mapScene)
                .Monster(monster)
                .PrepareImmediately(false)
                .RightWieldedItemIndex(mainHand1hSlot)
                .LeftWieldedItemIndex(offhandSlot)
                .HasClippingPlane(true)
                .UseScaledWeapons(true)
                .ClothColor1(color1)
                .ClothColor2(color2)
                .CharacterObjectStringId(leader.StringId)
                .AddColorRandomness(!leader.IsHero)
                .Race(leader.Race);

            animationDuration = leaderAction != ActionIndexCache.act_none
                ? MBActionSet.GetActionAnimationDuration(actionSet, in leaderAction)
                : 1f;

            AgentVisuals visuals = AgentVisuals.Create(data, "PartyIcon " + leader.Name, false, false, false);
            if (visuals != null)
            {
                visuals.GetWeakEntity().SetContourColor(contourColor, false);
            }

            __result = visuals;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobilePartyVisual), "GetPartyBattleAnimation")]
        public static void UseLevitateBattleAnimation(
            PartyBase party,
            int wieldedItemIndex,
            ref ActionIndexCache leaderAction,
            ref ActionIndexCache mountAction)
        {
            CharacterObject leader = PartyBaseHelper.GetVisualPartyLeader(party);
            if (!TryGetLevitateEquipment(leader, out _, out _))
            {
                return;
            }

            EnsureActionsInitialized();
            if (_actMapBattle != ActionIndexCache.act_none)
            {
                leaderAction = _actMapBattle;
                mountAction = ActionIndexCache.act_none;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobilePartyVisual), "AddCharacterToPartyIcon")]
        public static void InitializeLevitatePartyIconAnimation(
            MobilePartyVisual __instance,
            CharacterObject characterObject,
            in ActionIndexCache leaderAction,
            float animationStartDuration,
            bool ____isEntityMovingCache)
        {
            _levitatingVisuals.Remove(__instance);

            if (__instance.HumanAgentVisuals == null || __instance.MountAgentVisuals != null)
            {
                return;
            }

            if (!TryGetLevitateEquipment(characterObject, out _, out int mainHand1hSlot))
            {
                return;
            }

            EnsureActionsInitialized();
            bool hasMainHand1h = mainHand1hSlot >= 0;
            bool inBattle = leaderAction != ActionIndexCache.act_none;
            _levitatingVisuals.Add(__instance, new LevitateMapState
            {
                HasMainHand1h = hasMainHand1h,
                InBattle = inBattle
            });

            if (inBattle)
            {
                return;
            }

            Skeleton skeleton = __instance.HumanAgentVisuals.GetVisuals()?.GetSkeleton();
            if (skeleton == null)
            {
                return;
            }

            ActionIndexCache target = GetDesiredLocomotionAction(hasMainHand1h, ____isEntityMovingCache);
            if (target != ActionIndexCache.act_none)
            {
                float startProgress = animationStartDuration > 0f
                    ? MBMath.ClampFloat(animationStartDuration / LEVITATE_LOCOMOTION_DURATION, 0f, 1f)
                    : 0f;
                skeleton.SetAgentActionChannel(0, in target, startProgress, -0.2f, true, 0f);
                __instance.HumanAgentVisuals.Tick(null, 0.0001f, ____isEntityMovingCache, 0f);
                skeleton.ForceUpdateBoneFrames();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobilePartyVisual), "ResetPartyIcon")]
        public static void ClearLevitateStateOnReset(MobilePartyVisual __instance)
        {
            _levitatingVisuals.Remove(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobilePartyVisual), "Tick")]
        public static void MaintainLevitatePartyIconAnimation(MobilePartyVisual __instance, bool ____isEntityMovingCache)
        {
            if (__instance.HumanAgentVisuals == null || __instance.MountAgentVisuals != null)
            {
                return;
            }

            if (!_levitatingVisuals.TryGetValue(__instance, out LevitateMapState state) || state.InBattle)
            {
                return;
            }

            Skeleton skeleton = __instance.HumanAgentVisuals.GetVisuals()?.GetSkeleton();
            if (skeleton == null)
            {
                return;
            }

            ActionIndexCache desired = GetDesiredLocomotionAction(state.HasMainHand1h, ____isEntityMovingCache);
            if (desired != ActionIndexCache.act_none && skeleton.GetActionAtChannel(0) != desired)
            {
                skeleton.SetAgentActionChannel(0, in desired, 0f, -0.2f, true, 0f);
            }
        }
    }
}
