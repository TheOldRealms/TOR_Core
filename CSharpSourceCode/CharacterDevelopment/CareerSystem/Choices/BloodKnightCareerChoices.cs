using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class BloodKnightCareerChoices : TORCareerChoiceBase
    {
        private CareerChoiceObject _bloodKnightRoot;
        
        private CareerChoiceObject _peerlessWarriorKeystone;
        private CareerChoiceObject _peerlessWarriorPassive1;
        private CareerChoiceObject _peerlessWarriorPassive2;
        private CareerChoiceObject _peerlessWarriorPassive3;
        private CareerChoiceObject _peerlessWarriorPassive4;
        
        private CareerChoiceObject _bladeMasterKeystone;
        private CareerChoiceObject _bladeMasterPassive1;
        private CareerChoiceObject _bladeMasterPassive2;
        private CareerChoiceObject _bladeMasterPassive3;
        private CareerChoiceObject _bladeMasterPassive4;
        
        private CareerChoiceObject _doomRiderKeystone;
        private CareerChoiceObject _doomRiderPassive1;
        private CareerChoiceObject _doomRiderPassive2;
        private CareerChoiceObject _doomRiderPassive3;
        private CareerChoiceObject _doomRiderPassive4;
        
        private CareerChoiceObject _controlledHungerKeyStone;
        private CareerChoiceObject _controlledHungerPassive1;
        private CareerChoiceObject _controlledHungerPassive2;
        private CareerChoiceObject _controlledHungerPassive3;
        private CareerChoiceObject _controlledHungerPassive4;
        
        private CareerChoiceObject _avatarOfDeathKeystone;
        private CareerChoiceObject _avatarOfDeathPassive1;
        private CareerChoiceObject _avatarOfDeathPassive2;
        private CareerChoiceObject _avatarOfDeathPassive3;
        private CareerChoiceObject _avatarOfDeathPassive4;
        
        private CareerChoiceObject _dreadKnightKeystone;
        private CareerChoiceObject _dreadKnightPassive1;
        private CareerChoiceObject _dreadKnightPassive2;
        private CareerChoiceObject _dreadKnightPassive3;
        private CareerChoiceObject _dreadKnightPassive4;







        protected override void RegisterAll()
        {
            _bloodKnightRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("VampireCountRoot"));
            
            _peerlessWarriorKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorKeystone"));
            _peerlessWarriorPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorPassive1"));
            _peerlessWarriorPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorPassive2"));
            _peerlessWarriorPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorPassive3"));
            _peerlessWarriorPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("PeerlessWarriorPassive4"));
            
            _bladeMasterKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterKeystone"));
            _bladeMasterPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterPassive1"));
            _bladeMasterPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterPassive2"));
            _bladeMasterPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterPassive3"));
            _bladeMasterPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BladeMasterPassive4"));
            
            _doomRiderKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderKeystone"));
            _doomRiderPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderPassive1"));
            _doomRiderPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderPassive2"));
            _doomRiderPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderPassive3"));
            _doomRiderPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DoomRiderPassive4"));
            
            _controlledHungerKeyStone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerKeystone"));
            _controlledHungerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerPassive1"));
            _controlledHungerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerPassive2"));
            _controlledHungerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerPassive3"));
            _controlledHungerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ControlledHungerPassive4"));
            
            _avatarOfDeathKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathKeystone"));
            _avatarOfDeathPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathPassive1"));
            _avatarOfDeathPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathPassive2"));
            _avatarOfDeathPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathPassive3"));
            _avatarOfDeathPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("AvatarOfDeathPassive4"));
            
            _dreadKnightKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightKeystone"));
            _dreadKnightPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightPassive1"));
            _dreadKnightPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightPassive2"));
            _dreadKnightPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightPassive3"));
            _dreadKnightPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DreadKnightPassive4"));
            
        }

        protected override void InitializeKeyStones()
        {
            _bloodKnightRoot.Initialize(TORCareers.BloodKnight, "root", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(StatusEffectTemplate),
                        MutationTargetOriginalId = "redfury_effect",
                        PropertyName = "BaseEffectValue",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded,DefaultSkills.TwoHanded,DefaultSkills.Polearm }, 0.05f, false,true),
                        MutationType = OperationType.Add
                    }
                });
            /*_peerlessWarriorKeystone.Initialize(TORCareers.BloodKnight, "root", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "RedFury",
                        PropertyName = "Duration",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){  DefaultSkills.Athletics }, 0.03f),
                        MutationType = OperationType.Add
                    }
                });*/
        }

        protected override void InitializePassives()
        {
            _peerlessWarriorPassive1.Initialize(TORCareers.BloodKnight, "Increases hitpoints by 25.", "PeerlessWarrior", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _peerlessWarriorPassive2.Initialize(TORCareers.BloodKnight, "Extra Melee melee Damage(10%).", "PeerlessWarrior", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Melee));
            _peerlessWarriorPassive3.Initialize(TORCareers.BloodKnight, " For every Troop tier 4 and above, the gained XP is increased by 20% for kills.", "PeerlessWarrior", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special,true)); 
            _peerlessWarriorPassive4.Initialize(TORCareers.BloodKnight, "Everyday you gain randomly 100 xp in one of the melee combat skills", "PeerlessWarrior", false, ChoiceType.Passive,null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special,false));
            
            _bladeMasterPassive1.Initialize(TORCareers.BloodKnight, "Extra Melee melee Damage(20%).", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical,10),AttackTypeMask.Melee));
            _bladeMasterPassive2.Initialize(TORCareers.BloodKnight, "Increases health regeneration after battles by 5.", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.HealthRegeneration));
            _bladeMasterPassive3.Initialize(TORCareers.BloodKnight, "Hits below 15 damage will not stagger character.", "BladeMaster", false, ChoiceType.Passive, null); 
            _bladeMasterPassive4.Initialize(TORCareers.BloodKnight, "All troops are gaining XP(including Player) while raiding villages", "BladeMaster", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50));
            
            _doomRiderPassive1.Initialize(TORCareers.BloodKnight, "Villageraids are 50% more efficient.", "DoomRider", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(50f,PassiveEffectType.Special,true )); 
            _doomRiderPassive2.Initialize(TORCareers.BloodKnight, "Extra Melee melee Damage(20%).", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical,20),AttackTypeMask.Melee));
            _doomRiderPassive3.Initialize(TORCareers.BloodKnight, "Increases Hitpoints by 50.", "DoomRider", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _doomRiderPassive4.Initialize(TORCareers.BloodKnight, "Partyspeed Increased by 2", "DoomRider", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2.0f, PassiveEffectType.PartyMovementSpeed));
            
            _controlledHungerPassive1.Initialize(TORCareers.BloodKnight, "immune to to sunlight malus.", "ControlledHunger", false, ChoiceType.Passive, null); //TORPartySpeedCalculatingModel 46
            _controlledHungerPassive2.Initialize(TORCareers.BloodKnight, "Increases hitpoints by 50.", "ControlledHunger", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _controlledHungerPassive3.Initialize(TORCareers.BloodKnight, "Mount health is increased by 35%", "ControlledHunger",false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(35f,PassiveEffectType.Special,true ));
            _controlledHungerPassive4.Initialize(TORCareers.BloodKnight, "Wardsave for all vampire units.", "ControlledHunger", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            
            _avatarOfDeathPassive1.Initialize(TORCareers.BloodKnight, "25% Physical damage reduction from Melee and Ranged attacks.", "AvatarOfDeath", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical,25),AttackTypeMask.Ranged|AttackTypeMask.Melee));
            _avatarOfDeathPassive2.Initialize(TORCareers.BloodKnight, "Vampire Wages are reduced by 35%.", "AvatarOfDeath", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-35, PassiveEffectType.Special,true)); //TORPartyWageModel 85
            _avatarOfDeathPassive3.Initialize(TORCareers.BloodKnight, "35%. Magical Damage reduction from Spells", "AvatarOfDeath", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical,35),AttackTypeMask.Spell));
            _avatarOfDeathPassive4.Initialize(TORCareers.BloodKnight, "Defeated Units can be recruited with 15%(40% for >4Tier) chance to blood knight initates", "AvatarOfDeath", false, ChoiceType.Passive, null); 
            
            _dreadKnightPassive1.Initialize(TORCareers.BloodKnight, "Increases hitpoints by 50.", "DreadKnight", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
            _dreadKnightPassive2.Initialize(TORCareers.BloodKnight, "Horse charge damage is increased by 50%.", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special, true));
            _dreadKnightPassive3.Initialize(TORCareers.BloodKnight, "Mounted units damage is increased by 20%", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));
            _dreadKnightPassive4.Initialize(TORCareers.BloodKnight, "Mighty Fighter: Melee hits allow more often for Knockdowns or dismounts. Depends on enemy tier and strike Magnitude", "DreadKnight", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All,15),AttackTypeMask.All));

            
        }
    }
}