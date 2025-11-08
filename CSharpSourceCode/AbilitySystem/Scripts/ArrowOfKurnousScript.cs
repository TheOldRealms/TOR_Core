using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts;

public class ArrowOfKurnousScript : CareerAbilityMissleScript
{
    private float _radius;
    protected override void OnInit()
    {
        base.OnInit();
        var _effects = GetEffectsToTrigger();
        var radius = 0f;
        foreach (var effect in _effects)
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
        if (Hero.MainHero.HasCareerChoice("HailOfArrowsKeystone"))
        {
            var agents = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, _radius, CasterAgent.Team, new MBList<Agent>());
            var time = 4 * agents.Count;
            
            Agent.Main.ApplyStatusEffect("arrow_of_kurnous_buff_rel", Agent.Main, time);

        }

        base.HandleCollision(position, normal);
    }
}