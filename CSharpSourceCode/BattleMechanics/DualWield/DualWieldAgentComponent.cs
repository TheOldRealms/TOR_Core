using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.DualWield
{
    public class DualWieldAgentComponent : AgentComponent
    {
        private Banner _banner;
        private bool _pendingDualWieldActivation;
        private bool _pendingDualWieldDeactivation;
        private int _offhand_index = -1;

        public DualWieldAgentComponent(Agent agent, Banner banner) : base(agent)
        {
            _banner = banner;
        }

        internal void OnWieldedItemChanged()
        {
            var weapon = Agent.WieldedWeapon;
            var offhand = Agent.WieldedOffhandWeapon;
            if (!weapon.IsEmpty && offhand.IsEmpty && weapon.CurrentUsageItem.ItemUsage == "dualwield_mainhand")
            {
                var dw_offhand = MissionWeapon.Invalid;

                for (int i = 0; i < 5; i++)
                {
                    var equippedItem = Agent.Equipment[i];
                    if(!equippedItem.IsEmpty && equippedItem.CurrentUsageItem.ItemUsage == "dualwield_offhand")
                    {
                        dw_offhand = equippedItem;
                        _offhand_index = i;
                        break;
                    }
                }

                if(!dw_offhand.IsEmpty && dw_offhand.Item != null)
                {
                    _pendingDualWieldActivation = true;
                }
            }
            else if(weapon.IsEmpty && !offhand.IsEmpty && !offhand.IsShield())
            {
                _pendingDualWieldDeactivation = true;
            }
        }

        internal void Tick(float dt)
        {
            if (_pendingDualWieldActivation)
            {
                Agent.TryToWieldWeaponInSlot((EquipmentIndex)_offhand_index, Agent.WeaponWieldActionType.WithAnimation, false);
                _pendingDualWieldActivation = false;
            }
            if (_pendingDualWieldDeactivation)
            {
                if(!Agent.WieldedWeapon.IsEmpty&& Agent.WieldedWeapon.Item.IsMagicalStaff())
                    Agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimation);
                _pendingDualWieldDeactivation = false;
            }
        }
    }
}
