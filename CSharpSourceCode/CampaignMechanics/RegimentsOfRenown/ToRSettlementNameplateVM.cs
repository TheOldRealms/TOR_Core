using SandBox.ViewModelCollection.Nameplate;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.SettlementPOIs;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.RegimentsOfRenown
{
    public class ToRSettlementNameplateVM : SettlementNameplateVM
    {
        // 3 POI slots on the nameplate (RoR occupies its own fixed widget separately,
        // for a total of up to 4 visible icons per settlement). When RoR migrates to a
        // real POI subclass later, these slots become the full 4-slot set.
        private const int SlotCount = 3;

        private readonly Settlement _settlement;

        private bool _isRoRSettlement;
        private bool _isShrine;

        private readonly bool[] _slotVisible = new bool[SlotCount];
        private readonly bool[] _slotDiscovered = new bool[SlotCount];
        private readonly string[] _slotBrush = new string[SlotCount];
        private readonly string[] _slotText = new string[SlotCount];

        public ToRSettlementNameplateVM(Settlement settlement, GameEntity entity, Camera mapCamera, Action<CampaignVec2> fastMoveCameraToPosition)
            : base(settlement, entity, mapCamera, fastMoveCameraToPosition)
        {
            _settlement = settlement;
            _isRoRSettlement = settlement.IsRoRSettlement();
            _isShrine = settlement.SettlementComponent is ShrineComponent;
            for (int i = 0; i < SlotCount; i++) { _slotBrush[i] = ""; _slotText[i] = ""; }
            RefreshPoiState();
        }

        /// <summary>
        /// Rebuild slot assignments from current manager state. Call after discovery triggers
        /// so the nameplate reflects "? → icon" transitions.
        /// </summary>
        public void RefreshPoiState()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _slotVisible[i] = false;
                _slotDiscovered[i] = false;
                _slotBrush[i] = "";
                _slotText[i] = "";
            }

            var mgr = Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            if (mgr == null || _settlement == null) { NotifyAllSlots(); return; }

            int slot = 0;
            foreach (var poi in SettlementPOIManagerCampaignBehavior.All)
            {
                if (slot >= SlotCount) break;
                if (!poi.AppliesTo(_settlement)) continue;
                _slotVisible[slot] = true;
                _slotDiscovered[slot] = mgr.IsDiscovered(_settlement, poi);
                _slotBrush[slot] = poi.MapIconBrushName ?? "";
                _slotText[slot] = poi.MapIconText ?? "";
                slot++;
            }
            NotifyAllSlots();
        }

        private void NotifyAllSlots()
        {
            OnPropertyChangedWithValue(Poi1Visible, nameof(Poi1Visible));
            OnPropertyChangedWithValue(Poi1Discovered, nameof(Poi1Discovered));
            OnPropertyChangedWithValue(Poi1ShowQuestion, nameof(Poi1ShowQuestion));
            OnPropertyChangedWithValue(Poi1Brush, nameof(Poi1Brush));
            OnPropertyChangedWithValue(Poi1Text, nameof(Poi1Text));

            OnPropertyChangedWithValue(Poi2Visible, nameof(Poi2Visible));
            OnPropertyChangedWithValue(Poi2Discovered, nameof(Poi2Discovered));
            OnPropertyChangedWithValue(Poi2ShowQuestion, nameof(Poi2ShowQuestion));
            OnPropertyChangedWithValue(Poi2Brush, nameof(Poi2Brush));
            OnPropertyChangedWithValue(Poi2Text, nameof(Poi2Text));

            OnPropertyChangedWithValue(Poi3Visible, nameof(Poi3Visible));
            OnPropertyChangedWithValue(Poi3Discovered, nameof(Poi3Discovered));
            OnPropertyChangedWithValue(Poi3ShowQuestion, nameof(Poi3ShowQuestion));
            OnPropertyChangedWithValue(Poi3Brush, nameof(Poi3Brush));
            OnPropertyChangedWithValue(Poi3Text, nameof(Poi3Text));
        }

        [DataSourceProperty] public bool IsRoRSettlement { get => _isRoRSettlement; set { if (value != _isRoRSettlement) { _isRoRSettlement = value; OnPropertyChangedWithValue(value, nameof(IsRoRSettlement)); } } }
        [DataSourceProperty] public bool IsShrine { get => _isShrine; set { if (value != _isShrine) { _isShrine = value; OnPropertyChangedWithValue(value, nameof(IsShrine)); } } }

        [DataSourceProperty] public bool Poi1Visible => _slotVisible[0];
        [DataSourceProperty] public bool Poi1Discovered => _slotVisible[0] && _slotDiscovered[0];
        [DataSourceProperty] public bool Poi1ShowQuestion => _slotVisible[0] && !_slotDiscovered[0];
        [DataSourceProperty] public string Poi1Brush => _slotBrush[0];
        [DataSourceProperty] public string Poi1Text => _slotText[0];

        [DataSourceProperty] public bool Poi2Visible => _slotVisible[1];
        [DataSourceProperty] public bool Poi2Discovered => _slotVisible[1] && _slotDiscovered[1];
        [DataSourceProperty] public bool Poi2ShowQuestion => _slotVisible[1] && !_slotDiscovered[1];
        [DataSourceProperty] public string Poi2Brush => _slotBrush[1];
        [DataSourceProperty] public string Poi2Text => _slotText[1];

        [DataSourceProperty] public bool Poi3Visible => _slotVisible[2];
        [DataSourceProperty] public bool Poi3Discovered => _slotVisible[2] && _slotDiscovered[2];
        [DataSourceProperty] public bool Poi3ShowQuestion => _slotVisible[2] && !_slotDiscovered[2];
        [DataSourceProperty] public string Poi3Brush => _slotBrush[2];
        [DataSourceProperty] public string Poi3Text => _slotText[2];
    }
}
