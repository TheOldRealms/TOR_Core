using SandBox;
using SandBox.Missions.AgentBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.CivilianMissionAI
{
    public class TORDailyBehaviorGroup : AgentBehaviorGroup
    {
		public TORDailyBehaviorGroup(AgentNavigator navigator, Mission mission) : base(navigator, mission)
		{
		}

		public override void Tick(float dt, bool isSimulation)
		{
			if (ScriptedBehavior != null)
			{
				if (!ScriptedBehavior.IsActive)
				{
                    DisableAllBehaviors();
                    ScriptedBehavior.IsActive = true;
				}
			}
            else if (CheckBehaviorTimer == null || CheckBehaviorTimer.Check(Mission.CurrentTime))
			{
				Think(isSimulation);
			}
			TickActiveBehaviors(dt, isSimulation);
		}

		public override void ConversationTick()
		{
			foreach (var agentBehavior in Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					agentBehavior.ConversationTick();
				}
			}
		}

		private void Think(bool isSimulation)
		{
			float num = 0f;
			float[] array = new float[Behaviors.Count];
			for (int i = 0; i < Behaviors.Count; i++)
			{
				array[i] = Behaviors[i].GetAvailability(isSimulation);
				num += array[i];
			}
			if (num > 0f)
			{
				float num2 = MBRandom.RandomFloat * num;
				int j = 0;
				while (j < array.Length)
				{
					float num3 = array[j];
					num2 -= num3;
					if (num2 < 0f)
					{
						if (!Behaviors[j].IsActive)
						{
                            DisableAllBehaviors();
							Behaviors[j].IsActive = true;
                            CheckBehaviorTime = Behaviors[j].CheckTime;
                            SetCheckBehaviorTimer(CheckBehaviorTime);
							return;
						}
						break;
					}
					else
					{
						j++;
					}
				}
			}
		}

		private void TickActiveBehaviors(float dt, bool isSimulation)
		{
			foreach (var agentBehavior in Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					agentBehavior.Tick(dt, isSimulation);
				}
			}
		}

		private void SetCheckBehaviorTimer(float time)
		{
			if (CheckBehaviorTimer == null)
			{
				CheckBehaviorTimer = new Timer(Mission.CurrentTime, time, true);
				return;
			}
			CheckBehaviorTimer.Reset(Mission.CurrentTime, time);
		}

		public override float GetScore(bool isSimulation)
		{
			return 0.5f;
		}

		public override void OnAgentRemoved(Agent agent)
		{
			foreach (var agentBehavior in Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					agentBehavior.OnAgentRemoved(agent);
				}
			}
		}

		protected override void OnActivate()
		{
			Navigator.SetItemsVisibility(true);
			Navigator.SetSpecialItem();
		}

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			CheckBehaviorTimer = null;
		}

		public override void ForceThink(float inSeconds)
		{
			if (MathF.Abs(inSeconds) < 1E-45f)
			{
				Think(false);
				return;
			}
			SetCheckBehaviorTimer(inSeconds);
		}
	}
}
