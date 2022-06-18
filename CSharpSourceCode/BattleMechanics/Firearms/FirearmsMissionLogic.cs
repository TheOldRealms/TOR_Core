using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.Firearms
{
    public class FirearmsMissionLogic : MissionLogic
    {
        private int[] _soundIndex = new int[5];
        private Random _random;

        public FirearmsMissionLogic()
        {
            for (int i = 0; i < 5; i++)
            {
                _soundIndex[i] = SoundEvent.GetEventIdFromString("musket_fire_sound_" + (i + 1));
            }
            _random = new Random();
        }

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            var itemUsage = shooterAgent.WieldedWeapon.CurrentUsageItem.ItemUsage;
            if (itemUsage.Contains("handgun") || itemUsage.Contains("pistol"))
            {
                var frame = new MatrixFrame(orientation, position);
                // run particles of smoke
                var offset = (shooterAgent.WieldedWeapon.CurrentUsageItem.WeaponLength + 30) / 100;
                frame.Advance(offset);
                Mission.AddParticleSystemBurstByName("handgun_shoot_2", frame, false);
                // play sound of shot
                if (_soundIndex.Length > 0)
                {
                    int selected = _random.Next(0, _soundIndex.Length - 1);
                    Mission.MakeSound(_soundIndex[selected], position, false, true, -1, -1);
                }
            }
        }
    }
}
