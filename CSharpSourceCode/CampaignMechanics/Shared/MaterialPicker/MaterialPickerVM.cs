using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Shared.MaterialPicker
{
    /// <summary>
    /// Top-level VM for the material picker overlay. Owns the material list,
    /// the current selection, the slider state, and the reward preview. The
    /// confirm callback hands back the chosen option + batch count; the caller
    /// is responsible for deducting items and applying rewards (the picker
    /// doesn't touch the roster, so callers can still layer perk multipliers
    /// over the aggregate — matches the MaterialTurnInHelper contract).
    /// </summary>
    public class MaterialPickerVM : ViewModel
    {
        private readonly Action<MaterialPickerOptionVM, int> _onConfirm;
        private readonly Action _onCancel;
        private readonly MaterialPicker.RewardPreviewFn _rewardPreviewFn;

        private string _screenTitle;
        private string _subtitle;
        private string _rewardPreview;
        private string _confirmText;
        private string _cancelText;
        private int _quantity;
        private int _maxQuantity;
        private int _minQuantity;
        private bool _isConfirmEnabled;
        private bool _isSliderEnabled;
        private MBBindingList<MaterialPickerOptionVM> _options = [];
        private MaterialPickerOptionVM _selectedOption;

        public MaterialPickerVM(
            TextObject title,
            IEnumerable<MaterialPickerOption> options,
            MaterialPicker.RewardPreviewFn rewardPreviewFn,
            Action<MaterialPickerOptionVM, int> onConfirm,
            Action onCancel)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _rewardPreviewFn = rewardPreviewFn;
            ScreenTitle = title?.ToString() ?? string.Empty;
            Subtitle = TORTextHelper.GetText("tor_material_picker.subtitle", "Pick a material and a quantity.");
            ConfirmText = TORTextHelper.GetText("tor_inquiry_accept_text", "Accept");
            CancelText = TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel");
            MinQuantity = 1;
            MaxQuantity = 1;
            Quantity = 1;
            IsSliderEnabled = false;
            IsConfirmEnabled = false;
            RewardPreview = string.Empty;

            if (options != null)
            {
                foreach (var o in options)
                {
                    if (o.Item == null || o.AvailableBatches <= 0) continue;
                    var row = new MaterialPickerOptionVM(
                        o.Item, o.BatchSize, o.AvailableBatches,
                        o.GoldPerBatch, o.PrestigePerBatch, o.Tag,
                        SelectOption);
                    Options.Add(row);
                }
            }
        }

        /// <summary>Wired as an ItemTemplate command target: the row calls this on click.</summary>
        public void SelectOption(MaterialPickerOptionVM opt)
        {
            if (opt == null) return;
            if (_selectedOption != null) _selectedOption.IsSelected = false;
            _selectedOption = opt;
            _selectedOption.IsSelected = true;
            MaxQuantity = System.Math.Max(1, opt.AvailableBatches);
            IsSliderEnabled = opt.AvailableBatches > 1;
            Quantity = System.Math.Min(Quantity, MaxQuantity);
            if (Quantity < MinQuantity) Quantity = MinQuantity;
            RefreshRewardPreview();
            IsConfirmEnabled = true;
        }

        private void RefreshRewardPreview()
        {
            if (_selectedOption == null)
            {
                RewardPreview = string.Empty;
                return;
            }
            if (_rewardPreviewFn != null)
            {
                RewardPreview = _rewardPreviewFn(_selectedOption, Quantity) ?? string.Empty;
                return;
            }
            var totalGold = _selectedOption.GoldPerBatch * Quantity;
            var totalPrestige = _selectedOption.PrestigePerBatch * Quantity;
            var txt = TORTextHelper.GetTextObject("tor_material_picker.reward", "Reward: {PRESTIGE} Prestige + {GOLD} gold");
            txt.SetTextVariable("PRESTIGE", totalPrestige);
            txt.SetTextVariable("GOLD", totalGold);
            RewardPreview = txt.ToString();
        }

        public void ExecuteConfirm()
        {
            if (_selectedOption == null) return;
            var opt = _selectedOption;
            var qty = Quantity;
            _onConfirm?.Invoke(opt, qty);
        }

        public void ExecuteCancel() => _onCancel?.Invoke();

        [DataSourceProperty]
        public string ScreenTitle
        {
            get => _screenTitle;
            set { if (value != _screenTitle) { _screenTitle = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public string Subtitle
        {
            get => _subtitle;
            set { if (value != _subtitle) { _subtitle = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public MBBindingList<MaterialPickerOptionVM> Options
        {
            get => _options;
            set { if (value != _options) { _options = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value == _quantity) return;
                _quantity = value;
                OnPropertyChangedWithValue(value);
                RefreshRewardPreview();
            }
        }

        [DataSourceProperty]
        public int MinQuantity
        {
            get => _minQuantity;
            set { if (value != _minQuantity) { _minQuantity = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public int MaxQuantity
        {
            get => _maxQuantity;
            set { if (value != _maxQuantity) { _maxQuantity = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsSliderEnabled
        {
            get => _isSliderEnabled;
            set { if (value != _isSliderEnabled) { _isSliderEnabled = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsConfirmEnabled
        {
            get => _isConfirmEnabled;
            set { if (value != _isConfirmEnabled) { _isConfirmEnabled = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public string RewardPreview
        {
            get => _rewardPreview;
            set { if (value != _rewardPreview) { _rewardPreview = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public string ConfirmText
        {
            get => _confirmText;
            set { if (value != _confirmText) { _confirmText = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public string CancelText
        {
            get => _cancelText;
            set { if (value != _cancelText) { _cancelText = value; OnPropertyChangedWithValue(value); } }
        }
    }
}
