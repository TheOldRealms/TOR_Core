using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.CampaignMechanics.Career
{
    
    /// <summary>
    /// The main communicator, between the World and the serialization process.
    /// 1.Loads on startup the model of the Career,
    /// 2. checks all unlocked nodes with the model, removes all that's id is not existing in the model, removes it's properties,  and provides 
    /// </summary>
    public class CareerCampaignBase:CampaignBehaviorBase
    {
        private int _extraHealthPoints;
        private int _extraAmmo;
        private int _extraWind;
        // extra faith
        private float[] _bonusMeleeDamage;
        private float[] _bonusRangeDamage;
        private float[] _bonusSpellDamge;
        
        //Career ability perks
        private WeaponClass[] requiredWeaponTypes= { WeaponClass.OneHandedSword , WeaponClass.TwoHandedSword,WeaponClass.OneHandedPolearm, WeaponClass.TwoHandedPolearm, WeaponClass.LowGripPolearm };
        private string statusEffectOverride;
        private bool CanBeUsedOnHorse = false;
        
        //post attack behavior? Scriptname


        public bool HasRequiredWeaponFlags(WeaponClass weaponClass)
        {
            return requiredWeaponTypes.Any(item => weaponClass == item);
        }
        public bool CanBeUsedWhileMounted()
        {
            return CanBeUsedOnHorse;
        }
        public int GetExtraHealthPoints()
        {
            return 10;
        }
        public int GetExtraAmmoPoints()
        {
            return 0;
        }
        
        public int GetExtraWindPoints()
        {
            return 10;
        }
        
        public float[] GetCareerBonusSpellDamage()
        {
            //return _bonusMeleeDamage
            float[] damage = new float[(int)DamageType.All + 1];

            damage[(int)DamageType.Fire] = 0.25f;
            return damage;
        }

        public float[] GetCareerBonusMeleeDamage()
        {
            
            //return _bonusMeleeDamage
            float[] damage = new float[(int)DamageType.All + 1];

            damage[(int)DamageType.Holy] = 0.25f;
            return damage;

        }
        
        public float[] GetCareerBonusRangeDamage()
        {
            //return _bonusMeleeDamage
            float[] damage = new float[(int)DamageType.All + 1];
            damage[(int)DamageType.Lightning] = 0.25f;
            return damage;
        }

        
        
        public override void RegisterEvents()
        {
            
        }
        
        

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}