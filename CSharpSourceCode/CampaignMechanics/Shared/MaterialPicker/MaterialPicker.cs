using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.CampaignMechanics.Shared.MaterialPicker
{
    /// <summary>
    /// One material row the caller contributes to the picker. Raw data — the
    /// picker builds the per-row VM internally and wires the click handler
    /// through to its own selection logic, so callers don't take a VM dep.
    /// </summary>
    public readonly struct MaterialPickerOption
    {
        public MaterialPickerOption(TaleWorlds.Core.ItemObject item, int batchSize, int availableBatches,
                                    int goldPerBatch, int prestigePerBatch, object tag = null)
        {
            Item = item;
            BatchSize = batchSize;
            AvailableBatches = availableBatches;
            GoldPerBatch = goldPerBatch;
            PrestigePerBatch = prestigePerBatch;
            Tag = tag;
        }

        public readonly TaleWorlds.Core.ItemObject Item;
        public readonly int BatchSize;
        public readonly int AvailableBatches;
        public readonly int GoldPerBatch;
        public readonly int PrestigePerBatch;
        public readonly object Tag;
    }

    /// <summary>
    /// Picked option payload — passed to the caller's confirm callback.
    /// </summary>
    public readonly struct PickedMaterial
    {
        public PickedMaterial(TaleWorlds.Core.ItemObject item, int batchSize, int quantity,
                              int goldPerBatch, int prestigePerBatch, object tag)
        {
            Item = item;
            BatchSize = batchSize;
            Quantity = quantity;
            GoldPerBatch = goldPerBatch;
            PrestigePerBatch = prestigePerBatch;
            Tag = tag;
        }

        public readonly TaleWorlds.Core.ItemObject Item;
        public readonly int BatchSize;
        /// <summary>How many <see cref="BatchSize"/>-sized batches the player picked (slider value).</summary>
        public readonly int Quantity;
        public readonly int GoldPerBatch;
        public readonly int PrestigePerBatch;
        public readonly object Tag;

        /// <summary>Total units of <see cref="Item"/> the caller should deduct.</summary>
        public int TotalUnits => BatchSize * Quantity;
    }

    /// <summary>
    /// Overlay-style material picker: material list on the left, quantity slider +
    /// reward preview on the right, confirm/cancel at the bottom. Replaces the
    /// per-unit option explosion in <see cref="MaterialTurnInHelper"/> for flows
    /// where counts can be large (OathGold steel/grain, witchhunter warpstone).
    ///
    /// <para>Added to <see cref="ScreenManager.TopScreen"/> as a <see cref="GauntletLayer"/>
    /// — no new <see cref="TaleWorlds.Core.GameState"/>, so it opens cleanly over
    /// a dialog or settlement menu without unwinding navigation state.</para>
    ///
    /// <para>The picker doesn't touch the player's roster or wallet; the confirm
    /// callback is the single reward site so callers can layer perk multipliers
    /// (OathGold's Legacy of Grungni, witchhunter heretical bonus, etc.) over the
    /// aggregate before applying.</para>
    /// </summary>
    public static class MaterialPicker
    {
        private static GauntletLayer _layer;
        private static ScreenBase _owningScreen;
        private static MaterialPickerVM _vm;

        public delegate void ConfirmHandler(PickedMaterial picked);

        public static bool IsOpen => _layer != null;

        /// <summary>Callback that formats the "Reward: ..." preview on each slider
        /// tick. Receives the currently-selected row and the slider's quantity. If
        /// null, the VM falls back to "<c>{PRESTIGE} Prestige + {GOLD} gold</c>"
        /// using the row's per-batch Gold/Prestige fields.</summary>
        public delegate string RewardPreviewFn(MaterialPickerOptionVM option, int quantity);

        public static void Open(
            TextObject title,
            IEnumerable<MaterialPickerOption> options,
            ConfirmHandler onConfirm,
            Action onCancel = null,
            RewardPreviewFn rewardPreview = null)
        {
            if (IsOpen) return;
            if (options == null) return;

            _vm = new MaterialPickerVM(
                title,
                options,
                rewardPreview,
                (opt, qty) =>
                {
                    var picked = new PickedMaterial(opt.Item, opt.BatchSize, qty,
                        opt.GoldPerBatch, opt.PrestigePerBatch, opt.Tag);
                    Close();
                    onConfirm?.Invoke(picked);
                },
                () =>
                {
                    Close();
                    onCancel?.Invoke();
                });

            // Layer priority 3000 keeps the picker above dialog/HUD layers which
            // typically sit at 100-200. GauntletLayer ctor arg order differs across
            // Bannerlord revisions; the (name, priority, loadDefault) overload is
            // used by sibling screens (EnchantingScreen, AbilityHUDMissionView).
            _layer = new GauntletLayer("GauntletLayer", 3000, true);
            _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
            _layer.LoadMovie("MaterialPicker", _vm);
            _layer.IsFocusLayer = true;

            _owningScreen = ScreenManager.TopScreen;
            if (_owningScreen == null) { Close(); return; }
            _owningScreen.AddLayer(_layer);
            ScreenManager.TrySetFocus(_layer);
        }

        public static void Close()
        {
            if (_layer == null) return;
            try
            {
                _layer.IsFocusLayer = false;
                ScreenManager.TryLoseFocus(_layer);
                _owningScreen?.RemoveLayer(_layer);
            }
            finally
            {
                _layer = null;
                _owningScreen = null;
                _vm = null;
            }
        }
    }
}
