# BattleMechanics/Voice

Custom voice-over playback that bypasses the native `Agent.MakeVoice` (which the comments
note can crash for TOR's custom races/monsters).

- **`TORVoiceManager`** (singleton) — loads `tor_voice_definitions.xml` (from the
  TOR_Armory module's data folder) mapping a race/monster name to a `VoiceDefinition`
  (internal class) of sound file names; `GetVoiceToPlay(agent, voiceType)` picks the right
  line for that agent's race (human/vampire/skeleton/spirit_host/wraith/etc.) and
  `SkinVoiceType`. Played back through `Audio/TORAudioManager`, not native FMOD voice events.
  Initialized in `SubModule.OnSubModuleLoad`.
- **`AgentVoiceComponent(agent)`** (`: AgentComponent`) — per-agent component that requests
  voice lines (hurt/death/battle cries) from the manager at the right moments.
- **`BattleShoutsMissionLogic`** (`: MissionLogic`) — mission-level trigger for battle-cry
  style shouts (charge, morale events), added in `SubModule.OnMissionBehaviorInitialize`.
