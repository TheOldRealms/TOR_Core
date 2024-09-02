using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.DualWield
{
    public class DualWieldMissionLogic : MissionLogic
    {
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if(agent.IsHuman)
            {
                var comp = new DualWieldAgentComponent(agent, banner);
                agent.AddComponent(comp);
                if (agent.GetHero() != Hero.MainHero)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (agent.Equipment[i].Item.IsMagicalStaff())
                        {
                            var missionWeapon = new MissionWeapon(agent.Equipment[i].Item, null, banner);
                            agent.EquipWeaponToExtraSlotAndWield( ref missionWeapon);
                        }
                    }
                }
                
                agent.OnAgentWieldedItemChange += comp.OnWieldedItemChanged;
            }
        }

        public override void OnPreMissionTick(float dt)
        {
            if(Agent.Main != null)
            {
                var comp = Agent.Main.GetComponent<DualWieldAgentComponent>();
                if(comp != null) comp.Tick(dt);
            }
        }
    }
}
