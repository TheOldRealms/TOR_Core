using SandBox.ViewModelCollection.Nameplate;
using System;
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
        // 3 POI slots alongside the RoR widget → up to 4 icons visible per nameplate.
        private const int SlotCount = 3;

        private readonly Settlement _settlement;

        private bool _isRoRSettlement;
        private bool _isShrine;

        private readonly bool[] _slotVisible = new bool[SlotCount];
        private readonly string[] _slotText = new string[SlotCount];

        public ToRSettlementNameplateVM(Settlement settlement, GameEntity entity, Camera mapCamera, Action<CampaignVec2> fastMoveCameraToPosition)
            : base(settlement, entity, mapCamera, fastMoveCameraToPosition)
        {
            _settlement = settlement;
            _isRoRSettlement = settlement.IsRoRSettlement();
            _isShrine = settlement.SettlementComponent is ShrineComponent;
            for (int i = 0; i < SlotCount; i++) _slotText[i] = "";
            RefreshPoiState();
            SettlementPOIManagerCampaignBehavior.DiscoveryChanged += OnDiscoveryChanged;
            SettlementPOIManagerCampaignBehavior.IconsChanged     += OnDiscoveryChanged;
        }

        private void OnDiscoveryChanged() => RefreshPoiState();

        /// <summary>
        /// Rebuild slot (visible, text) pairs from current discovery state. Each applicable
        /// POI takes the next slot; discovered → POI.MapIconText (sprite &lt;img&gt; tag or
        /// letter fallback), undiscovered → "?". Beyond 3 applicable POIs are dropped.
        /// </summary>
        public void RefreshPoiState()
        {
            for (int i = 0; i < SlotCount; i++) { _slotVisible[i] = false; _slotText[i] = ""; }

            var mgr = Campaign.Current?.GetCampaignBehavior<SettlementPOIManagerCampaignBehavior>();
            if (mgr == null || _settlement == null) { NotifyAllSlots(); return; }

            int slot = 0;
            foreach (var poi in SettlementPOIManagerCampaignBehavior.All)
            {
                if (slot >= SlotCount) break;
                if (!poi.AppliesTo(_settlement)) continue;
                _slotVisible[slot] = true;
                _slotText[slot] = mgr.IsDiscovered(_settlement, poi) ? poi.MapIconTextFor(_settlement) : "?";
                slot++;
            }
            NotifyAllSlots();
        }

        private void NotifyAllSlots()
        {
            OnPropertyChangedWithValue(Poi1Visible, nameof(Poi1Visible));
            OnPropertyChangedWithValue(Poi1Text,    nameof(Poi1Text));
            OnPropertyChangedWithValue(Poi2Visible, nameof(Poi2Visible));
            OnPropertyChangedWithValue(Poi2Text,    nameof(Poi2Text));
            OnPropertyChangedWithValue(Poi3Visible, nameof(Poi3Visible));
            OnPropertyChangedWithValue(Poi3Text,    nameof(Poi3Text));
        }

        [DataSourceProperty] public bool IsRoRSettlement { get => _isRoRSettlement; set { if (value != _isRoRSettlement) { _isRoRSettlement = value; OnPropertyChangedWithValue(value, nameof(IsRoRSettlement)); } } }
        [DataSourceProperty] public bool IsShrine { get => _isShrine; set { if (value != _isShrine) { _isShrine = value; OnPropertyChangedWithValue(value, nameof(IsShrine)); } } }

        [DataSourceProperty] public bool   Poi1Visible => _slotVisible[0];
        [DataSourceProperty] public string Poi1Text    => _slotText[0];
        [DataSourceProperty] public bool   Poi2Visible => _slotVisible[1];
        [DataSourceProperty] public string Poi2Text    => _slotText[1];
        [DataSourceProperty] public bool   Poi3Visible => _slotVisible[2];
        [DataSourceProperty] public string Poi3Text    => _slotText[2];
    }
}
