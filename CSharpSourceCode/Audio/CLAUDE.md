# Audio

Small standalone audio subsystem for playing sound files that live outside the game's
FMOD sound-bank pipeline (loaded directly from disk as .ogg/.wav under the TOR_Armory
module's `ModuleSounds/` folder).

- **`TORAudioManager`** (static) — loads a file into a `TORModuleSound` via
  `CreateSoundInstance` (tries `.ogg` then falls back to `.wav`), plays it through a native
  `SoundEvent.CreateEventFromExternalFile` using a dummy FMOD event
  (`event:/Extra/voiceover`) as the carrier, and ticks all `_activeSounds` every frame
  (`SubModule.OnApplicationTick`) to handle looping (`RestartsWhenFinished`) and cleanup.
  `StopAll()` is called on `SubModule.OnGameEnd`.
- **`TORModuleSound`** — handle for one loaded sound: play/remove, 3D position + falloff
  range (`SetPosition`), restart-on-finish behavior.
- **`TORAudioAmbientSoundEmitter`** (`ScriptComponentBehavior`) — a scene-prop/entity script
  (also usable in the level editor via `OnEditorInit`/`OnEditorVariableChanged`) that plays
  a looping 3D ambient sound from `AudioName` when the camera is within `Range`, using the
  manager above. Drop this on a prop in a scene to give it ambient audio.

Distinct from voice lines / battle shouts, which live in `BattleMechanics/Voice`.
