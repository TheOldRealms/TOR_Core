using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.AbilitySystem.Scripts;

public class ImpenetrableScript : CareerAbilityScript
{
    private bool _protectionStarted;
    private bool _protectionEnded;

    protected override void OnBeforeTick(float dt)
    {
        base.OnBeforeTick(dt);

        if (!HasTickedOnce && !IsFading)
        {
            GameEntity.SetGlobalFrame(GetNextGlobalFrame(GameEntity.GetGlobalFrame(), dt));
            var duration = EffectsToTrigger.Max(effect => effect.ImbuedStatusEffectDuration);
            ExtendLifeTime(Campaign.Current.Models.GetAbilityModel()
                .CalculateStatusEffectDurationForAbility((CharacterObject)CasterAgent.Character, Ability.Template, duration));
            var perkBehavior = Mission.Current.GetMissionBehavior<CareerPerkMissionBehavior>();
            if (perkBehavior != null) perkBehavior.CareerMissionVariables[0] = 0;
        }
        else if (CasterAgent.HasAttribute(CharacterAttributes.IMPENETRABLE))
        {
            ExtendLifeTime(dt);
        }
    }

    protected override void OnAfterTick(float dt)
    {
        if (CasterAgent.HasAttribute(CharacterAttributes.IMPENETRABLE))
        {
            _protectionStarted = true;
            return;
        }

        if (!_protectionStarted) return;

        _protectionEnded = true;
        Stop();
    }

    protected override void OnBeforeRemoved(int removeReason)
    {
        var perkBehavior = Mission.Current.GetMissionBehavior<CareerPerkMissionBehavior>();

        if (perkBehavior == null || Agent.Main == null)
        {
            return;
        }

        var bonus = perkBehavior.CareerMissionVariables[0] > 3 ? 3 : perkBehavior.CareerMissionVariables[0];

        perkBehavior.CareerMissionVariables[0] = 0;

        if (!_protectionEnded || !CasterAgent.IsActive() || Agent.Main != CasterAgent || Mission.Current.MissionEnded || Mission.Current.IsMissionEnding || Mission.Current.MissionIsEnding)
        {
            return;
        }

        if (Hero.MainHero.HasCareerChoice("GromrilArmorKeystone") && bonus > 0)
        {
            for (int i = 0; i < bonus; i++)
            {
                Agent.Main.ApplyStatusEffect("impenetrable_res_buff", Agent.Main, 10, false, false, true);
            }
        }

        if (Hero.MainHero.HasCareerChoice("RuneWeaponsKeystone") && bonus > 0)
        {
            for (int i = 0; i < bonus; i++)
            {
                Agent.Main.ApplyStatusEffect("impenetrable_dmg_buff", Agent.Main, 10, false, false, true);
            }
        }
    }
}
