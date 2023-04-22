using SandBox.ViewModelCollection.Nameplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.RegimentsOfRenown
{
    public class ToRSettlementNameplateVM : SettlementNameplateVM
    {
        private bool _isRoRSettlement;
        private bool _isShrine;

        public ToRSettlementNameplateVM(Settlement settlement, GameEntity entity, Camera mapCamera, Action<Vec2> fastMoveCameraToPosition) : base(settlement, entity, mapCamera, fastMoveCameraToPosition)
        {
            _isRoRSettlement = settlement.IsRoRSettlement();
            _isShrine = settlement.SettlementComponent is TORCustomSettlementComponent && ((TORCustomSettlementComponent)settlement.SettlementComponent).SettlementType is Shrine;
        }

        [DataSourceProperty]
        public bool IsRoRSettlement
        {
            get
            {
                return _isRoRSettlement;
            }
            set
            {
                if (value != _isRoRSettlement)
                {
                    _isRoRSettlement = value;
                    OnPropertyChangedWithValue(value, "IsRoRSettlement");
                }
            }
        }

        [DataSourceProperty]
        public bool IsShrine
        {
            get
            {
                return _isShrine;
            }
            set
            {
                if (value != _isShrine)
                {
                    _isShrine = value;
                    OnPropertyChangedWithValue(value, "IsShrine");
                }
            }
        }
    }
}
