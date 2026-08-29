# BattleMechanics/AI/CivilianMissionAI

Non-combat AI for town/village scenes — NPCs going about their day and reacting to trouble.

- **`TORDailyBehaviorGroup`** (`: AgentBehaviorGroup`) — normal daytime routine group.
- **`TORAlarmedBehaviorGroup`** (`: AgentBehaviorGroup`) — takes over when something alarms
  the civilian population (a fight breaks out, a crime is witnessed).
- **`TORWalkingBehavior`** (`: AgentBehavior`) — idle wandering/walking-to-destination.
- **`TORFightBehavior`** (`: SandBox.Missions.AgentBehaviors.AgentBehavior`) — a civilian
  joining/reacting to a brawl.
