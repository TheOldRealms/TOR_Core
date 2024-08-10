using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts;

public class DragonFireScript : CareerAbilityScript
{
    private float _radius;
    protected override void OnInit()
    {
        base.OnInit();
        var radius = 0f;
        var effects = GetEffectsToTrigger();
        foreach (var effect in effects)
        {
            if (effect.EffectRadius > radius)
                radius = effect.EffectRadius;
        }
        _radius = radius;
    }

    protected override void HandleCollision(Vec3 position, Vec3 normal)
    {
        if (Agent.Main == null)
            return;
        if (Hero.MainHero.HasCareerChoice("SecretOfMoonDragonKeystone"))
        {
            var agents = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, _radius , CasterAgent.Team, new MBList<Agent>());

            foreach (var agent in agents)
            {
                Agent.Main.ApplyStatusEffect("dragonfire_buff_dmg",Agent.Main,10f,false, false,true);
            }
            
        }
        
        if (Hero.MainHero.HasCareerChoice("SecretOfFellfangKeystone"))
        {
            var agents = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, _radius , CasterAgent.Team, new MBList<Agent>());

            foreach (var agent in agents)
            {
                agent.ApplyStatusEffect("dragonfire_wom_mark",Agent.Main,5f,false, false,true);
            }
            
        }

        base.HandleCollision(position, normal);
    }
}