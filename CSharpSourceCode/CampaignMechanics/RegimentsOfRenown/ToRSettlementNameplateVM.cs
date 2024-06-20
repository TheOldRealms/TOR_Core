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
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.RegimentsOfRenown
{
    public class ToRSettlementNameplateVM(Settlement settlement, GameEntity entity, Camera mapCamera, Action<Vec2> fastMoveCameraToPosition) : SettlementNameplateVM(settlement, entity, mapCamera, fastMoveCameraToPosition)
    {
        private bool _isRoRSettlement = settlement.IsRoRSettlement();
        private bool _isShrine = settlement.SettlementComponent is ShrineComponent;

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
