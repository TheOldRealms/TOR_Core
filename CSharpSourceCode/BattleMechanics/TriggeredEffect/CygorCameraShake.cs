using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    internal static class CygorCameraShake
    {
        private static FieldInfo _startField;
        private static FieldInfo _intensityField;
        private static bool _resolved;

        public static void Apply(Vec3 position, float range, float intensity)
        {
            if (range <= 0f || intensity <= 0f) return;

            var screen = FindScreen();
            if (screen == null) return;

            float distance = screen.CombatCamera.Position.Distance(position);
            if (distance >= range) return;

            float scaled = intensity * (1f - distance / range);
            if (scaled <= 0.02f) return;
            if (!ResolveFields()) return;

            float now = Game.Current != null ? Game.Current.ApplicationTime : -1f;
            if (now < 0f) return;

            float existingStart = (float)_startField.GetValue(screen);
            float existingIntensity = (float)_intensityField.GetValue(screen);
            if (existingStart > now - 2f)
            {
                float elapsed = (now - existingStart) / 2f;
                if (elapsed < 0f) elapsed = 0f;
                float decay = 1f - (float)Math.Pow(elapsed, 0.4);
                if (decay * existingIntensity >= scaled) return;
            }

            _startField.SetValue(screen, now);
            _intensityField.SetValue(screen, scaled);
        }

        private static MissionScreen FindScreen()
        {
            if (ScreenManager.TopScreen is MissionScreen top && top.CombatCamera != null)
            {
                return top;
            }

            var behaviors = Mission.Current?.MissionBehaviors;
            if (behaviors == null) return null;

            for (int i = 0; i < behaviors.Count; i++)
            {
                if (behaviors[i] is MissionView view && view.MissionScreen != null && view.MissionScreen.CombatCamera != null)
                {
                    return view.MissionScreen;
                }
            }

            return null;
        }

        private static bool ResolveFields()
        {
            if (_resolved) return _startField != null && _intensityField != null;

            _resolved = true;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            _startField = typeof(MissionScreen).GetField("_cameraShakeStartTime", flags);
            _intensityField = typeof(MissionScreen).GetField("_cameraShakeIntensity", flags);
            return _startField != null && _intensityField != null;
        }
    }
}
