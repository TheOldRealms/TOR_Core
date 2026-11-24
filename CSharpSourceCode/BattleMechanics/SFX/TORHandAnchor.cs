using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Scripts;

namespace TOR_Core.BattleMechanics.SFX
{
    // Keeps the flame near the caster while the spell controls where damage lands.
    public sealed class TORHandAnchor : ScriptComponentBehavior
    {
        public float Forward = 0.9f;
        public float FollowSeconds = 1.3f;
        public float GiveUpSeconds = 0.3f;
        public float Up = 0.05f;
        public float Right = 0f;

        private Agent _caster;
        private float _age;

        protected override void OnInit()
        {
            base.OnInit();
            _caster = null;
            _age = 0f;
            SetScriptComponentToTick(GetTickRequirement());
        }

        public override TickRequirement GetTickRequirement() => TickRequirement.Tick;

        protected override void OnTick(float dt)
        {
            _age += dt;
            try
            {
                if (_caster == null)
                {
                    var rootEntity = GameEntity.Root;
                    if (rootEntity.IsValid)
                        _caster = rootEntity.GetFirstScriptOfType<AbilityScript>()?.CasterAgent;
                    if (_caster == null)
                    {
                        if (_age > GiveUpSeconds)
                        {
                            StopFollowing();
                        }
                        return;
                    }
                }

                if (_age > FollowSeconds || !_caster.IsActive())
                {
                    StopFollowing();
                    return;
                }

                MatrixFrame root = GameEntity.Root.GetGlobalFrame();
                Vec3 side = root.rotation.s, forward = root.rotation.f, up = root.rotation.u;
                side.Normalize();
                forward.Normalize();
                up.Normalize();

                Vec3 chest = _caster.GetChestGlobalPosition();
                if (chest.IsValid)
                    GameEntity.SetGlobalPosition(chest + forward * Forward + up * Up + side * Right);
            }
            catch (Exception)
            {
                StopFollowing();
            }
        }

        private void StopFollowing()
        {
            _caster = null;
            SetScriptComponentToTick(TickRequirement.None);
        }

    }

    // Stops new particles while the last flames burn out.
    public sealed class TOREmissionWindow : ScriptComponentBehavior
    {
        public float StopAt = 1.2f;

        private float _age;

        protected override void OnInit()
        {
            base.OnInit();
            _age = 0f;
            SetScriptComponentToTick(GetTickRequirement());
        }

        public override TickRequirement GetTickRequirement() => TickRequirement.Tick;

        protected override void OnTick(float dt)
        {
            _age += dt;
            if (_age < StopAt) return;
            try { GameEntity.SetRuntimeEmissionRateMultiplier(0f); } catch (Exception) { }
            SetScriptComponentToTick(TickRequirement.None);
        }
    }
}
