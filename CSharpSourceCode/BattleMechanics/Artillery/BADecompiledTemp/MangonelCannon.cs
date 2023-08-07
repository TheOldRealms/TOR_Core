using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;


namespace TOR_Core.BattleMechanics.Artillery.BA
{
    public class MangonelCannon : Mangonel
    {
        private SynchedMissionObject _barrel;
        private MatrixFrame _barrelInitialLocalFrame;
        private float _verticalOffsetAngle;
        private bool _cannonMove;
        private bool _cannonMoveInit;
        private Vec3 _Destination;
        private BallistaMove MovementComponent;
        private float WheelDiameter = 0.6f;
        private float MinSpeed = 0.5f;
        private float MaxSpeed = 1.25f;

        protected override void OnInit()
        {
            this.CollectEntities();
            base.OnInit();
        }

        public override UsableMachineAIBase CreateAIBehaviorObject() => (UsableMachineAIBase)new MangonelCannonAI(this);

        protected override void OnTick(float dt) => base.OnTick(dt);

        protected override void OnRangedSiegeWeaponStateChange() => base.OnRangedSiegeWeaponStateChange();

        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            TextObject forStandingPoint = usableGameObject.GameEntity.HasTag("reload")
                ? new TextObject(this.PilotStandingPoint == usableGameObject ? "{=fEQAPJ2e}{KEY} Use" : "{=Na81xuXn}{KEY} Rearm")
                : (usableGameObject.GameEntity.HasTag("rotate") ? new TextObject("{=5wx4BF5h}{KEY} Rotate") : (usableGameObject.GameEntity.HasTag(this.AmmoPickUpTag) ? new TextObject("{=bNYm3K6b}{KEY} Pick Up") : (!usableGameObject.GameEntity.HasTag("ammoload") ? new TextObject("{=fEQAPJ2e}{KEY} Use") : new TextObject("{=ibC4xPoo}{KEY} Load Ammo"))));
            forStandingPoint.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            return forStandingPoint;
        }

        public override float GetTargetValue(List<Vec3> weaponPos) => 40f * this.GetUserMultiplierOfWeapon() * this.GetDistanceMultiplierOfWeapon(weaponPos[0]) * this.GetHitPointMultiplierOfWeapon();

        public override float ProcessTargetValue(float baseValue, TargetFlags flags)
        {
            if (flags.HasAnyFlag<TargetFlags>(TargetFlags.NotAThreat))
                return -1000f;
            if (flags.HasAnyFlag<TargetFlags>(TargetFlags.IsSiegeEngine))
                baseValue *= 10000f;
            if (flags.HasAnyFlag<TargetFlags>(TargetFlags.IsStructure))
                baseValue *= 2.5f;
            if (flags.HasAnyFlag<TargetFlags>(TargetFlags.IsSmall))
                baseValue *= 8f;
            if (flags.HasAnyFlag<TargetFlags>(TargetFlags.IsMoving))
                baseValue *= 8f;
            if (flags.HasAnyFlag<TargetFlags>(TargetFlags.DebugThreat))
                baseValue *= 10000f;
            if (flags.HasAnyFlag<TargetFlags>(TargetFlags.IsSiegeTower))
                baseValue *= 8f;
            return baseValue;
        }

        protected override void GetSoundEventIndices()
        {
            this.MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/move");
            this.ReloadSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/reload");
        }

        protected override void RegisterAnimationParameters() => base.RegisterAnimationParameters();

        private void CollectEntities() => this._barrel = this.GameEntity.CollectObjectsWithTag<SynchedMissionObject>("Cbarrel")[0];

        protected override float GetDetachmentWeightAux(BattleSideEnum side) => this.GetDetachmentWeightAuxForExternalAmmoWeapons(side);
    }

}
