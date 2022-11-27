using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using HarmonyLib;
using NLog.Layouts;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.CampaignMechanics.Career
{
    [Serializable]
    public class CareerTemplate        //The Serializable Component that stores all passive nodes and key stones, and is load into the game on start up
    {
        [XmlAttribute]
        public CareerId CareerId = CareerId.None;     //might also be just a string ensures, that no typos are made and the right Career template
        [XmlAttribute]
        public string AbilityTemplateId;
        [XmlArray("CareerAbilityWeaponRequirements")] 
        public List<WeaponClass> CareerAbilityWeaponRequirements = new List<WeaponClass>(); 
        [XmlArray("KeyStones")] 
        public List<KeyStoneNode> KeyStoneNodes= new List<KeyStoneNode>();
        
        //[XmlAttribute] public List<PassiveNode> PassiveNodes;
    }
    
  
    // Optional, if the said type is not wield, the ability can't be performed.
    [Serializable]
    public class KeyStoneNode
    {
        [XmlAttribute] public string NodeId = "a";
        [XmlElement] 
        public AbilityTemplateModifier Modifier;
        [XmlAttribute]
        public string CharacterAttribute=""; // Attributes can serve to add new benefits for campaign via models : Example : grailknight: Requires attribute "QuestingKnight" to recruit Questing Knights 
        [XmlAttribute]
        public float AttributeValue; // Not mandatory but potential either a scale factor or addition 
        
    }
    
    [Serializable]
    public class AbilityTemplateModifier
    {
        
        //Override : Based on Hierachical structure, the former effect, is overriden by the new effect. Game Design needs to consider to lock abilities that could be conflicting.
        //null, '""',"none" or Invalid will be ignored upon application of the Modifier 
        
        //modify : More or less, we are going with addition for easier handling.
        
        [XmlAttribute] public string NameOverride = "";         //Override
        [XmlAttribute] public string SpriteOverride = "";   //Override
        [XmlAttribute] public bool StartsOnCoolDownOverride;    //Override
        [XmlAttribute] public ChargeType ChargeType = ChargeType.Invalid;  //Override
        [XmlAttribute] public int Charge = 0;                   //Modifier
        [XmlAttribute] public int ChargeRequirement = 0;        //Modifier
        [XmlAttribute] public int CoolDown = 0;                 //Modifier
        [XmlAttribute] public int Usages =0;                   //Modifier
        [XmlAttribute] public int WindsOfMagicCost = 0;         //Modifier
        [XmlAttribute] public float BaseMisCastChance = 0;      //Modifier
        [XmlAttribute] public float Duration = 0;               //Modifier
        [XmlAttribute] public float Radius = 0;                 //Modifier
        [XmlAttribute] public float BaseMovementSpeed = 0;      //Modifier
        //[XmlAttribute] public string TriggeredEffectID = "";        //Override
        //[XmlAttribute] public bool? HasLight = null;                //Override
        //[XmlAttribute] public float? LightIntensity = null;           //Override
        //[XmlAttribute] public float? LightRadius = null;              //Override
        //[XmlAttribute] public Vec3? LightColorRGB = null;  //Override
        //[XmlAttribute] public float? LightFlickeringMagnitude = null;  //Override
        //[XmlAttribute] public float? LightFlickeringInterval = null;    //Override
        //[XmlAttribute] public bool? ShadowCastEnabled = true;       //Override
        //[XmlAttribute] public string ParticleEffectPrefab = "";     //Override
        //[XmlAttribute] public string SoundEffectToPlay = "";        //Override
        //[XmlAttribute] public bool? ShouldSoundLoopOverDuration = null; //Override
        //[XmlAttribute] public CastType CastType = AbilitySystem.CastType.Invalid;   //Override
        [XmlAttribute] public float CastTime = 0;               //Modifier
        [XmlAttribute] public AbilityTargetType AbilityTargetType = AbilityTargetType.Invalid; //Override
        [XmlAttribute] public float Offset = 0;                 //Modifier
        [XmlAttribute] public float MinDistance = 0;            //Modifier
        [XmlAttribute] public float MaxDistance = 0;            //Modifier
        [XmlAttribute] public string TooltipDescription = "";       //Override, Stack?

        //[XmlAttribute] public DamageType DamageType = DamageType.Invalid;          //Override
        [XmlAttribute] public int Damage;                        //Modifier
        //[XmlAttribute] public string BurstParticleEffectPrefab = "none";           //Override
        //[XmlAttribute] public string SoundEffectId = "none";                       //Override
        //[XmlAttribute] public float? SoundEffectLength = null;                      //Override
        //[XmlAttribute] public float? ImpactRadius = null;                           //Modifier
        //[XmlAttribute] public bool? HasShockWave = null;                            //Override
        //[XmlAttribute] public TargetType TargetType = TargetType.Invalid;          //Override  
        //[XmlAttribute] public string ImbuedStatusEffectID = "none";                //Override
        [XmlAttribute] public float ImbuedStatusEffectDuration = 0;               //Modifier
        //[XmlAttribute] public string ScriptNameToTrigger = "none";                 //Override
        //[XmlAttribute] public string TroopIdToSummon = "none";                     //override
        [XmlAttribute] public int NumberToSummon = 0;                               //modifier
        public AbilityTemplateModifier() { }
    }
    
    



    public class PassiveNode : CareerTreeNode
    { 
        [XmlAttribute]
        public float Current=0;
        [XmlAttribute]
        public PassiveNodeEffect EffectType=PassiveNodeEffect.None;
    }
    [Serializable]
    public class CareerTreeNode    //needs to be serializable
    {
        [XmlAttribute]
        public int Order=0; // nodes with higher order have preference over other nodes in case of a conflict.
        [XmlAttribute]
        public string Id="";
        [XmlAttribute]
        public List<string> ParentIDs=new List<string>();
        [XmlAttribute]// The node can have 1 or many parents. If parents are empty it's the root node. The parents only account for setting the "unlockable" state if other conditions are met.
        public TreeNodeState State=TreeNodeState.Locked;
        [XmlAttribute]
        public string DescriptionText="";
        [XmlAttribute]
        public string AttributeCondition=""; // The Node requires the character to posses a certain attribute, like beeing a Questing Knight, to unlock Node
        [XmlAttribute]
        public string UnlockConditionText="";
        [XmlAttribute]
        public List<string> LockNodes=new List<string>(); // On investing into this talent node, the list of  nodes that get blocked
    }

    public enum ModificationType
    {
        Add,
        Override,
        ForceOveride
    }
    
    //OnAbilityAttackScript
    public enum CareerId
    {
        None,
        MinorVampire,
        GrailKnight,
        WarriorPriest
    }

    public enum TreeNodeState
    {
        Locked,
        Available,
        Unlocked
    }

    public enum PassiveNodeEffect
    {
        None,
        HealthPoint,
        Ammunition,
        WindsOfMagic
    }

 
}