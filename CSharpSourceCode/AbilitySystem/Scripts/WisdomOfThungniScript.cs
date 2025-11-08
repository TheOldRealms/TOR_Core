using TaleWorlds.CampaignSystem;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.AbilitySystem.Scripts;

public class WisdomOfThungniScript : CareerAbilityScript
{
    private const int BASEVALUE = 5;

    public override void Initialize(Ability ability)
    {
        base.Initialize(ability);

        if (Agent.Main == null) return;
        var agent = Agent.Main;
        var abilityComponent = agent.GetComponent<AbilityComponent>();

        var bonus = Ability.Template.ScaleVariable1;
        var secondRound = false;
        var value = (int)(BASEVALUE + bonus);


        var trait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "magical_weapon_10");
        if (trait == null) return;

        var comp = agent.GetComponent<ItemTraitAgentComponent>();
        if (comp != null) comp.AddTraitToWieldedWeapon(trait, ability.Template.Duration);

        foreach (var element in abilityComponent.KnownAbilitySystem)
        {
            if (!element.IsOnCooldown()) continue;

            if (!element.Template.IsSpell) continue;

            var left = element.GetCoolDownLeft();
            if (secondRound) value /= 2;

            element.SetCoolDown(left - value);

            if (Hero.MainHero.HasAttribute("StoneAndSteelPassive4"))
                if (!element.IsOnCooldown())
                {
                    var choice = TORCareerChoices.GetChoice("StoneAndSteelPassive4");
                    Agent.Main.ApplyStatusEffect("thungni_stone_and_steel_buff", Agent.Main, choice.GetPassiveValue());
                }

            if (!secondRound && Hero.MainHero.HasCareerChoice("ForgefireBurningKeystone"))
            {
                secondRound = true;
                continue;
            }

            if (left < value)
            {
                value -= left;
                continue;
            }


            break;
        }
    }
}