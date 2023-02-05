using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using NLog;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Cosmetics
{
    public class LanceRemovalMissionLogic: MissionLogic
    {
        private bool _hasUnprocessedAgents;
        private Queue<Agent> _unprocessedAgents = new Queue<Agent>();
        private int _counter;

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!Mission.Current.IsFriendlyMission && !Mission.Current.IsSiegeBattle) return;
            
            
            if (agent.IsHuman&& agent.Character.IsKnight())
            {
                _hasUnprocessedAgents = true;
                _unprocessedAgents.Enqueue(agent);
            }
            
            base.OnAgentBuild(agent, banner);

        }


        /*public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsHuman && !agent.Character.IsHero)
            {
                if (agent.Character.IsKnight())
                {
                    agent.Character = Mission.Agents.Find(x => !x.Character.IsKnight()&&!x.Character.IsHero).Character;
                    TORCommon.Say("lol");
                }
                    
                
                
                
                var tier = agent.Character.DefaultFormationClass;
                if (tier == FormationClass.HeavyCavalry && Mission.Current.IsFriendlyMission)
                {

                    return;

                }

                
            }
            base.OnAgentCreated(agent);
        }*/
        
        


        public override void OnMissionTick(float dt)
        {
            if (_hasUnprocessedAgents)
            {
                while (_unprocessedAgents.Count > 0)
                {
                    var agent = _unprocessedAgents.Dequeue();
                    try
                    {
                        for (int i = 0; i < (int)EquipmentIndex.ArmorItemBeginSlot; i++)
                        {
                            var weapon = agent.Equipment[i];

                            if (weapon.Item.StringId.Contains("lance"))
                            {
                                var prevItem = agent.Equipment[i];
                                

                                if (Mission.IsFriendlyMission)
                                {
                                    var item = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_weapon_halberd_001");
                                    if(item != null)
                                    {
                                       //agent.RemoveEquippedWeapon((EquipmentIndex)i);
                                        var missionWeapon = new MissionWeapon(item,prevItem.ItemModifier, prevItem.Banner);
                                        agent.EquipWeaponWithNewEntity((EquipmentIndex)i, ref missionWeapon);
                                    }
                                }
                                else
                                {
                                    agent.RemoveEquippedWeapon((EquipmentIndex)i);
                                    agent.EquipItemsFromSpawnEquipment(false);
                                }
                                
                            }
                        }

                    }
                    catch
                    {
                        TORCommon.Log("couldnt find EquipmentIndex", LogLevel.Warn);
                    }
                    
                    _counter++;
                }
                _hasUnprocessedAgents = false;
            }
        }
    }
}