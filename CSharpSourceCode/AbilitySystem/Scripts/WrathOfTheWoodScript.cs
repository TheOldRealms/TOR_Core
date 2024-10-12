using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
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


        
        

    }

    protected override void OnBeforeTick(float dt)
    {
        if(spawned) return;
        
        var count = 1;

        if (Hero.MainHero.HasCareerChoice("TreeSingingKeystone"))
        {
            count = 5;
        }


        var choices = Hero.MainHero.GetAllCareerChoices().WhereQ(x => x.Contains("Keystone")).ToListQ();
        var bonus = Mathf.Clamp(0.5f-(0.1f*choices.Count), 0f, 0.5f);
        
        while (count <= maximumSummons)
        {
            var threshold = this.Ability.Template.ScaleVariable1 + bonus;
            if (MBRandom.RandomFloat < threshold)
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

        var spawnCounter = 0;
        var targetPosition = CasterAgent.Frame.Advance(-10).origin;
        foreach (var treeSpirit in treeSpiritUnitIds)
        {
            var unitCount = treeSpirit.count;
            AgentBuildData data = null;
            if (treeSpirit.id == treemanID &&  !Mission.Current.IsFieldBattle)
            {
                 data = TORSummonHelper.GetAgentBuildData(CasterAgent,dryadID);
                 unitCount +=25;
            }
            else
            { 
                data = TORSummonHelper.GetAgentBuildData(CasterAgent, treeSpirit.id); 
            }
            
            
            for (int i = 0; i < unitCount; i++)
            {
                targetPosition = Mission.Current.GetRandomPositionAroundPoint(targetPosition, 0.1f, 2.5f);
                
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
        var treeSpirits = Mission.Current.Agents.WhereQ(x => x.Team == Agent.Main.Team && x.Character.StringId == dryadID);
        if (Hero.MainHero.HasCareerChoice("PathShapingKeystone"))
        {
            foreach (var treespirit in treeSpirits)
            {
                treespirit.ApplyStatusEffect("path_shaping_buff",null,10,true);
                treespirit.ApplyStatusEffect("path_shaping_buff_ats",null,10,true);
            }
        }

        if (Hero.MainHero.HasCareerChoice("MagicOfAthelLorenKeystone"))
        {
            foreach (var treeSpirit in treeSpirits)
            {
                treeSpirit.ApplyStatusEffect("magic_athel_loren_windslink",null,20,true);
            }
        }
    }
}