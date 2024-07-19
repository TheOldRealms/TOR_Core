using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts;

public class WrathOfTheWoodScript : CareerAbilityScript
{
    private int maximumSummons = 25;
    private List<(string id, int count)> treeSpiritUnitIds = new List<(string id, int count)>();
    
    private const string dryadID = "tor_we_dryad";
    private const string treemanID = "tor_we_treeman";
    private bool spawned;
    protected override void OnInit()
    {
        base.OnInit();
        
        
        var count = 1;

        if (Hero.MainHero.HasCareerChoice("TreeSingingKeystone"))
        {
            count = 5;
        }
        while (count <= maximumSummons)
        {
            if (MBRandom.RandomFloat < 0.25f)
            {
                count++;
            }
            else
            {
                break;
            }
        }
        
        foreach (var triggeredEffect in this.EffectsToTrigger)
        {
            if (triggeredEffect.SummonedTroopId != "none")
            {
                switch (triggeredEffect.SummonedTroopId)
                {
                    case dryadID:
                        treeSpiritUnitIds.Add(new (triggeredEffect.SummonedTroopId,count));
                        break;
                    case treemanID:
                        treeSpiritUnitIds.Add(new (triggeredEffect.SummonedTroopId,1));
                        break;
                }
            }
        }
    }

    protected override void OnBeforeTick(float dt)
    {
        if(spawned) return;

        var spawnCounter = 0;
        var targetPosition = CasterAgent.Frame.Advance(-10).origin;
        foreach (var treeSpirit in treeSpiritUnitIds)
        {

            for (int i = 0; i < treeSpirit.count; i++)
            {
                targetPosition = Mission.Current.GetRandomPositionAroundPoint(targetPosition, 0.1f, 2.5f);
                var data = TORSummonHelper.GetAgentBuildData(CasterAgent, treeSpirit.id); 
                TORSummonHelper.SpawnAgent(data, targetPosition);
                spawnCounter++;
            }

        }

        if (Hero.MainHero.HasCareerChoice("VitalSurgeKeystone"))
        {
            foreach (var hero in Agent.Main.GetOriginMobileParty().GetMemberHeroes())
            {
                hero.Heal(spawnCounter,false);
            }
        }

        
        
        
        
        spawned = true;
    }

    protected override void OnAfterTick(float dt)
    {
        base.OnAfterTick(dt);
        
        if (Hero.MainHero.HasCareerChoice("PathShapingKeystone"))
        {
            var treespirits = Mission.Current.Agents.WhereQ(x => x.Team == Agent.Main.Team && (x.Character as CharacterObject).IsTreeSpirit());

            foreach (var treespirit in treespirits)
            {
                treespirit.ApplyStatusEffect("path_shaping_buff",null,10,true);
            }
        }
    }
}