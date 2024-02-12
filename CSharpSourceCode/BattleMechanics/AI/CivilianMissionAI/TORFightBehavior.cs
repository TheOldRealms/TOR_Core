using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.CivilianMissionAI
{
    public class TORFightBehavior : SandBox.Missions.AgentBehaviors.AgentBehavior
    {
        public TORFightBehavior(AgentBehaviorGroup behaviorGroup) : base(behaviorGroup)
        {
            if (OwnerAgent.HumanAIComponent == null)
            {
                OwnerAgent.AddComponent(new HumanAIComponent(OwnerAgent));
            }
        }

		public override float GetAvailability(bool isSimulation)
		{
			if (!MissionFightHandler.IsAgentAggressive(OwnerAgent))
			{
				return 0.1f;
			}
			return 1f;
		}

		protected override void OnActivate()
		{
			TextObject textObject = new TextObject("{=!}{p0} {p1} activate alarmed behavior group.", null);
			textObject.SetTextVariable("p0", OwnerAgent.Name.ToString());
			textObject.SetTextVariable("p1", OwnerAgent.Index.ToString());
		}

		protected override void OnDeactivate()
		{
			TextObject textObject = new TextObject("{=!}{p0} {p1} deactivate fight behavior.", null);
			textObject.SetTextVariable("p0", OwnerAgent.Name.ToString());
			textObject.SetTextVariable("p1", OwnerAgent.Index.ToString());
		}

		public override string GetDebugInfo() => "TOR Fight";
    }
}
