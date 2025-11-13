using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.RaiseDead;

public static class TreeSpiritHelpers
{
    public static float GetSuccessChance(Hero spellsinger)
    {
        return Mathf.Min(0.7f, spellsinger.GetSkillValue(TORSkills.SpellCraft) * 0.002f);//350 Spellcraft to reach 0.7
    }

    public static bool CanBindTreeSpirits()
    {
        var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();
        return heroes.Any(hero => hero.IsSpellSinger());
    }
}