using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts;

public class ImpenetrableScript : CareerAbilityScript
{
    protected override void OnBeforeTick(float dt)
    {
        base.OnBeforeTick(dt);

        // Shieldwall selects nearby allies before the normal movement update on the first tick.
        if (!HasTickedOnce && !IsFading)
        {
            GameEntity.SetGlobalFrame(GetNextGlobalFrame(GameEntity.GetGlobalFrame(), dt));
        }
    }

    protected override void OnBeforeRemoved(int removeReason)
    {
        var perkBehavior = Mission.Current.GetMissionBehavior<CareerPerkMissionBehavior>();

        if (perkBehavior == null || Agent.Main == null)
        {
            return;
        }

        var bonus = perkBehavior.CareerMissionVariables[0];

        perkBehavior.CareerMissionVariables[0] = 0;

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