using TaleWorlds.CampaignSystem;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerCampaignBase:CampaignBehaviorBase
    {
        private int _extraHealthPoints;
        private int _extraAmmo;
        private int _extraWind;
        private float[] _bonusMeleeDamage;
        private float[] _bonusRangeDamage;
        private float[] _bonusSpellDamge;


        public int GetMaximumHealthPoints()
        {
            return 10;
        }
        public int GetExtraAmmoPoints()
        {
            return 5;
        }
        
        public float[] GetCareerBonusSpellDamage()
        {
            //return _bonusMeleeDamage
            float[] damage = new float[(int)DamageType.All + 1];

            damage[(int)DamageType.Magical] = 0.25f;
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