using SandBox.ViewModelCollection.Nameplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.RegimentsOfRenown
{
    public class RoRSettlementNameplateVM : SettlementNameplateVM
    {
        private bool _isRoRSettlement;

        public RoRSettlementNameplateVM(Settlement settlement, GameEntity entity, Camera mapCamera, Action<Vec2> fastMoveCameraToPosition) : base(settlement, entity, mapCamera, fastMoveCameraToPosition)
        {
            _isRoRSettlement = settlement.IsRoRSettlement();
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
    }
}
