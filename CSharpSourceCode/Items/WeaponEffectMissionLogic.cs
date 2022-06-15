using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.Items
{
    public class WeaponEffectMissionLogic : MissionLogic
    {
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.IsHuman)
            {
                var comp = new ItemTraitAgentComponent(agent);
                agent.AddComponent(comp);
                agent.OnAgentWieldedItemChange += comp.OnWieldedItemChanged;
            }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (affectedAgent == affectorAgent)
                return;

            if (affectorWeapon.Item != null && affectorWeapon.Item.HasTrait(affectorAgent))
            {
                var relevantTraits = affectorWeapon.Item.GetTraits(affectorAgent).Where(x => x.ImbuedStatusEffectId != "none");
                if (relevantTraits != null && relevantTraits.Count() > 0)
                {
                    foreach (var trait in relevantTraits)
                    {
                        affectedAgent.ApplyStatusEffect(trait.ImbuedStatusEffectId, affectorAgent);
                    }
                }
                //TODO: disabling this for first release, we dont actually have an item script. This just clogs system resources and spams the screen with debug messages.
                /*
                if(magiceffect.OnHitScriptName != "none")
                {
                    try
                    {
                        var obj = Activator.CreateInstance(Type.GetType(magiceffect.OnHitScriptName));
                        if(obj is IMagicWeaponHitEffect)
                        {
                            var script = obj as IMagicWeaponHitEffect;
                            script.OnHit(affectedAgent, affectorAgent, affectorWeapon);
                        }
                    }
                    catch(Exception)
                    {
                        TOW_Core.Utilities.TORCommon.Log("Tried to create magicweapon onhitscript: " + magiceffect.OnHitScriptName + ", but failed.", NLog.LogLevel.Error);
                    }
                }
                */
            }
        }

        private bool HasWeaponWithTrait(Agent agent)
        {
            if (agent.IsHuman)
            {
                for (int i = 0; i < 4; i++)
                {
                    var weapon = agent.Equipment[i];
                    if (weapon.Item != null)
                    {
                        var magiceffect = weapon.Item.GetTraits(agent);
                        if (magiceffect != null)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
