using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Shared.MaterialPicker
{
    /// <summary>
    /// One row in the material picker's left list. Holds the <see cref="ItemObject"/>
    /// metadata plus the per-unit reward preview so the right pane can read it off
    /// the selected row rather than carrying a parallel data structure.
    /// </summary>
    public class MaterialPickerOptionVM : ViewModel
    {
        private readonly Action<MaterialPickerOptionVM> _onSelected;
        private string _displayName;
        private string _availableLabel;
        private ImageIdentifier _icon;
        private bool _isSelected;

        public ItemObject Item { get; }

        /// <summary>Minimum step; a slider position of 1 removes <see cref="BatchSize"/> units.</summary>
        public int BatchSize { get; }

        /// <summary>Gold the reward callback should add per batch.</summary>
        public int GoldPerBatch { get; }

        /// <summary>Prestige (or culture-specific resource unit count) the reward callback should add per batch.</summary>
        public int PrestigePerBatch { get; }

        /// <summary>Opaque discriminator the caller can read in the confirm callback.</summary>
        public object Tag { get; }

        /// <summary>How many <see cref="BatchSize"/>-sized batches are available in the player's roster.</summary>
        public int AvailableBatches { get; }

        public MaterialPickerOptionVM(
            ItemObject item,
            int batchSize,
            int availableBatches,
            int goldPerBatch,
            int prestigePerBatch,
            object tag,
            Action<MaterialPickerOptionVM> onSelected)
        {
            Item = item;
            BatchSize = batchSize;
            AvailableBatches = availableBatches;
            GoldPerBatch = goldPerBatch;
            PrestigePerBatch = prestigePerBatch;
            Tag = tag;
            _onSelected = onSelected;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            DisplayName = Item?.Name?.ToString() ?? string.Empty;
            Icon = new ItemImageIdentifier(Item);
            var available = TORTextHelper.GetTextObject("tor_material_picker.available", "Available: {COUNT}");
            available.SetTextVariable("COUNT", AvailableBatches * BatchSize);
            AvailableLabel = available.ToString();
        }

        public void ExecuteSelect() => _onSelected?.Invoke(this);

        [DataSourceProperty]
        public string DisplayName
        {
            get => _displayName;
            set { if (value != _displayName) { _displayName = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public string AvailableLabel
        {
            get => _availableLabel;
            set { if (value != _availableLabel) { _availableLabel = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public ImageIdentifier Icon
        {
            get => _icon;
            set { if (value != _icon) { _icon = value; OnPropertyChangedWithValue(value); } }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set { if (value != _isSelected) { _isSelected = value; OnPropertyChangedWithValue(value); } }
        }
    }
}
